using Microsoft.Data.SqlClient;
using System.Data;

namespace StyleMatch.Helpers;

/// <summary>
/// Clase de ayuda para la gestión de datos
/// </summary>
public static class DataHelper
{
    /// <summary>
    /// Cadena de conexión a la base de datos
    /// </summary>
    public static string? ConnectionString { get; set; }

    /// <summary>
    /// Crea una nueva conexión a la base de datos
    /// </summary>
    /// <returns></returns>
    public static async Task<SqlConnection> CreateConnection()
    {
        SqlConnection conn = new(ConnectionString);
        await conn.OpenAsync();
        return conn;
    }

    /// <summary>
    /// Ejecuta un comando y devuelve el valor de retorno como entero
    /// </summary>
    /// <param name="cmd">Comando a ejecutar</param>
    /// <returns>Valor de retorno del comando como entero</returns>
    public static async Task<int> ExecuteReturnInt32Async(this SqlCommand cmd)
    {
        //Si el command no tiene un parámetro de retorno, se lo agrego
        SqlParameter param =
            cmd.Parameters.OfType<SqlParameter>().FirstOrDefault(p => p.Direction == ParameterDirection.ReturnValue)
            ?? cmd.Parameters.Add(new SqlParameter { Direction = ParameterDirection.ReturnValue });

        //Si no está conectado a la base, lo conecto
        if (cmd.Connection.State == ConnectionState.Closed || cmd.Connection.State == ConnectionState.Broken)
            await cmd.Connection.OpenAsync();
        await cmd.ExecuteNonQueryAsync();
        return (int)(param.Value ?? 0);
    }

    /// <summary>
    /// Crea un <see cref="SqlCommand"/>
    /// </summary>
    /// <param name="conn">Conexión</param>
    /// <param name="commandText">Texto del comando</param>
    /// <param name="commandType">Tipo de comando</param>
    /// <returns></returns>
    public static SqlCommand CreateCommand(this SqlConnection conn, string commandText, CommandType commandType)
    {
        var cmd = conn.CreateCommand();
        cmd.CommandText = commandText;
        cmd.CommandType = commandType;
        return cmd;
    }
}
