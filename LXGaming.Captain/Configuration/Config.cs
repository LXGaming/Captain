using System.Text.Json.Serialization;
using LXGaming.Captain.Configuration.Categories;

namespace LXGaming.Captain.Configuration;

public class Config {

    [JsonPropertyName("docker")]
    public DockerCategory DockerCategory { get; init; } = new();

    [JsonPropertyName("notification")]
    public NotificationCategory NotificationCategory { get; init; } = new();
}