using System.Text.Json.Serialization;

namespace Backend.MCP.Protocol
{
    public class JsonRpcMessage
    {
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        [JsonPropertyName("id")]
        public object Id { get; set; } = 1;
    }

    public class JsonRpcRequest : JsonRpcMessage
    {
        [JsonPropertyName("method")]
        public string Method { get; set; } = string.Empty;

        [JsonPropertyName("params")]
        public object? Params { get; set; }
    }

    public class JsonRpcResponse : JsonRpcMessage
    {
        [JsonPropertyName("result")]
        public object? Result { get; set; }

        [JsonPropertyName("error")]
        public object? Error { get; set; }
    }

    public class McpTool
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("inputSchema")]
        public object? InputSchema { get; set; }
    }

    public class ToolListResult
    {
        [JsonPropertyName("tools")]
        public List<McpTool> Tools { get; set; } = new List<McpTool>();
    }

    public class CallToolParams
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("arguments")]
        public Dictionary<string, object> Arguments { get; set; } = new Dictionary<string, object>();
    }

    public class ToolContent
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "text";

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }
}