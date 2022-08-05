using System.Globalization;

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

    public static T FromString(string value) {
        return (T) Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
    }

    public static string ToString(T value) {
        return value.ToString(CultureInfo.InvariantCulture);
    }
}