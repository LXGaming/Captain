using System.Text.Json.Serialization;

namespace LXGaming.Captain.Configuration.Categories; 

public class DockerCategory {

    public const int DefaultRestartThreshold = 3;
    public const int DefaultRestartTimeout = 60; // 1 Minute
    
    [JsonPropertyName("restartThreshold")]
    public int RestartThreshold { get; init; } = DefaultRestartThreshold;

    [JsonPropertyName("restartTimeout")]
    public int RestartTimeout { get; init; } = DefaultRestartTimeout;
}