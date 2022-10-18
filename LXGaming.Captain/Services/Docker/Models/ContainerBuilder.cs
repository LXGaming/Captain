using LXGaming.Captain.Triggers;

namespace LXGaming.Captain.Services.Docker.Models;

public class ContainerBuilder {

    public string? Id { get; set; }
    public string? Name { get; set; }
    public IDictionary<string, string>? Labels { get; set; }
    public bool? Tty { get; set; }
    public TriggerBase? RestartTrigger { get; set; }

    public Container Build() {
        if (Id == null) { throw new InvalidOperationException(nameof(Id)); }
        if (Name == null) { throw new InvalidOperationException(nameof(Name)); }
        if (Labels == null) { throw new InvalidOperationException(nameof(Labels)); }
        if (Tty == null) { throw new InvalidOperationException(nameof(Tty)); }
        if (RestartTrigger == null) { throw new InvalidOperationException(nameof(RestartTrigger)); }

        return new Container(
            Id,
            Name,
            Labels,
            Tty.Value,
            RestartTrigger);
    }

    public ContainerBuilder WithId(string? id) {
        Id = id;
        return this;
    }

    public ContainerBuilder WithName(string? name) {
        Name = name;
        return this;
    }

    public ContainerBuilder WithLabels(IDictionary<string, string>? labels) {
        Labels = labels;
        return this;
    }

    public ContainerBuilder WithTty(bool? tty) {
        Tty = tty;
        return this;
    }

    public ContainerBuilder WithRestartTrigger(TriggerBase? restartTrigger) {
        RestartTrigger = restartTrigger;
        return this;
    }
}