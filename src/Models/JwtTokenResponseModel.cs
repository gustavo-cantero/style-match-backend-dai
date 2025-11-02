namespace StyleMatch.Models;

/// <summary>
/// Entidad que devuelve el token
/// </summary>
public class JwtTokenResponseModel
{
    /// <summary>
    /// Token
    /// </summary>
    public string? Token { get; set; }

    public bool ExternalAuthMail { get; set; } = false;
}

