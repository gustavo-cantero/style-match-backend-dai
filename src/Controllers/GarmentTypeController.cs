using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StyleMatch.Models;

namespace StyleMatch.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class GarmentTypeController : ControllerBase
{
    /// <summary>
    /// Crea un nuevo tipo de prenda (por ejemplo: superior, inferior, calzado)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Add([FromBody] GarmentTypeModel data)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        if (string.IsNullOrWhiteSpace(data.Name))
            return ValidationProblem("El nombre del tipo de prenda es obligatorio.");

        int res = await Data.GarmentType.AddAsync(data);

        return res switch
        {
            1 => Ok("Tipo de prenda creado correctamente."),
            -1 => ValidationProblem("Ya existe un tipo de prenda con el mismo nombre."),
            _ => StatusCode(500, "Error al crear el tipo de prenda.")
        };
    }

    /// <summary>
    /// Actualiza un tipo de prenda existente.
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] GarmentTypeModel data)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        if (data.GarmentTypeId == 0)
            return ValidationProblem("El ID del tipo de prenda es obligatorio.");

        if (string.IsNullOrWhiteSpace(data.Name))
            return ValidationProblem("El nombre del tipo de prenda es obligatorio.");

        int res = await Data.GarmentType.UpdateAsync(data);

        return res switch
        {
            1 => Ok("Tipo de prenda actualizado correctamente."),
            0 => NotFound("No se encontró el tipo de prenda especificado."),
            -1 => ValidationProblem("Ya existe otro tipo de prenda con el mismo nombre."),
            _ => StatusCode(500, "Error al actualizar el tipo de prenda.")
        };
    }

    /// <summary>
    /// Obtiene la lista completa de tipos de prenda.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List()
    {
        var list = await Data.GarmentType.ListAsync();
        return Ok(list);
    }

    /// <summary>
    /// Obtiene un tipo de prenda específico por su ID.
    /// </summary>
    [HttpGet("{garmentTypeId:int}")]
    public async Task<IActionResult> Get(int garmentTypeId)
    {
        var garmentType = await Data.GarmentType.GetAsync(garmentTypeId);
        if (garmentType == null)
            return NotFound("No se encontró el tipo de prenda especificado.");

        return Ok(garmentType);
    }

    /// <summary>
    /// Elimina un tipo de prenda.
    /// </summary>
    [HttpDelete("{garmentTypeId:int}")]
    public async Task<IActionResult> Delete(int garmentTypeId)
    {
        bool deleted = await Data.GarmentType.DeleteAsync(garmentTypeId);

        if (deleted)
            return Ok("Tipo de prenda eliminado correctamente.");

        return NotFound("No se encontró el tipo de prenda especificado.");
    }
}
