using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StyleMatch.Models;
using System.ComponentModel.DataAnnotations;

namespace StyleMatch.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UserController(ConfigurationModel config) : ControllerBase
{
    /// <summary>
    /// Datos para cambiar la contraseña
    /// </summary>
    public class UserDataPassword
    {
        /// <summary>
        /// Contraseña actual
        /// </summary>
        [Required]
        public string Current { get; set; }
        /// <summary>
        /// Nueva contraseña
        /// </summary>
        [Required]
        public string New { get; set; }
    }

    /// <summary>
    /// Creación de usuario
    /// </summary>
    /// <param name="data">Datos del usuario</param>
    /// <returns>Resultado de la creación</returns>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UserModel data)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState); // 400 con detalles

        if (string.IsNullOrWhiteSpace(data.Password))
            ModelState.AddModelError(nameof(data.Password), "La contraseña es requerida");

        data.UserId = null;
        data.RoleId = Role.User;

        int res = await Data.User.SaveAsync(data);

        return res switch
        {
            0 => Ok(await Auth.CreateTokenAsync(config, data.Email)),
            -1 => ValidationProblem("El usuario ya existe"),
            _ => StatusCode(500, "Error al crear el usuario"),
        };
    }

    /// <summary>
    /// Actualización de usuario
    /// </summary>
    /// <param name="data">Datos del usuario</param>
    /// <returns>Resultado de la actualización</returns>
    [HttpPut]
    [Authorize]
    public async Task<IActionResult> Update([FromBody] UserModel data)
    {
        data.UserId = HttpContext.GetUserId();
        data.RoleId = Role.User;

        int res = await Data.User.SaveAsync(data);

        return res switch
        {
            0 => Ok(),
            -1 => ValidationProblem("El usuario ya existe"),
            _ => StatusCode(500, "Error al actualizar el usuario"),
        };
    }

    /// <summary>
    /// Devuelve los datos del usuario
    /// </summary>
    /// <returns>Datos del usuario</returns>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetUserData() => Ok(await Data.User.GetAsync(HttpContext.GetUserId()));

    /// <summary>
    /// Cambio de contraseña
    /// </summary>
    /// <param name="data">Contraseñas</param>
    /// <returns>Resultado del cambio de contraseña</returns>
    [HttpPost("/password")]
    [Authorize]
    public async Task<IActionResult> UpdatePassword([FromBody] UserDataPassword data)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        bool result = await Data.User.UpdatePasswordAsync(HttpContext.GetUserId(), data.Current, data.New);
        if (!result)
            return Conflict("Error al cambiar la contraseña. Verifique que la contraseña actual sea correcta.");
        return Ok();
    }
}