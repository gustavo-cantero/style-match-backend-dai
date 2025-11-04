using Microsoft.Data.SqlClient;
using StyleMatch.Helpers;
using StyleMatch.Models;
using System.Data;

namespace StyleMatch.Data;

/// <summary>
/// Clase para el manejo de la información de las categorías
/// </summary>
public static class Category
{
    /// <summary>
    /// Graba los datos de una categoría
    /// </summary>
    /// <param name="userId">Identificador del usuario</param>
    /// <param name="data">Datos de la categoría</param>
    public static async Task<int> AddAsync(int userId, CategoryModel data)
    {
        using var conn = await DataHelper.CreateConnection();
        using SqlCommand cmd = conn.CreateCommand("Category_Add");
        cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
        cmd.Parameters.Add("@ExternalId", SqlDbType.UniqueIdentifier).Value = data.ExternalId;
        cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 50).Value = data.Name;
        return await cmd.ExecuteReturnInt32Async();
    }

    /// <summary>
    /// Graba los datos de una categoría
    /// </summary>
    /// <param name="userId">Identificador del usuario</param>
    /// <param name="data">Datos de la categoría</param>
    public static async Task<int> UpdateAsync(int userId, CategoryModel data)
    {
        using var conn = await DataHelper.CreateConnection();
        using SqlCommand cmd = conn.CreateCommand("Category_Update");
        cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
        cmd.Parameters.Add("@ExternalId", SqlDbType.UniqueIdentifier).Value = data.ExternalId;
        cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 50).Value = data.Name;
        return await cmd.ExecuteReturnInt32Async();
    }

    /// <summary>
    /// Devuelve las categorías de un usuario
    /// </summary>
    /// <param name="userId">Identificador del usuario</param>
    public static async Task<IEnumerable<CategoryModel>> ListAsync(int userId)
    {
        using var conn = await DataHelper.CreateConnection();
        using SqlCommand cmd = conn.CreateCommand("Category_List");
        cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
        using var dr = await cmd.ExecuteReaderAsync();
        List<CategoryModel> list = [];
        while (await dr.ReadAsync())
            list.Add(new CategoryModel()
            {
                ExternalId = dr.GetGuid("ExternalId"),
                Name = dr.GetString("Name")
            });
        return list;
    }

    /// <summary>
    /// Devuelve una categoría de un usuario
    /// </summary>
    /// <param name="userId">Identificador del usuario</param>
    /// <param name="externalId">Identificador externo de la categoría</param>
    /// <returns>Datos de la categoría o null si no existe</returns>
    public static async Task<CategoryModel?> GetAsync(int userId, Guid externalId)
    {
        using var conn = await DataHelper.CreateConnection();
        using SqlCommand cmd = conn.CreateCommand("Category_Get");
        cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
        cmd.Parameters.Add("@ExternalId", SqlDbType.UniqueIdentifier).Value = externalId;
        using var dr = await cmd.ExecuteReaderAsync();
        if (!await dr.ReadAsync())
            return null;

        CategoryModel cat = new()
        {
            ExternalId = externalId,
            Name = dr.GetString("Name")
        };

        if (await dr.NextResultAsync() && await dr.ReadAsync())
        {
            List<FavouriteModel> favs = [];
            cat.Favourites = favs;
            do
            {
                favs.Add(new FavouriteModel()
                {
                    ExternalId = dr.GetGuid("ExternalId"),
                    Name = dr.GetString("Name")
                });
            } while (await dr.ReadAsync());
        }

        return cat;
    }

    /// <summary>
    /// Elimina una categoría de un usuario
    /// </summary>
    /// <param name="userId">Identificador del usuario</param>
    /// <param name="externalId">Identificador externo de la categoría</param>
    /// <returns>Resultado de la eliminación</returns>
    internal static async Task<bool> DeleteAsync(int userId, Guid externalId)
    {
        using var conn = await DataHelper.CreateConnection();
        using SqlCommand cmd = conn.CreateCommand("Category_Delete");
        cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
        cmd.Parameters.Add("@ExternalId", SqlDbType.UniqueIdentifier).Value = externalId;
        return (await cmd.ExecuteReturnInt32Async()) == 1;
    }
}