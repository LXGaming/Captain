using System.Text.Json.Serialization;

namespace LXGaming.Captain.Configuration.Categories;

public class NotificationCategory {

    [JsonPropertyName("enabled")]
    public bool Enabled { get; init; } = true;

    [JsonPropertyName("url")]
    public string Url { get; init; } = "";

    [JsonPropertyName("username")]
    public string? Username { get; init; }

    [JsonPropertyName("avatarUrl")]
    public string? AvatarUrl { get; init; }

    [JsonPropertyName("mentions")]
    public HashSet<string> Mentions { get; init; } = [];
}