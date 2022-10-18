using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace LXGaming.Captain.Utilities.Json;

public class RegexConverter : JsonConverter<Regex> {

    public override Regex? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        return new Regex(reader.GetString()!, RegexOptions.Compiled);
    }

    public override void Write(Utf8JsonWriter writer, Regex value, JsonSerializerOptions options) {
        writer.WriteStringValue(value.ToString());
    }
}