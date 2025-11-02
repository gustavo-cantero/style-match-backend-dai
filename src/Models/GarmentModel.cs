using System.ComponentModel.DataAnnotations;

namespace StyleMatch.Models;

/// <summary>
/// Entidad para la representación de una prenda
/// </summary>
public class GarmentModel
{
    /// <summary>
    /// Identificador externo de la prenda
    /// </summary>
    public Guid? ExternalId { get; set; }
    /// <summary>
    /// Nombre de la prenda
    /// </summary>
    [Required]
    public string Name { get; set; }
    /// <summary>
    /// Identificador del tipo de prenda
    /// </summary>
    [Required]
    public byte GarmentTypeId { get; set; }
}