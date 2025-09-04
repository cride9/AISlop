using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AISlop
{
    public class Parser
    {
        public class Command
        {
            public string Thought { get; set; }
            public string Tool { get; set; }
            public Dictionary<string, string> Args { get; set; }

            public override string ToString()
            {
                StringBuilder sb = new();
                sb.AppendLine($"Tool: {Tool}");
                sb.AppendLine($"Thought: {Thought}");
                sb.AppendLine("Args:");
                foreach (var item in Args)
                {
                    sb.AppendLine($"  {item.Key}: {item.Value}");
                }
                return sb.ToString();
            }
        }

        public class ToolCall
        {
            [JsonPropertyName("tool")]
            public string Tool { get; set; }

            [JsonPropertyName("args")]
            public Dictionary<string, string> Args { get; set; }
        }

        public class AIResponse
        {
            [JsonPropertyName("thought")]
            public string Thought { get; set; }

            [JsonPropertyName("tool_call")]
            public ToolCall ToolCall { get; set; }
        }

        public static Command Parse(string response)
        {
            string? jsonCommand = ExtractJson(response);
            if (string.IsNullOrEmpty(jsonCommand))
                return null!;

            var aiResponse = JsonSerializer.Deserialize<AIResponse>(jsonCommand);
            if (aiResponse?.ToolCall == null)
                return null!;

            return new Command
            {
                Thought = aiResponse.Thought,
                Tool = aiResponse.ToolCall.Tool,
                Args = aiResponse.ToolCall.Args
            };
        }

        public static string? ExtractJson(string rawResponse)
        {
            int startIndex = rawResponse.IndexOf('{');
            if (startIndex == -1)
                return null;

            int endIndex = rawResponse.LastIndexOf('}');
            if (endIndex == -1)
                return null;

            if (endIndex < startIndex)
                return null;

            string jsonSubstring = rawResponse.Substring(startIndex, endIndex - startIndex + 1);

            try
            {
                JsonDocument.Parse(jsonSubstring);
                return jsonSubstring;
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
