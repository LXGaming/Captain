using System.Text.Json.Serialization;
using LXGaming.Captain.Configuration.Categories.Docker;

namespace LXGaming.Captain.Configuration.Categories;

public class DockerCategory {

    [JsonPropertyName("health")]
    public HealthCategory HealthCategory { get; init; } = new();

    [JsonPropertyName("logs")]
    public List<LogCategory> LogCategories { get; init; } = [];

    [JsonPropertyName("restart")]
    public RestartCategory RestartCategory { get; init; } = new();
}