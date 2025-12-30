using LXGaming.Captain.Triggers;

namespace LXGaming.Captain.Services.Docker.Models;

public class Container {

    public required string Id { get; init; }

    public required string Name { get; set; }

    public required IDictionary<string, string> Labels { get; init; }

    public required bool Tty { get; init; }

    public required TriggerBase RestartTrigger { get; init; }
}