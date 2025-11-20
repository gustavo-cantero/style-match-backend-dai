using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StyleMatch.Models;
using StyleMatch.Services;
using io = System.IO;

namespace StyleMatch.Controllers;

/// <summary>
/// Controller para el manejo de los favoritos
/// </summary>
/// <param name="config">Configuración de la aplicación</param>
[Route("api/[controller]")]
[ApiController]
[Authorize]
public class FavouriteController(ConfigurationModel config) : ControllerBase
{
    //#region Métodos privados

    /// <summary>
    /// Crea de nuevo la imagen del favorito
    /// </summary>
    /// <param name="data">Datos del favorito</param>
    private void CreateImage(FavouriteModel data)
    {
        _ = Task.Run(async () =>
        {

            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "Garment");

            var imgAI = await OutfitService.GenerateOutfitAsync(
                data.Garments.Select(g => Path.Combine(folderPath, g.ExternalId.ToString())),
                config.OpenAIKey
            );

            //Guardo la imagen completa
            folderPath = Path.Combine(Directory.GetCurrentDirectory(), "Favourites");
            string fileImg = Path.Combine(folderPath, data.ExternalId.ToString());
            using (FileStream fs = new(fileImg, FileMode.Create, FileAccess.Write))
            {
                await fs.WriteAsync(imgAI);
                fs.Close();
            }

            // Guardo el thumbnail
            string fileThumb = $"{fileImg}.thumb";
            using MemoryStream thumbStream = new(imgAI);
            ImageResizer.CreateThumb(thumbStream, Const.MAX_GARMENT_THUMB_SIZE, fileThumb, Const.JPEG_QUALITY);
        });
    }

    //#endregion


    /// <summary>
    /// Creación de un favorito
    /// </summary>
    /// <param name="data">Datos del favorito</param>
    [HttpPost]
    public async Task<IActionResult> Add([FromBody] FavouriteModel data)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        if (string.IsNullOrWhiteSpace(data.Name))
            return ValidationProblem("El nombre del favorito es obligatorio");

        if (!(data.Garments?.Any() ?? false))
            return ValidationProblem("Debe poseer al menos una prenda");

        data.ExternalId = Guid.CreateVersion7();

        int res = await Data.Favourite.SaveAsync(HttpContext.GetUserId(), data, false);

        if (res == 2) //Cambiaron las prendas asignadas
            CreateImage(data);

        return res switch
        {
            1 => Ok(data.ExternalId),
            2 => Ok(data.ExternalId),
            -1 => ValidationProblem("Existe otro outfit con el mismo nombre"),
            -2 => ValidationProblem("No existe la categoría"),
            -3 => ValidationProblem("Al menos una de las prendas no es del usuario"),
            _ => StatusCode(500, "Error al actualizar el outfit"),
        };
    }

    /// <summary>
    /// Actualización de un favorito
    /// </summary>
    /// <param name="data">Datos del favorito</param>
    /// <param name="externalId">Identificador del favorito</param>
    [HttpPut("{externalId:guid}")]
    public async Task<IActionResult> Update(Guid externalId, [FromBody] FavouriteModel data)
    {
        data.ExternalId = externalId;

        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        if (!data.ExternalId.HasValue)
            return ValidationProblem("El identificador del outfit es obligatorio");

        if (string.IsNullOrWhiteSpace(data.Name))
            return ValidationProblem("El nombre del outfit es obligatorio");

        if (!(data.Garments?.Any() ?? false))
            return ValidationProblem("Debe poseer al menos una prenda");

        int res = await Data.Favourite.SaveAsync(HttpContext.GetUserId(), data, true);

        if (res == 2) //Cambiaron las prendas asignadas
        {
            DeleteFavourite(externalId);
            CreateImage(data);
        }

        return res switch
        {
            1 => Ok(data.ExternalId),
            2 => Ok(data.ExternalId),
            0 => NotFound(),
            -1 => ValidationProblem("Existe otro outfit con el mismo nombre"),
            -2 => ValidationProblem("No existe la categoría"),
            -3 => ValidationProblem("Al menos una de las prendas no es del usuario"),
            _ => StatusCode(500, "Error al actualizar el outfit"),
        };
    }

    /// <summary>
    /// Lista los favoritos de un usuario
    /// </summary>
    /// <returns>Lista de los favoritos del usuario</returns>
    [HttpGet]
    public async Task<IActionResult> List() => Ok(await Data.Favourite.ListAsync(HttpContext.GetUserId()));

    /// <summary>
    /// Devuelve los datos de un favorito
    /// </summary>
    /// <param name="externalId">Identificador del favorito</param>
    /// <returns>Datos del favorito</returns>
    [HttpGet("{externalId:guid}")]
    public async Task<IActionResult> Get(Guid externalId)
    {
        var dt = await Data.Favourite.GetAsync(HttpContext.GetUserId(), externalId);
        if (dt == null)
            return NotFound();
        return Ok(dt);
    }

    /// <summary>
    /// Devuelve la imagen de un favorito
    /// </summary>
    /// <param name="externalId">Identificador del favorito</param>
    /// <returns>Imagen del favorito</returns>
    [HttpGet("{externalId:guid}/image")]
    public async Task<IActionResult> GetImage(Guid externalId)
    {
        if (!(await Data.Favourite.ExistsAsync(HttpContext.GetUserId(), externalId)))
            return NotFound();

        string file = Path.Combine(Directory.GetCurrentDirectory(), "Favourites", externalId.ToString());
        if (io.File.Exists(file))
            return PhysicalFile(file, "image/jpeg");
        return NoContent();
    }

    /// <summary>
    /// Devuelve la miniatura de un favorito
    /// </summary>s
    /// <param name="externalId">Identificador del favorito</param>
    /// <returns>Miniatura del favorito</returns>
    [HttpGet("{externalId:guid}/thumb")]
    public async Task<IActionResult> GetThumb(Guid externalId)
    {
        if (!(await Data.Favourite.ExistsAsync(HttpContext.GetUserId(), externalId)))
            return NotFound();

        string file = Path.Combine(Directory.GetCurrentDirectory(), "Favourites", $"{externalId}.thumb");
        if (io.File.Exists(file))
            return PhysicalFile(file, "image/jpeg");
        return NoContent();
    }

    /// <summary>
    /// Elimina un favorito de un usuario
    /// </summary>
    /// <param name="externalId">Identificador del favorito</param>
    [HttpDelete("{externalId:guid}")]
    public async Task<IActionResult> Delete(Guid externalId)
    {
        if (await Data.Favourite.DeleteAsync(HttpContext.GetUserId(), externalId))
        {
            DeleteFavourite(externalId);
            return Ok();
        }
        return NotFound();
    }

    /// <summary>
    /// Elimina los archivos de un favorito
    /// </summary>
    /// <param name="externalId">Identificador del favorito</param>
    private static void DeleteFavourite(Guid externalId)
    {
        string file = Path.Combine(Directory.GetCurrentDirectory(), "Favourites", externalId.ToString());
        if (io.File.Exists(file))
            io.File.Delete(file);
        file += ".thumb";
        if (io.File.Exists(file))
            io.File.Delete(file);
    }
}