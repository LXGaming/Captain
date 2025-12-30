namespace LXGaming.Captain.Services.Docker.Models;

public readonly struct MessageAction(string key, string? value) {

    public string Key { get; } = key;

    public string? Value { get; } = value;

    public override string ToString() {
        return Value != null ? $"{Key}: {Value}" : Key;
    }
}