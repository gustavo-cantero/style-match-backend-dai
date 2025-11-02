using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace StyleMatch.Models;

/// <summary>
/// Entidad para la representación de una categoría
/// </summary>
public class CategoryModel
{
    /// <summary>
    /// Identificador externo de la categoría
    /// </summary>
    public Guid? ExternalId { get; set; }
    /// <summary>
    /// Nombre de la categoría
    /// </summary>
    [Required]
    public string Name { get; set; }
    /// <summary>
    /// Grupos de favoritos asociados a la categoría
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<FavouriteModel>? Favourites { get; set; }
}