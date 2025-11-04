using Microsoft.IdentityModel.Tokens;
using StyleMatch.Data;
using StyleMatch.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace StyleMatch;

/// <summary>
/// Clase para el guardado de la información del usuario
/// </summary>
public static class Auth
{
    /// <summary>
    /// Crea un vector de <see cref="Claim"/> con los datos del usuario
    /// </summary>
    /// <param name="username">Usuario</param>
    /// <returns>Vector de <see cref="Claim"/> con los datos del usuario</returns>
    /// <exception cref="ArgumentNullException">Excepción en caso de no especificar el usuario</exception>
    internal static async Task<IEnumerable<Claim>> CreateClaimsAsync(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentNullException(nameof(username), "The username is mandatory");

        var user = await User.GetAsync(username) ?? throw new ArgumentNullException(nameof(username), "The user does not exist");

        return [
                new (ClaimTypes.Name, user.Name),
                new (ClaimTypes.Email, user.Email),
                new (ClaimTypes.Sid, user.UserId!.Value.ToString()),
                new (ClaimTypes.Role, user.RoleId.ToString()),
                new (ClaimTypes.UserData, JsonSerializer.Serialize(user))
            ];
    }

    /// <summary>
    /// Logea al usuario y devuelve un token
    /// </summary>
    /// <param name="config">Configuración de la aplicación</param>
    /// <param name="email">Identificador del usuario</param>
    /// <param name="password">Contraseña del usuario</param>
    /// <returns>Token nuevo</returns>
    public static async Task<JwtTokenResponseModel?> LoginAsync(ConfigurationModel config, string email, string password)
    {
        if (await User.AuthAsync(email, password))
            return await CreateTokenAsync(config, email);
        return null;
    }


    /// <summary>
    /// Login del usuario con una cuenta de Google y devuelve un token.
    /// </summary>
    /// <param name="config">Configuración de la aplicación</param>
    /// <param name="email">E-mail del usuario</param>
    /// <returns>Token nuevo</returns>
    public static async Task<JwtTokenResponseModel?> LoginWithGoogleAsync(ConfigurationModel config, string email)
    {
        var user = await User.GetAsync(email);
        if (user == null || !user.IsActive)
            return null;

        return await CreateTokenAsync(config, email);
    }


    /// <summary>
    /// Crea un token para un usuario
    /// </summary>
    /// <param name="config">Configuración de la aplicación</param>
    /// <param name="email">Identificador del usuario</param>
    /// <returns>Token nuevo</returns>
    public static async Task<JwtTokenResponseModel> CreateTokenAsync(ConfigurationModel config, string email)
    {
        var tokeOptions = new JwtSecurityToken(
            issuer: config.JWTValidIssuer,
            audience: config.JWTValidAudience,
            claims: await CreateClaimsAsync(email),
            expires: DateTime.Now.AddMinutes(config.JWTExpiresMinutes),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config.JWTSecret)),
                SecurityAlgorithms.HmacSha256));

        return new JwtTokenResponseModel { Token = new JwtSecurityTokenHandler().WriteToken(tokeOptions) };
    }

    /// <summary>
    /// Identificador del usuario logeado
    /// </summary>
    /// <param name="httpContext">Contexto de la llamada http</param>
    public static int GetUserId(this HttpContext httpContext) => int.Parse(httpContext.User.FindFirstValue(ClaimTypes.Sid.ToString()));

    /// <summary>
    /// Email del usuario logeado
    /// </summary>
    /// <param name="httpContext">Contexto de la llamada http</param>
    public static string GetEmail(this HttpContext httpContext) => httpContext.User.FindFirstValue(ClaimTypes.Email.ToString());


    /// <summary>
    /// Actualiza el token del usuario
    /// </summary>
    /// <param name="config">Configuración de la aplicación</param>
    /// <param name="email">Identificador del usuario</param>
    /// <param name="clientId">Identificador del cliente (para uso desde la api)</param>
    /// <returns>Token nuevo</returns>
    public static async Task<JwtTokenResponseModel?> RefreshTokenAsync(ConfigurationModel config, string email)
    {
        if (await User.ValidateAsync(email))
            return await CreateTokenAsync(config, email);
        return null;
    }
}