using System;

namespace Backend.API.Models
{
    public class Card
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public string ManaCost { get; set; }
        public string Type { get; set; }
        public string Rarity { get; set; }
        public string SetName { get; set; }
        public string Text { get; set; }
        public string Power { get; set; }
        public string Toughness { get; set; }
        public string ImageUrl { get; set; }
        public string MultiverseId { get; set; }
    }
}
