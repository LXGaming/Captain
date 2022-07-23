using System.Text.Json.Serialization;
using LXGaming.Captain.Configuration.Categories;

namespace LXGaming.Captain.Configuration;

public class Config {

    [JsonPropertyName("discord")]
    public DiscordCategory DiscordCategory { get; init; } = new();

    [JsonPropertyName("docker")]
    public DockerCategory DockerCategory { get; init; } = new();
}