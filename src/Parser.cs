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
            string? jsonCommand = ExtractJson(response);
            if (string.IsNullOrEmpty(jsonCommand))
                return null!;

            if (jsonCommand.StartsWith("Exception"))
                return null!;

            try
            {

                var aiResponse = JsonSerializer.Deserialize<AIResponse>(jsonCommand);
                if (aiResponse is null || aiResponse.ToolCalls.Count() == 0)
                    return null!;

                return aiResponse.ToolCalls
                    .Select(tc => new Command
                    {
                        Thought = aiResponse.Thought,
                        Tool = tc.Tool,
                        Args = tc.Args
                    })
                    .ToList();
            }
            catch (Exception)
            {
                return null!;
            }
        }

        public static string? ExtractJson(string rawResponse)
        {
            if (string.IsNullOrWhiteSpace(rawResponse))
                return null;

            var matches = Regex.Matches(
                rawResponse,
                @"\{(?:[^{}]|(?<open>\{)|(?<-open>\}))*\}(?(open)(?!))",
                RegexOptions.Singleline
            );

            if (matches.Count == 0)
                return null;

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
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
