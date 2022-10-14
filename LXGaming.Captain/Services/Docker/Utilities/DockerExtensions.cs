using Docker.DotNet.Models;
using Humanizer;

namespace LXGaming.Captain.Services.Docker.Utilities;

public static class DockerExtensions {

    public static string? GetExitCode(this Actor actor) {
        return actor.GetAttributeValue("exitCode");
    }

    public static string GetId(this Actor actor) {
        return actor.ID.Truncate(12, "");
    }

    public static IDictionary<string, string> GetLabels(this Actor actor) {
        return actor.Attributes
            .Where(pair => !string.Equals(pair.Key, "image", StringComparison.OrdinalIgnoreCase))
            .Where(pair => !string.Equals(pair.Key, "name", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(pair => pair.Key, pair => pair.Value);
    }

    public static string? GetName(this Actor actor) {
        return actor.GetAttributeValue("name");
    }

    private static string? GetAttributeValue(this Actor actor, string key) {
        return actor.Attributes.TryGetValue(key, out var value) ? value : null;
    }

    public static string GetId(this ContainerListResponse containerListResponse) {
        return containerListResponse.ID.Truncate(12, "");
    }

    public static string? GetName(this ContainerListResponse containerListResponse) {
        var name = containerListResponse.Names.FirstOrDefault();
        return name?.StartsWith('/') ?? false ? name[1..] : name;
    }

    public static (string, string?) ParseAction(this Message message) {
        var split = message.Action.Split(':', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return split.Length switch {
            1 => (split[0], null),
            2 => (split[0], split[1]),
            _ => (message.Action, null)
        };
    }
}