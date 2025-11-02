using Microsoft.Data.SqlClient;
using StyleMatch.Helpers;
using StyleMatch.Models;
using System.Data;

namespace StyleMatch.Data;

/// <summary>
/// Clase para el manejo de la información de las prendas
/// </summary>
public static class Garment
{
    /// <summary>
    /// Graba los datos de una prenda
    /// </summary>
    /// <param name="userId">Identificador del usuario</param>
    /// <param name="data">Datos de la prenda</param>
    public static async Task AddAsync(int userId, GarmentModel data)
    {
        using var conn = await DataHelper.CreateConnection();
        using SqlCommand cmd = conn.CreateCommand("Garment_Add");
        cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
        cmd.Parameters.Add("@ExternalId", SqlDbType.UniqueIdentifier).Value = data.ExternalId;
        cmd.Parameters.Add("@GarmentTypeId", SqlDbType.TinyInt).Value = data.GarmentTypeId;
        cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 50).Value = data.Name;
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Graba los datos de una prenda
    /// </summary>
    /// <param name="userId">Identificador del usuario</param>
    /// <param name="data">Datos de la prenda</param>
    public static async Task<int> UpdateAsync(int userId, GarmentModel data)
    {
        using var conn = await DataHelper.CreateConnection();
        using SqlCommand cmd = conn.CreateCommand("Garment_Update");
        cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
        cmd.Parameters.Add("@ExternalId", SqlDbType.UniqueIdentifier).Value = data.ExternalId;
        cmd.Parameters.Add("@GarmentTypeId", SqlDbType.TinyInt).Value = data.GarmentTypeId;
        cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 50).Value = data.Name;
        return await cmd.ExecuteReturnInt32Async();
    }

    /// <summary>
    /// Devuelve las prendas de un usuario
    /// </summary>
    /// <param name="userId">Identificador del usuario</param>
    public static async Task<IEnumerable<GarmentModel>> ListAsync(int userId)
    {
        using var conn = await DataHelper.CreateConnection();
        using SqlCommand cmd = conn.CreateCommand("Garment_List");
        cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
        using var dr = await cmd.ExecuteReaderAsync();
        List<GarmentModel> list = [];
        while (await dr.ReadAsync())
            list.Add(new GarmentModel()
            {
                ExternalId = dr.GetGuid("ExternalId"),
                GarmentTypeId = dr.GetByte("GarmentTypeId"),
                Name = dr.GetString("Name")
            });
        return list;
    }

    /// <summary>
    /// Devuelve una prenda de un usuario
    /// </summary>
    /// <param name="userId">Identificador del usuario</param>
    /// <param name="externalId">Identificador externo de la prenda</param>
    /// <returns>Datos de la prenda o null si no existe</returns>
    public static async Task<GarmentModel?> GetAsync(int userId, Guid externalId)
    {
        using var conn = await DataHelper.CreateConnection();
        using SqlCommand cmd = conn.CreateCommand("Garment_Get");
        cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
        cmd.Parameters.Add("@ExternalId", SqlDbType.UniqueIdentifier).Value = externalId;
        using var dr = await cmd.ExecuteReaderAsync();
        if (!await dr.ReadAsync())
            return null;

        return new GarmentModel()
        {
            ExternalId = externalId,
            GarmentTypeId = dr.GetByte("GarmentTypeId"),
            Name = dr.GetString("Name")
        };
    }

    /// <summary>
    /// Devuelve si existe una prenda de un usuario
    /// </summary>
    /// <param name="userId">Identificador del usuario</param>
    /// <param name="externalId">Identificador externo de la prenda</param>
    /// <returns>True si existe, false si no</returns>
    public static async Task<bool> ExistsAsync(int userId, Guid externalId)
    {
        using var conn = await DataHelper.CreateConnection();
        using SqlCommand cmd = conn.CreateCommand("Garment_Exists");
        cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
        cmd.Parameters.Add("@ExternalId", SqlDbType.UniqueIdentifier).Value = externalId;
        return (await cmd.ExecuteReturnInt32Async()) == 1;
    }

    /// <summary>
    /// Elimina una prenda de un usuario
    /// </summary>
    /// <param name="userId">Identificador del usuario</param>
    /// <param name="externalId">Identificador externo de la prenda</param>
    /// <returns>Resultado de la eliminación</returns>
    internal static async Task<bool> DeleteAsync(int userId, Guid externalId)
    {
        using var conn = await DataHelper.CreateConnection();
        using SqlCommand cmd = conn.CreateCommand("Garment_Delete");
        cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
        cmd.Parameters.Add("@ExternalId", SqlDbType.UniqueIdentifier).Value = externalId;
        return (await cmd.ExecuteReturnInt32Async()) == 1;
    }
}