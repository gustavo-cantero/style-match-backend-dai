using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace StyleMatch.Models;

/// <summary>
/// Entidad para la representación de un usuario
/// </summary>
public class UserModel
{
    /// <summary>
    /// Identificador del usuario
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// Usuario
    /// </summary>
    [Required, EmailAddress]
    public string Email { get; set; }

    /// <summary>
    /// Nombre
    /// </summary>
    [Required, StringLength(50)]
    public string Name { get; set; }

    /// <summary>
    /// Roles del usuario
    /// </summary>
    [JsonIgnore]
    public Role RoleId { get; set; } = Role.User;

    /// <summary>
    /// Contraseña
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Password { get; set; }

    /// <summary>
    /// Establece si el usuario está activo
    /// </summary>
    public bool IsActive { get; set; } = true;

    public string? ProfileImage { get; set; }
    public string? Provider { get; set; }

}