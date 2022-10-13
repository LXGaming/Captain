using System.Text.Json.Serialization;

namespace LXGaming.Captain.Configuration.Categories.Docker;

public class RestartCategory {

    public const int DefaultThreshold = 3;
    public const int DefaultTimeout = 60; // 1 Minute

    [JsonPropertyName("automaticStop")]
    public bool AutomaticStop { get; init; } = false;

    [JsonPropertyName("threshold")]
    public int Threshold { get; init; } = DefaultThreshold;

    [JsonPropertyName("timeout")]
    public int Timeout { get; init; } = DefaultTimeout;
}