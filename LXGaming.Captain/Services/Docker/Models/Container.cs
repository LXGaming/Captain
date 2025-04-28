using LXGaming.Captain.Triggers;

namespace LXGaming.Captain.Services.Docker.Models;

public class Container(
    string id,
    string name,
    IDictionary<string, string> labels,
    bool tty,
    TriggerBase restartTrigger) {

    public string Id { get; } = id;

    public string Name { get; } = name;

    public IDictionary<string, string> Labels { get; } = labels;

    public bool Tty { get; } = tty;

    public TriggerBase RestartTrigger { get; } = restartTrigger;
}