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

    /// <summary>
    /// Servidor SMTP para el envío de correos
    /// </summary>
    public string SmtpServer { get; set; }
    /// <summary>
    /// Puerto del servidor SMTP
    /// </summary>
    public int SmtpPort { get; set; }
    /// <summary>
    /// Usuario del servidor SMTP
    /// </summary>
    public string SmtpUser { get; set; }
    /// <summary>
    /// Contraseña del servidor SMTP
    /// </summary>
    public string SmtpPassword { get; set; }
    /// <summary>
    /// Correo desde el cual se envían los emails
    /// </summary>
    public string SmtpFrom { get; set; }

    /// <summary>
    /// Api Key de OpenAI
    /// </summary>
    public string OpenAIKey { get; set; }

    //Las siguientes lineas son para poder tener el botón de log in with Google
    public string GoogleClientId { get; set; } = string.Empty;
    public string GoogleClientSecret { get; set; } = string.Empty;


}

