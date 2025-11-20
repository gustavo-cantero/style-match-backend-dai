using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace StyleMatch.Models;

/// <summary>
/// Entidad para la representación de un grupo de favoritos
/// </summary>
public class FavouriteModel
{
    /// <summary>
    /// Identificador externo del grupo de favoritos
    /// </summary>
    public Guid? ExternalId { get; set; }
    /// <summary>
    /// Nombre del grupo de favoritos
    /// </summary>
    [Required]
    public string Name { get; set; }
    /// <summary>
    /// Prendas
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<GarmentModel>? Garments { get; set; }
    /// <summary>
    /// Datos de la categoría asociada
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CategoryModel Category { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? CategoryId { get; set; }
}