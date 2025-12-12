using System;
using System.ComponentModel.DataAnnotations;
using CsvHelper.Configuration.Attributes;

namespace Backend.Persistence.Models
{
    /// <summary>
    /// Modelo que representa una carta de Magic: The Gathering.
    /// Dataset de Kaggle: Magic: The Gathering Cards
    /// </summary>
    public class Card
    {
        /// <summary>
        /// ID único de la carta (GUID generado automáticamente).
        /// </summary>
        [Key]
        [Name("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Nombre de la carta (campo requerido).
        /// </summary>
        [Required(ErrorMessage = "Card name is required")]
        [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
        [Name("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Coste de maná de la carta (ej: "{3}{U}{U}").
        /// </summary>
        [StringLength(50)]
        [Name("mana_cost")]  // ? CAMBIADO: snake_case para coincidir con CSV
        public string? ManaCost { get; set; }

        /// <summary>
        /// Tipo de carta (ej: "Creature — Human Wizard", "Instant", "Land").
        /// </summary>
        [StringLength(200)]
        [Name("type")]
        public string? Type { get; set; }

        /// <summary>
        /// Rareza de la carta (Common, Uncommon, Rare, Mythic Rare).
        /// </summary>
        [StringLength(50)]
        [Name("rarity")]
        public string? Rarity { get; set; }

        /// <summary>
        /// Código del set/expansión (ej: "10E", "M21").
        /// </summary>
        [StringLength(10)]
        [Name("set")]  // ? AÑADIDO: código corto del set
        public string? Set { get; set; }

        /// <summary>
        /// Nombre del set/expansión a la que pertenece (ej: "Tenth Edition").
        /// </summary>
        [StringLength(200)]
        [Name("set_name")]  // ? CAMBIADO: snake_case para coincidir con CSV
        public string? SetName { get; set; }

        /// <summary>
        /// Texto de reglas de la carta.
        /// </summary>
        [StringLength(2000)]  // ? AUMENTADO: algunos textos son largos
        [Name("text")]
        public string? Text { get; set; }

        /// <summary>
        /// Poder de la criatura (solo para criaturas).
        /// </summary>
        [StringLength(10)]
        [Name("power")]
        public string? Power { get; set; }

        /// <summary>
        /// Resistencia de la criatura (solo para criaturas).
        /// </summary>
        [StringLength(10)]
        [Name("toughness")]
        public string? Toughness { get; set; }

        /// <summary>
        /// URL de la imagen de la carta.
        /// </summary>
        [Url(ErrorMessage = "Invalid URL format")]
        [StringLength(500)]
        [Name("image_url")]  // ? CAMBIADO: snake_case para coincidir con CSV
        public string? ImageUrl { get; set; }

        /// <summary>
        /// ID de Multiverse de Gatherer (identificador oficial de Wizards).
        /// NOTA: Este campo existe en el CSV pero no lo usaremos activamente.
        /// </summary>
        [StringLength(50)]
        [Name("multiverse_id")]  // ? CAMBIADO: snake_case
        [Ignore]  // ? OPCIONAL: ignorar si no lo necesitas
        public string? MultiverseId { get; set; }

        /// <summary>
        /// Fecha de creación del registro.
        /// </summary>
        [Ignore] // CsvHelper ignorará este campo
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Fecha de última actualización.
        /// </summary>
        [Ignore] // CsvHelper ignorará este campo
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Valida si la carta es una criatura.
        /// </summary>
        public bool IsCreature()
        {
            return Type?.Contains("Creature", StringComparison.OrdinalIgnoreCase) ?? false;
        }

        /// <summary>
        /// Obtiene el color/colores de la carta basándose en el coste de maná.
        /// </summary>
        public string GetColors()
        {
            if (string.IsNullOrEmpty(ManaCost)) return "Colorless";

            var colors = new List<string>();
            if (ManaCost.Contains("{W}")) colors.Add("White");
            if (ManaCost.Contains("{U}")) colors.Add("Blue");
            if (ManaCost.Contains("{B}")) colors.Add("Black");
            if (ManaCost.Contains("{R}")) colors.Add("Red");
            if (ManaCost.Contains("{G}")) colors.Add("Green");

            return colors.Count == 0 ? "Colorless" : string.Join(", ", colors);
        }

        /// <summary>
        /// Obtiene una representación corta del texto (primeras 100 caracteres).
        /// </summary>
        public string GetShortText()
        {
            if (string.IsNullOrEmpty(Text)) return "";

            return Text.Length > 100
                ? Text.Substring(0, 97) + "..."
                : Text;
        }

        /// <summary>
        /// Obtiene el poder/resistencia formateado (ej: "4/4").
        /// </summary>
        public string GetPowerToughness()
        {
            if (string.IsNullOrEmpty(Power) || string.IsNullOrEmpty(Toughness))
                return "";

            return $"{Power}/{Toughness}";
        }
    }
}
