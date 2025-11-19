using Google.Apis.Auth;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MimeKit;
using StyleMatch.Models;
using System.Security.Authentication;


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
        Random random = new();
        var code = random.Next(0, 999999).ToString().PadLeft(6, '0');

        // Guardo el código en la base de datos
        await Data.User.UpdateRecoveryCode(user.UserId!.Value, code);

        // Armo el mensaje
        using MimeMessage message = new()
        {
            Subject = "Recuperación de contraseña - StyleMatch",
            Body = new TextPart("plain")
            {
                Text = $"Hola {user.Name},\n\nTu código de recuperación de contraseña es: {code}\n" +
                "Este código expira en 15 minutos.\n\n" +
                "Si no solicitaste un cambio de contraseña, ignorá este mensaje."
            }
        };

        message.From.Add(new MailboxAddress("Style Match", config.SmtpFrom));
        message.To.Add(new MailboxAddress(user.Name, user.Email));

        // Conexión, autenticación y envío del correo
        using SmtpClient smtp = new() { SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13 };
        await smtp.ConnectAsync(
                config.SmtpServer,
                config.SmtpPort,
                SecureSocketOptions.StartTls
            );
        await smtp.AuthenticateAsync(config.SmtpUser, config.SmtpPassword);
        await smtp.SendAsync(message);

        return Ok("El código de recuperación fue enviado correctamente al correo");
    }

    /// <summary>
    /// For changing the password using the verification code received through e-mail.
    /// </summary>
    [HttpPost("recovery/reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel data)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Verifico código y expiración
        var res = await Data.User.CheckRecoveryCode(data.Email, data.Code);

        if (res == 1)
        {
            var user = await Data.User.GetAsync(data.Email);
            if (user == null || !user.IsActive)
                return NotFound("Usuario no encontrado o inactivo");
            var updated = await Data.User.UpdatePasswordAsync(user.UserId!.Value, data.NewPassword);
            if (!updated)
                return StatusCode(500, "No se pudo actualizar la contraseña");

            return Ok("La contraseña fue actualizada correctamente");
        }

        return res switch
        {
            -1 => BadRequest("El código ingresado es incorrecto"),
            -2 => BadRequest("El código ha expirado. Solicitá uno nuevo"),
            _ => StatusCode(500, "No se pudo actualizar la contraseña"),
        };
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