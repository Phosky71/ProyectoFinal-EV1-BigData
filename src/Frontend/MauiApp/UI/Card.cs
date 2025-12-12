namespace MauiApp.UI
{
    public class Card
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Name { get; set; }
        public string ManaCost { get; set; }
        public string Type { get; set; }
        public string Rarity { get; set; }
        public string SetName { get; set; }
        public string Text { get; set; }
        public string ImageUrl { get; set; }
    }
}