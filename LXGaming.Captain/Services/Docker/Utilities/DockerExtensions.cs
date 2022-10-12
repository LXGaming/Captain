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

    public static string? GetImage(this Actor actor) {
        return actor.GetAttributeValue("image");
    }

    public static string? GetName(this Actor actor) {
        return actor.GetAttributeValue("name");
    }

    private static string? GetAttributeValue(this Actor actor, string key) {
        return actor.Attributes.TryGetValue(key, out var value) ? value : null;
    }
}