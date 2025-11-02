using System.ComponentModel.DataAnnotations;

namespace StyleMatch.Models
{
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
        public IEnumerable<GarmentModel>? Garments { get; set; }
    }
}
