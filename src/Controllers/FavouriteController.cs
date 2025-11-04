using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StyleMatch.Models;

namespace StyleMatch.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class FavouriteController : ControllerBase
{
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
        return res switch
        {
            1 => Ok(data.ExternalId),
            -1 => ValidationProblem("Existe otra categoría con el mismo nombre"),
            -2 => ValidationProblem("No existe la categoría"),
            _ => StatusCode(500, "Error al actualizar el favorito"),
        };
    }

    /// <summary>
    /// Actualización de un favorito
    /// </summary>
    /// <param name="data">Datos del favorito</param>
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] FavouriteModel data)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        if (!data.ExternalId.HasValue)
            return ValidationProblem("El identificador del favorito es obligatorio");

        if (string.IsNullOrWhiteSpace(data.Name))
            return ValidationProblem("El nombre del favorito es obligatorio");

        if (!(data.Garments?.Any() ?? false))
            return ValidationProblem("Debe poseer al menos una prenda");

        int res = await Data.Favourite.SaveAsync(HttpContext.GetUserId(), data, true);
        return res switch
        {
            1 => Ok(data.ExternalId),
            0 => NotFound(),
            -1 => ValidationProblem("Existe otra categoría con el mismo nombre"),
            _ => StatusCode(500, "Error al actualizar el favorito"),
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
    /// Elimina un favorito de un usuario
    /// </summary>
    /// <param name="externalId">Identificador del favorito</param>
    [HttpDelete("{externalId:guid}")]
    public async Task<IActionResult> Delete(Guid externalId)
    {
        if (await Data.Favourite.DeleteAsync(HttpContext.GetUserId(), externalId))
            return Ok();
        return NotFound();
    }
}