using System.Collections.Generic;

namespace Frontend.WPFApp.Models
{
    public class GetAllResponse
    {
        public int Count { get; set; }
        public string PersistenceMode { get; set; } = string.Empty;
        public List<Card> Data { get; set; } = new List<Card>();
    }
}
