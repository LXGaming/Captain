using Humanizer;
using LXGaming.Captain.Triggers;

namespace LXGaming.Captain.Services.Docker.Models;

public class Container {

    public string ShortId => Id.Truncate(12, "");

    public readonly string Id;
    public readonly string Name;
    public readonly IDictionary<string, string> Labels;
    public readonly bool Tty;
    public readonly TriggerBase RestartTrigger;

    public Container(string id, string name, IDictionary<string, string> labels, bool tty, TriggerBase restartTrigger) {
        Id = id;
        Name = name;
        Labels = labels;
        Tty = tty;
        RestartTrigger = restartTrigger;
    }
}