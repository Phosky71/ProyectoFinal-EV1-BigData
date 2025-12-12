namespace Frontend.WPFApp.Models
{
    public class LoadKaggleResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int RecordsLoaded { get; set; }
        public string PersistenceMode { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
    }
}
