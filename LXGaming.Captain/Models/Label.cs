namespace LXGaming.Captain.Models;

public class Label<T> where T : IConvertible {

    public string Id { get; init; }

    public string Name { get; init; }

    public T DefaultValue { get; init; }

    public Label(string id, string name, T defaultValue) {
        Id = id;
        Name = name;
        DefaultValue = defaultValue;
    }
}