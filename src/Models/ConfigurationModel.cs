namespace StyleMatch.Models;

/// <summary>
/// Configuración de la aplicación
/// </summary>
public class ConfigurationModel
{
    /// <summary>
    /// Audencia válida para el JWT
    /// </summary>
    public string? JWTValidAudience { get; set; }
    /// <summary>
    /// Issuer válido para el JWT
    /// </summary>
    public string? JWTValidIssuer { get; set; }
    /// <summary>
    /// Secret para el JWT
    /// </summary>
    public string JWTSecret { get; set; }
    /// <summary>
    /// Minutos de expiración para el JWT
    /// </summary>
    public int JWTExpiresMinutes { get; set; } = 30;
}

