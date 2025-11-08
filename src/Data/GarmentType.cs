using Microsoft.Data.SqlClient;
using StyleMatch.Helpers;
using StyleMatch.Models;
using System.Data;

namespace StyleMatch.Data;

/// <summary>
/// Clase para el manejo de la información de los tipos de prenda
/// </summary>
public static class GarmentType
{
    /// <summary>
    /// Agrega un nuevo tipo de prenda
    /// </summary>
    public static async Task<int> AddAsync(GarmentTypeModel data)
    {
        using var conn = await DataHelper.CreateConnection();
        using SqlCommand cmd = conn.CreateCommand("sp_GarmentType_Add");
        cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 50).Value = data.Name;
        return await cmd.ExecuteReturnInt32Async();
    }

    /// <summary>
    /// Actualiza un tipo de prenda existente
    /// </summary>
    public static async Task<int> UpdateAsync(GarmentTypeModel data)
    {
        using var conn = await DataHelper.CreateConnection();
        using SqlCommand cmd = conn.CreateCommand("sp_GarmentType_Update");
        cmd.Parameters.Add("@GarmentTypeId", SqlDbType.Int).Value = data.GarmentTypeId;
        cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 50).Value = data.Name;
        return await cmd.ExecuteReturnInt32Async();
    }

    /// <summary>
    /// Devuelve la lista completa de tipos de prenda
    /// </summary>
    public static async Task<IEnumerable<GarmentTypeModel>> ListAsync()
    {
        using var conn = await DataHelper.CreateConnection();
        using SqlCommand cmd = conn.CreateCommand("sp_GarmentType_List");
        using var dr = await cmd.ExecuteReaderAsync();

        List<GarmentTypeModel> list = [];
        while (await dr.ReadAsync())
        {
            list.Add(new GarmentTypeModel()
            {
                GarmentTypeId = Convert.ToInt32(dr["GarmentTypeId"]),
                Name = dr.GetString("Name")
            });
        }
        return list;
    }

    /// <summary>
    /// Devuelve los datos de un tipo de prenda por ID
    /// </summary>
    public static async Task<GarmentTypeModel?> GetAsync(int garmentTypeId)
    {
        using var conn = await DataHelper.CreateConnection();
        using SqlCommand cmd = conn.CreateCommand("sp_GarmentType_Get");
        cmd.Parameters.Add("@GarmentTypeId", SqlDbType.Int).Value = garmentTypeId;
        using var dr = await cmd.ExecuteReaderAsync();

        if (!await dr.ReadAsync())
            return null;

        return new GarmentTypeModel()
        {
            GarmentTypeId = Convert.ToInt32(dr["GarmentTypeId"]),
            Name = dr.GetString("Name")
        };
    }

    /// <summary>
    /// Elimina un tipo de prenda por ID
    /// </summary>
    public static async Task<bool> DeleteAsync(int garmentTypeId)
    {
        using var conn = await DataHelper.CreateConnection();
        using SqlCommand cmd = conn.CreateCommand("sp_GarmentType_Delete");
        cmd.Parameters.Add("@GarmentTypeId", SqlDbType.Int).Value = garmentTypeId;
        return (await cmd.ExecuteReturnInt32Async()) == 1;
    }
}
