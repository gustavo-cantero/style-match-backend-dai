using Microsoft.Data.SqlClient;
using StyleMatch.Helpers;
using StyleMatch.Models;
using System.Data;

namespace StyleMatch.Data;

/// <summary>
/// Clase para el manejo de la información de un usuario
/// </summary>
public static class User
{
    #region Seguridad

    /// <summary>
    /// Devuelve los datos básicos de un usuario
    /// </summary>
    /// <param name="email">Email del usuario</param>
    /// <returns>Datos básicos del usuario</returns>
    public static async Task<UserModel?> GetAsync(string email)
    {
        using var conn = await DataHelper.CreateConnection();
        using SqlCommand cmd = conn.CreateCommand("User_GetByEmail", CommandType.StoredProcedure);
        cmd.Parameters.Add("@Email", SqlDbType.VarChar, 200).Value = email;
        using var dr = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow);
        if (!await dr.ReadAsync())
            return null;

        return new UserModel()
        {
            UserId = dr.GetInt32("UserId"),
            Email = dr.GetString("Email"),
            Name = dr.GetString("Name"),
            IsActive = dr.GetBoolean("IsActive"),
            RoleId = (Role)dr.GetByte("RoleId")
        };
    }

    /// <summary>
    /// Devuelve los datos básicos de un usuario
    /// </summary>
    /// <param name="userId">Identificador del usuario</param>
    /// <returns>Datos básicos del usuario</returns>
    public static async Task<UserModel?> GetAsync(int userId)
    {
        using var conn = await DataHelper.CreateConnection();
        using SqlCommand cmd = conn.CreateCommand("User_GetByUserId", CommandType.StoredProcedure);
        cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
        using var dr = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow);
        if (!await dr.ReadAsync())
            return null;

        return new UserModel()
        {
            UserId = dr.GetInt32("UserId"),
            Email = dr.GetString("Email"),
            Name = dr.GetString("Name"),
            IsActive = dr.GetBoolean("IsActive"),
            RoleId = (Role)dr.GetByte("RoleId")
        };
    }

    /// <summary>
    /// Autentica un usuario
    /// </summary>
    /// <param name="email">Email</param>
    /// <param name="password">Contraseña</param>
    /// <returns>Resultado de la autenticación</returns>
    public static async Task<bool> AuthAsync(string email, string password)
    {
        using var conn = await DataHelper.CreateConnection();
        using SqlCommand cmd = conn.CreateCommand("User_Authenticate", CommandType.StoredProcedure);

        cmd.Parameters.Add("@Email", SqlDbType.VarChar, 200).Value = email;
        cmd.Parameters.Add("@password", SqlDbType.NVarChar, 255).Value = password;
        return (await cmd.ExecuteReturnInt32Async()) == 1;
    }




    #endregion

    /// <summary>
    /// Graba los datos de un usuario
    /// </summary>
    /// <param name="data">Datos del usuario</param>
    public static async Task<int> SaveAsync(UserModel data)
    {
        using var conn = await DataHelper.CreateConnection();
        using SqlCommand cmd = conn.CreateCommand();

        cmd.CommandType = CommandType.StoredProcedure;
        if (data.UserId.HasValue && data.UserId.Value != 0)
        {
            cmd.CommandText = "User_Update";
            cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = data.UserId.Value;
        }
        else
        {
            cmd.CommandText = "User_Add";
            cmd.Parameters.Add("@RoleId", SqlDbType.Int).Value = data.RoleId;
        }
        if (!string.IsNullOrWhiteSpace(data.Email))
            cmd.Parameters.Add("@Email", SqlDbType.VarChar, 200).Value = data.Email;
        if (!string.IsNullOrWhiteSpace(data.Name))
            cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 50).Value = data.Name;
        cmd.Parameters.Add("@IsActive", SqlDbType.Bit).Value = data.IsActive;
        if (!string.IsNullOrEmpty(data.Password))
            cmd.Parameters.Add("@Password", SqlDbType.NVarChar, 50).Value = data.Password;

        return await cmd.ExecuteReturnInt32Async();
    }

    /// <summary>
    /// Cambia la contraseña de un usuario
    /// </summary>
    /// <param name="userId">Identificador del usuario</param>
    /// <param name="currentPassword">Contraseña actual</param>
    /// <param name="newPassword">Nueva contraseña</param>
    /// <returns>Devuelve si se pudo cambiar la contraseña</returns>
    public static async Task<bool> UpdatePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        using var conn = await DataHelper.CreateConnection();
        using SqlCommand cmd = conn.CreateCommand("User_UpdatePassword", CommandType.StoredProcedure);

        cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
        cmd.Parameters.Add("@CurrentPassword", SqlDbType.NVarChar, 100).Value = currentPassword;
        cmd.Parameters.Add("@NewPassword", SqlDbType.NVarChar, 100).Value = newPassword;
        return await cmd.ExecuteReturnInt32Async() > 0;
    }

    /// <summary>
    /// Valida un usuario
    /// </summary>
    /// <param name="email">Usuario</param>
    /// <returns>Resultado de la validación</returns>
    public static async Task<bool> ValidateAsync(string email)
    {
        using var conn = await DataHelper.CreateConnection();
        using SqlCommand cmd = conn.CreateCommand("User_Validate", CommandType.StoredProcedure);
        cmd.Parameters.Add("@Email", SqlDbType.VarChar, 200).Value = email;
        return await cmd.ExecuteReturnInt32Async() > 0;
    }


    /// <summary>
    /// Actualiza la contraseña de un usuario utilizando el código de recuperación
    /// </summary>
    /// <param name="userId">Identificador del usuario</param>
    /// <param name="newPassword">Nueva contraseña</param>
    /// <returns>Devuelve si se pudo actualizar correctamente</returns>
    public static async Task<bool> UpdatePasswordAsync(int userId, string newPassword)
    {
        using var conn = await DataHelper.CreateConnection();
        using SqlCommand cmd = conn.CreateCommand("User_UpdatePasswordWithRecoveryCode", CommandType.StoredProcedure);

        cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
        cmd.Parameters.Add("@NewPassword", SqlDbType.NVarChar, 100).Value = newPassword;

        return await cmd.ExecuteReturnInt32Async() > 0;
    }

    /// <remarks>
    /// Para que funcione la opción de log in with google
    /// </remarks>
    /// <summary>
    /// Crea un usuario desde Google Login (sin contraseña)
    /// </summary>
    /// <param name="user">Datos del usuario</param>
    public static async Task CreateFromGoogleAsync(UserModel user)
    {
        using var conn = await DataHelper.CreateConnection();
        using var cmd = conn.CreateCommand("User_CreateFromGoogle", CommandType.StoredProcedure);

        cmd.Parameters.Add("@Email", SqlDbType.NVarChar, 200).Value = user.Email!;
        cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 100).Value = (object?)user.Name ?? DBNull.Value;
        cmd.Parameters.Add("@IsActive", SqlDbType.Bit).Value = user.IsActive;

        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Actualiza el código de recuperación de contraseña de un usuario
    /// </summary>
    /// <param name="userId">Identificador del usuario</param>
    /// <param name="recoveryCode">Código de recuperación</param>
    public static async Task UpdateRecoveryCode(int userId, string recoveryCode)
    {
        // Actualizo el código y la expiración en la base de datos
        using var conn = await DataHelper.CreateConnection();
        using var cmd = conn.CreateCommand("User_UpdateRecoveryCode", CommandType.StoredProcedure);

        cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
        cmd.Parameters.Add("@RecoveryCode", SqlDbType.VarChar, 6).Value = recoveryCode;

        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Evalúa si el código de recuperación es válido
    /// </summary>
    /// <param name="email">Dirección de correo electrónico del usuario</param>
    /// <param name="recoveryCode">Código de recuperación</param>
    /// <returns>Resultado de la validación</returns>
    public static async Task<int> CheckRecoveryCode(string email, string recoveryCode)
    {
        using var conn = await DataHelper.CreateConnection();
        using var cmd = conn.CreateCommand("User_CheckRecoveryCode", CommandType.StoredProcedure);
        cmd.Parameters.Add("@Email", SqlDbType.VarChar, 200).Value = email;
        cmd.Parameters.Add("@RecoveryCode", SqlDbType.VarChar, 6).Value = recoveryCode;
        return await cmd.ExecuteReturnInt32Async();
    }
}