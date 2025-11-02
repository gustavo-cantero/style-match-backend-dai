using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StyleMatch.Models;

namespace StyleMatch.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController(ConfigurationModel config) : ControllerBase
{
    /// <summary>
    /// Autenticación del usuario
    /// </summary>
    /// <param name="data">Datos del usuario</param>
    /// <returns>Resultado de la autenticación</returns>
    [HttpPost]
    public async Task<IActionResult> Login([FromBody] AuthModel data)
    {
        if (data == null || data.Email == null || data.Password == null)
            return BadRequest();

        var token = await Auth.LoginAsync(config, data.Email, data.Password);
        if (token == null)
            return Unauthorized();

        return Ok(token);
    }

    /// <summary>
    /// Returns a new token for the already logged in user.
    /// </summary>
    /// <returns>The new token</returns>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> RefreshToken()
    {
        var token = await Auth.RefreshTokenAsync(config, HttpContext.GetEmail());
        if (token == null)
            return Unauthorized();

        return Ok(token);
    }
}