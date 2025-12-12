using System.Collections.Generic;
using Frontend.WPFApp.Models;

namespace Frontend.WPFApp.Validators
{
    public static class CardValidator
    {
        public static List<string> Validate(Card card)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(card.Name))
                errors.Add("El nombre es obligatorio");

            if (string.IsNullOrWhiteSpace(card.Type))
                errors.Add("El tipo es obligatorio");

            if (string.IsNullOrWhiteSpace(card.Rarity))
                errors.Add("La rareza es obligatoria");

            if (string.IsNullOrWhiteSpace(card.Set))
                errors.Add("El set es obligatorio");

            return errors;
        }
    }
}
