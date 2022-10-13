using System.Text.Json.Serialization;

namespace LXGaming.Captain.Configuration.Categories.Docker;

public class HealthCategory {

    [JsonPropertyName("healthy")]
    public bool Healthy { get; init; } = false;

    [JsonPropertyName("unhealthy")]
    public bool Unhealthy { get; init; } = true;
}