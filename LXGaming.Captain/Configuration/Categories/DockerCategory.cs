using System.Text.Json.Serialization;
using LXGaming.Captain.Configuration.Categories.Docker;

namespace LXGaming.Captain.Configuration.Categories;

public class DockerCategory {

    [JsonPropertyName("restart")]
    public RestartCategory RestartCategory { get; init; } = new();
}