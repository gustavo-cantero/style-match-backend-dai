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
        using var conn = await DataHelper.CreateConnection();
        var tran = (SqlTransaction)await conn.BeginTransactionAsync();
        try
        {
            using SqlCommand cmd = conn.CreateCommand(isUpdate ? "Favourite_Update" : "Favourite_Add");
            cmd.Transaction = tran;
            cmd.Parameters.Add("@ExternalId", SqlDbType.UniqueIdentifier).Value = data.ExternalId;
            cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
            cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 50).Value = data.Name;
            cmd.Parameters.Add("@CategoryExternalId", SqlDbType.UniqueIdentifier).Value = data.Category.ExternalId;
            int res = await cmd.ExecuteReturnInt32Async();

            cmd.Parameters.Clear();
            cmd.Parameters.Add("@FavouriteId", SqlDbType.Int).Value = res;

            if (isUpdate)
            {
                //Si es un update, elimino las prendas anteriores
                cmd.CommandText = "Favourite_DeleteGarments";
                await cmd.ExecuteNonQueryAsync();
            }

            //Si res < 0 es un error
            if (res > 0 && data.Garments != null)
            {
                cmd.CommandText = "Favourite_AddGarment";
                var p = cmd.Parameters.Add("@GarmentExternalId", SqlDbType.UniqueIdentifier);
                //Agrego las prendas
                foreach (var garment in data.Garments)
                {
                    p.Value = garment.ExternalId;
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            await tran.CommitAsync();
            return 1;
        }
        catch
        {
            await tran.RollbackAsync();
            throw;
        }
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
                Name = dr.GetString("Name")
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