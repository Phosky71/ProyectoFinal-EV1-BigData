using System;
using System.Collections.Generic;

namespace Frontend.WPFApp.Models
{
    public class MCPQueryResponse
    {
        public string Query { get; set; } = string.Empty;
        public string Response { get; set; } = string.Empty;
        public string RouterUsed { get; set; } = string.Empty;
        public int ResultCount { get; set; }
        public long ExecutionTimeMs { get; set; }
        public List<Card> Data { get; set; } = new List<Card>();
        public DateTime Timestamp { get; set; }
    }
}
