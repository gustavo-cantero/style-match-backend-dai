namespace StyleMatch.Models;

/// <summary>
/// Informción de autenticación
/// </summary>
public class AuthModel
{
    /// <summary>
    /// Email
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// Contraseña
    /// </summary>
    public required string Password { get; set; }
}
