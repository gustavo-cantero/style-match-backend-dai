using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StyleMatch.Models;
using System.Text.Json;
using io = System.IO;

namespace StyleMatch.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class GarmentController : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(2 * 1024 * 1024)] //2MB
    [RequestFormLimits(MultipartBodyLengthLimit = 1024L * 1024 * 2, MultipartHeadersLengthLimit = 1024 * 1024)]
    public async Task<IActionResult> Upload()
    {
        //Validaciones de los datos
        if (!Request.Form.Files.Any() || !Request.Form.ContainsKey("data"))
            return BadRequest();

        GarmentModel? garment;
        try
        {
            garment = JsonSerializer.Deserialize<GarmentModel>(
                Request.Form["data"],
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
        }
        catch
        {
            return BadRequest();
        }

        if (garment == null)
            return BadRequest();

        if (string.IsNullOrWhiteSpace(garment.Name))
            return ValidationProblem("Debe especificar un nombre");

        //Leo y cambio de tamaño la imagen
        garment.ExternalId = Guid.CreateVersion7();

        var file = Request.Form.Files[0];

        // Creo la carpeta si no existe
        var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "Garment");
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        string fileImg = Path.Combine(folderPath, garment.ExternalId.ToString()!);
        string fileThumb = $"{fileImg}.thumb";

        try
        {
            // Guardo el archivo en disco
            using (var mainStream = file.OpenReadStream())
            ImageResizer.ResizeToJpeg(mainStream, Const.MAX_GARMENT_IMAGE_SIZE, fileImg, Const.JPEG_QUALITY);

            // Vuelvo a abrir el stream para crear el thumbnail
            using (var thumbStream = file.OpenReadStream())
            ImageResizer.CreateThumb(thumbStream, Const.MAX_GARMENT_THUMB_SIZE, fileThumb, Const.JPEG_QUALITY);

            //Guardo los datos en la base
            await Data.Garment.AddAsync(HttpContext.GetUserId(), garment);

            return Ok(garment);
        }
        catch
        {
            if (io.File.Exists(fileImg))
                io.File.Delete(fileImg);
            if (io.File.Exists(fileThumb))
                io.File.Delete(fileThumb);

            return StatusCode(500, "Server side error");
        }
    }

    /// <summary>
    /// Actualización de una prenda
    /// </summary>
    /// <param name="data">Datos de la prenda</param>
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] GarmentModel data)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        int res = await Data.Garment.UpdateAsync(HttpContext.GetUserId(), data);
        return res switch
        {
            1 => Ok(),
            0 => NotFound(),
            -1 => ValidationProblem("No existe el tipo de prenda"),
            _ => StatusCode(500, "Error al actualizar la prenda"),
        };
    }

    /// <summary>
    /// Lista las prendas de un usuario
    /// </summary>
    /// <returns>Lista de las prendas del usuario</returns>
    [HttpGet]
    public async Task<IActionResult> List()
    {
        var dt = await Data.Garment.ListAsync(HttpContext.GetUserId());
        return Ok(dt);
    }

    /// <summary>
    /// Devuelve los datos de una prenda
    /// </summary>
    /// <param name="externalId">Identificador de la prenda</param>
    /// <returns>Datos de la prenda</returns>
    [HttpGet("{externalId:guid}")]
    public async Task<IActionResult> Get(Guid externalId)
    {
        var dt = await Data.Garment.GetAsync(HttpContext.GetUserId(), externalId);
        if (dt == null)
            return NotFound();
        return Ok(dt);
    }

    /// <summary>
    /// Devuelve la imagen de una prenda
    /// </summary>
    /// <param name="externalId">Identificador de la prenda</param>
    /// <returns>Imagen de la prenda</returns>
    [HttpGet("{externalId:guid}/image")]
    public async Task<IActionResult> GetImage(Guid externalId)
    {
        if (!(await Data.Garment.ExistsAsync(HttpContext.GetUserId(), externalId)))
            return NotFound();

        string file = Path.Combine(Directory.GetCurrentDirectory(), "Garment", externalId.ToString());
        return PhysicalFile(file, "image/jpeg");
    }

    /// <summary>
    /// Devuelve la miniatura de una prenda
    /// </summary>
    /// <param name="externalId">Identificador de la prenda</param>
    /// <returns>Miniatura de la prenda</returns>
    [HttpGet("{externalId:guid}/thumb")]
    public async Task<IActionResult> GetThumb(Guid externalId)
    {
        if (!(await Data.Garment.ExistsAsync(HttpContext.GetUserId(), externalId)))
            return NotFound();

        string file = Path.Combine(Directory.GetCurrentDirectory(), "Garment", $"{externalId}.thumb");
        return PhysicalFile(file, "image/jpeg");
    }

    /// <summary>
    /// Elimina una prenda de un usuario
    /// </summary>
    /// <param name="externalId">Identificador de la prenda</param>
    [HttpDelete("{externalId:guid}")]
    public async Task<IActionResult> Delete(Guid externalId)
    {
        if (await Data.Garment.DeleteAsync(HttpContext.GetUserId(), externalId))
        {
            string file = Path.Combine(Directory.GetCurrentDirectory(), "Garment", externalId.ToString());
            if (io.File.Exists(file))
                io.File.Delete(file);
            file += ".thumb";
            if (io.File.Exists(file))
                io.File.Delete(file);
            return Ok();
        }
        return NotFound();
    }
}