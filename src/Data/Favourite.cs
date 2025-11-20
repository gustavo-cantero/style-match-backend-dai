using Microsoft.Data.SqlClient;
using StyleMatch.Helpers;
using StyleMatch.Models;
using System.Data;

namespace StyleMatch.Data;

/// <summary>
/// Clase para el manejo de la información de los favoritos
/// </summary>
public static class Favourite
{
    /// <summary>
    /// Graba los datos de un favorito
    /// </summary>
    /// <param name="userId">Identificador del usuario</param>
    /// <param name="data">Datos del favorito</param>
    /// <param name="isUpdate">Indica si es una actualización</param>
    /// <returns>Identificador del favorito o código de error</returns>
    public static async Task<int> SaveAsync(int userId, FavouriteModel data, bool isUpdate)
    {
        if (!(data.Garments?.Any() ?? false))
            throw new ArgumentOutOfRangeException(nameof(data), "No tiene favoritos seleccionados");

        using var conn = await DataHelper.CreateConnection();
        using SqlCommand cmd = conn.CreateCommand(isUpdate ? "Favourite_Update" : "Favourite_Add");
        cmd.Parameters.Add("@ExternalId", SqlDbType.UniqueIdentifier).Value = data.ExternalId;
        cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
        cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 50).Value = data.Name;
        cmd.Parameters.Add("@CategoryExternalId", SqlDbType.UniqueIdentifier).Value = data.Category.ExternalId;
        cmd.Parameters.Add("@GarmentExternalIds", SqlDbType.VarChar, 1000).Value = string.Join(",", data.Garments.Select(g => g.ExternalId.ToString()));
        return await cmd.ExecuteReturnInt32Async();
    }

    /// <summary>
    /// Devuelve si existe un favorito de un usuario
    /// </summary>
    /// <param name="userId">Identificador del usuario</param>
    /// <param name="externalId">Identificador externo del favorito</param>
    /// <returns>True si existe, false si no</returns>
    public static async Task<bool> ExistsAsync(int userId, Guid externalId)
    {
        using var conn = await DataHelper.CreateConnection();
        using SqlCommand cmd = conn.CreateCommand("Favourite_Exists");
        cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
        cmd.Parameters.Add("@ExternalId", SqlDbType.UniqueIdentifier).Value = externalId;
        return (await cmd.ExecuteReturnInt32Async()) == 1;
    }

    /// <summary>
    /// Devuelve los favoritos de un usuario
    /// </summary>
    /// <param name="userId">Identificador del usuario</param>
    public static async Task<IEnumerable<FavouriteModel>> ListAsync(int userId)
    {
        using var conn = await DataHelper.CreateConnection();
        using SqlCommand cmd = conn.CreateCommand("Favourite_List");
        cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
        using var dr = await cmd.ExecuteReaderAsync();
        List<FavouriteModel> list = [];
        while (await dr.ReadAsync())
            list.Add(new FavouriteModel()
            {
                ExternalId = dr.GetGuid("ExternalId"),
                Name = dr.GetString("Name"),
                CategoryId = dr.GetGuid("CategoryId")
            });
        return list;
    }

    /// <summary>
    /// Devuelve un favorito de un usuario
    /// </summary>
    /// <param name="userId">Identificador del usuario</param>
    /// <param name="externalId">Identificador externo del favorito</param>
    /// <returns>Datos del favorito o null si no existe</returns>
    public static async Task<FavouriteModel?> GetAsync(int userId, Guid externalId)
    {
        using var conn = await DataHelper.CreateConnection();
        using SqlCommand cmd = conn.CreateCommand("Favourite_Get");
        cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
        cmd.Parameters.Add("@ExternalId", SqlDbType.UniqueIdentifier).Value = externalId;
        using var dr = await cmd.ExecuteReaderAsync();
        if (!await dr.ReadAsync())
            return null;

        /// Datos de favorito
        FavouriteModel fav = new()
        {
            ExternalId = externalId,
            Name = dr.GetString("Name")
        };

        /// Datos de la categoría
        if (await dr.NextResultAsync() && await dr.ReadAsync())
            fav.Category = new()
            {
                ExternalId = dr.GetGuid("ExternalId"),
                Name = dr.GetString("Name")
            };

        /// Datos de las prendas
        if (await dr.NextResultAsync() && await dr.ReadAsync())
        {
            List<GarmentModel> favs = [];
            fav.Garments = favs;
            do
            {
                favs.Add(new GarmentModel()
                {
                    ExternalId = dr.GetGuid("ExternalId"),
                    Name = dr.GetString("Name")
                });
            } while (await dr.ReadAsync());
        }

        return fav;
    }

    /// <summary>
    /// Elimina un favorito de un usuario
    /// </summary>
    /// <param name="userId">Identificador del usuario</param>
    /// <param name="externalId">Identificador externo del favorito</param>
    /// <returns>Resultado de la eliminación</returns>
    internal static async Task<bool> DeleteAsync(int userId, Guid externalId)
    {
        using var conn = await DataHelper.CreateConnection();
        using SqlCommand cmd = conn.CreateCommand("Favourite_Delete");
        cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
        cmd.Parameters.Add("@ExternalId", SqlDbType.UniqueIdentifier).Value = externalId;
        return (await cmd.ExecuteReturnInt32Async()) == 1;
    }
}