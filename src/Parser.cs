using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace AISlop
{
    public class Parser
    {
        public class Command
        {
            public string Thought { get; set; } = string.Empty;
            public string Tool { get; set; } = string.Empty;
            public Dictionary<string, string> Args { get; set; } = null!;
            public string Error { get; set; }

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
            public string Tool { get; set; } = string.Empty;

            [JsonPropertyName("args")]
            public Dictionary<string, string> Args { get; set; } = null!;
        }

        public class AIResponse
        {
            [JsonPropertyName("thought")]
            public string Thought { get; set; } = string.Empty;

            [JsonPropertyName("tool_calls")]
            public IEnumerable<ToolCall> ToolCalls { get; set; } = null!;
        }

        public static IEnumerable<Command> Parse(string response)
        {
            string jsonCommand = ExtractJson(response);
            if (jsonCommand.StartsWith("Exception:"))
                return new List<Command>() { new() { Error = jsonCommand } };

            try
            {
                var aiResponse = JsonSerializer.Deserialize<AIResponse>(jsonCommand);
                if (aiResponse is null || aiResponse.ToolCalls.Count() == 0)
                    return new List<Command>() { new() { Error = "Exception: No jsons found in the response!" } };

                return aiResponse.ToolCalls
                    .Select(tc => new Command
                    {
                        Thought = aiResponse.Thought,
                        Tool = tc.Tool,
                        Args = tc.Args
                    })
                    .ToList();
            }
            catch (JsonException ex)
            {
                return new List<Command>() { new() { Error = ex.Message } };
            }
        }

        public static string ExtractJson(string rawResponse)
        {
            if (string.IsNullOrWhiteSpace(rawResponse))
                return "Exception: Response was empty!";

            var matches = Regex.Matches(
                rawResponse,
                @"\{(?:[^{}]|(?<open>\{)|(?<-open>\}))*\}(?(open)(?!))",
                RegexOptions.Singleline
            );

            if (matches.Count == 0)
                return "Exception: No jsons found in the response!";

            var bestMatch = matches
                .OrderByDescending(m => m.Value.Length)
                .First()
                .Value
                .Trim();

            try
            {
                JsonDocument.Parse(bestMatch);
                return bestMatch;
            }
            catch (JsonException ex)
            {
                return $"Exception: {ex.Message}";
            }
        }
    }
}
