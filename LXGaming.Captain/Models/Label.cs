namespace LXGaming.Captain.Models;

public class Label<T>(string id, string name, T defaultValue) where T : IConvertible {

    public string Id { get; } = id;

    public string Name { get; } = name;

    public T DefaultValue { get; } = defaultValue;
}