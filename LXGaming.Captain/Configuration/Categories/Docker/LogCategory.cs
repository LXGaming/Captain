using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using LXGaming.Captain.Utilities.Json;

namespace LXGaming.Captain.Configuration.Categories.Docker;

public class LogCategory {

    [JsonPropertyName("label")]
    public string? Label { get; init; }

    [JsonPropertyName("names")]
    public ISet<string>? Names { get; init; }

    [JsonPropertyName("pattern")]
    [JsonConverter(typeof(RegexConverter))]
    public Regex? Regex { get; init; }

    [JsonPropertyName("replacement")]
    public string? Replacement { get; init; }
}