using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StyleMatch.Models;

namespace StyleMatch.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CategoryController : ControllerBase
{
    /// <summary>
    /// Creación de una categoría
    /// </summary>
    /// <param name="data">Datos de la categoría</param>
    [HttpPost]
    public async Task<IActionResult> Add([FromBody] CategoryModel data)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        if (string.IsNullOrWhiteSpace(data.Name))
            return ValidationProblem("El nombre de la categoría es obligatorio");

        data.ExternalId = Guid.CreateVersion7();

        int res = await Data.Category.AddAsync(HttpContext.GetUserId(), data);
        return res switch
        {
            1 => Ok(data.ExternalId),
            -1 => ValidationProblem("Existe otra categoría con el mismo nombre"),
            _ => StatusCode(500, "Error al actualizar la categoría"),
        };
    }

    /// <summary>
    /// Actualización de una categoría
    /// </summary>
    /// <param name="data">Datos de la categoría</param>
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] CategoryModel data)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        if (!data.ExternalId.HasValue)
            return ValidationProblem("El identificador de la categoría es obligatorio");

        if (string.IsNullOrWhiteSpace(data.Name))
            return ValidationProblem("El nombre de la categoría es obligatorio");

        int res = await Data.Category.UpdateAsync(HttpContext.GetUserId(), data);
        return res switch
        {
            1 => Ok(data.ExternalId),
            0 => NotFound(),
            -1 => ValidationProblem("Existe otra categoría con el mismo nombre"),
            _ => StatusCode(500, "Error al actualizar la categoría"),
        };
    }

    /// <summary>
    /// Lista las categorías de un usuario
    /// </summary>
    /// <returns>Lista de las categorías del usuario</returns>
    [HttpGet]
    public async Task<IActionResult> List() => Ok(await Data.Category.ListAsync(HttpContext.GetUserId()));

    /// <summary>
    /// Devuelve los datos de una categoría
    /// </summary>
    /// <param name="externalId">Identificador de la categoría</param>
    /// <returns>Datos de la categoría</returns>
    [HttpGet("{externalId:guid}")]
    public async Task<IActionResult> Get(Guid externalId)
    {
        var dt = await Data.Category.GetAsync(HttpContext.GetUserId(), externalId);
        if (dt == null)
            return NotFound();
        return Ok(dt);
    }

    /// <summary>
    /// Elimina una categoría de un usuario
    /// </summary>
    /// <param name="externalId">Identificador de la categoría</param>
    [HttpDelete("{externalId:guid}")]
    public async Task<IActionResult> Delete(Guid externalId)
    {
        if (await Data.Category.DeleteAsync(HttpContext.GetUserId(), externalId))
            return Ok();
        return NotFound();
    }
}