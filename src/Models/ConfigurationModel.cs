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

    /// Las siguientes lienas son para poder enviar el mail de recuperación de contraseña
    public string SmtpServer { get; set; }
    public int SmtpPort { get; set; }
    public string SmtpUser { get; set; }
    public string SmtpPassword { get; set; }
    public string SmtpFrom { get; set; }

    //Las siguientes lineas son para poder tener el botón de log in with Google
    public string GoogleClientId { get; set; } = string.Empty;
    public string GoogleClientSecret { get; set; } = string.Empty;


}

