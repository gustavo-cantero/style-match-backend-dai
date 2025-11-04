using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StyleMatch.Models;
using Microsoft.Data.SqlClient;
using StyleMatch.Data;
using StyleMatch.Helpers;
using System.Data;
using System.Net;
using System.Net.Mail;
using StyleMatch.Helpers;
using Google.Apis.Auth; 


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

    /// <summary>
    /// Send an e-mail with a code to change the password.
    /// </summary>
    /// <param name="data">Correo electrónico del usuario</param>
    /// <returns>Resultado del envío del código</returns>
    [HttpPost("recovery/send-code")]
    public async Task<IActionResult> SendRecoveryCode([FromBody] ForgotPasswordModel data)
    {
        if (data == null || string.IsNullOrWhiteSpace(data.Email))
            return BadRequest("Debe ingresar un correo electrónico.");

        // Verifico si el usuario existe
        var user = await Data.User.GetAsync(data.Email);
        if (user == null || !user.IsActive)
            return NotFound("Usuario no encontrado o inactivo.");

        // Genero un código aleatorio de 6 dígitos
        var random = new Random();
        var code = random.Next(100000, 999999).ToString();

        // Actualizo el código y la expiración en la base de datos
        using (var conn = await DataHelper.CreateConnection())
        using (var cmd = conn.CreateCommand("User_UpdateRecoveryCode", CommandType.StoredProcedure))
        {
            cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = user.UserId;
            cmd.Parameters.Add("@RecoveryCode", SqlDbType.NVarChar, 10).Value = code;
            cmd.Parameters.Add("@ExpiresOn", SqlDbType.DateTime2).Value = DateTime.UtcNow.AddMinutes(15);
            await cmd.ExecuteNonQueryAsync();
        }

        // Configuración del cliente SMTP
        using var smtp = new SmtpClient(config.SmtpServer, config.SmtpPort)
        {
            Credentials = new NetworkCredential(config.SmtpUser, config.SmtpPassword),
            EnableSsl = true
        };

        // Armo el mensaje
        var message = new MailMessage
        {
            From = new MailAddress(config.SmtpFrom),
            Subject = "Recuperación de contraseña - StyleMatch",
            Body = $"Hola {user.Name},\n\nTu código de recuperación de contraseña es: {code}\n" +
                "Este código expira en 15 minutos.\n\n" +
                "Si no solicitaste un cambio de contraseña, ignorá este mensaje.",
            IsBodyHtml = false
        };
        message.To.Add(user.Email);

        // Ahora sí, envío del correo
        await smtp.SendMailAsync(message);

        return Ok("El código de recuperación fue enviado correctamente al correo.");
    }

    /// <summary>
    /// For changing the password using the verification code received through e-mail.
    /// </summary>
    [HttpPost("recovery/reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel data)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await Data.User.GetAsync(data.Email);
        if (user == null || !user.IsActive)
            return NotFound("Usuario no encontrado o inactivo.");

        // Verifico código y expiración
        using (var conn = await DataHelper.CreateConnection())
        using (var cmd = conn.CreateCommand("User_GetRecoveryData", CommandType.StoredProcedure))
        {
            cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = user.UserId;
            using var dr = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow);
            if (!await dr.ReadAsync())
                return BadRequest("No hay código de recuperación activo.");

            var storedCode = dr.GetString("RecoveryCode");
            var expiresOn = dr.GetDateTime("ExpiresOn");

            if (!string.Equals(storedCode, data.Code, StringComparison.Ordinal))
                return BadRequest("El código ingresado es incorrecto.");

            if (DateTime.UtcNow > expiresOn)
                return BadRequest("El código ha expirado. Solicitá uno nuevo.");
        }

        // Actualizo la contraseña
        var updated = await Data.User.UpdatePasswordAsync(user.UserId!.Value, data.NewPassword);
        if (!updated)
            return StatusCode(500, "No se pudo actualizar la contraseña.");

        // Limpio el código de recuperación en la DB
        using (var conn = await DataHelper.CreateConnection())
        using (var cmd = conn.CreateCommand("User_ClearRecoveryCode", CommandType.StoredProcedure))
        {
            cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = user.UserId;
            await cmd.ExecuteNonQueryAsync();
        }

        return Ok("La contraseña fue actualizada correctamente.");
    }

    [HttpPost("google")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginModel data)
    {
        if (data == null || string.IsNullOrWhiteSpace(data.IdToken))
            return BadRequest("Token de Google inválido.");

        try
        {
            // Validar el token de Google
            var payload = await GoogleJsonWebSignature.ValidateAsync(
                data.IdToken,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { config.GoogleClientId }
                }
            );

            var email = payload.Email;
            var name = payload.Name ?? email;
            var picture = payload.Picture;
            //buscar el usuario
            Console.WriteLine($"Google payload email: {payload.Email}");
            //si no existe, crearlo
            var user = await Data.User.GetAsync(email);

            if (user == null)
            {
                var nuevo = new UserModel
                {
                    Name = name,
                    Email = email,
                    IsActive = true,
                    RoleId = Role.User
                };

                await Data.User.CreateFromGoogleAsync(nuevo);

                // Luego de crearlo, lo leemos de nuevo
                user = await Data.User.GetAsync(email);
            }


            // Generar JWT interno 
            var token = await Auth.LoginWithGoogleAsync(config, user.Email);
            if (token == null)
                return Unauthorized();

            return Ok(token);
        }
        catch (InvalidJwtException)
        {
            return Unauthorized("Token de Google inválido o expirado.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return StatusCode(500, "Error al procesar el inicio de sesión con Google.");
        }
    }



    
}