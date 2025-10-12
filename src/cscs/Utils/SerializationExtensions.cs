using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
using System.Xml.Serialization;

static class SerializationExtensions
{
    static JsonSerializerOptions jsonOptions = new()
    {
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip,  // allow comments
        AllowTrailingCommas = true,                      // allow commas after last element
        PropertyNameCaseInsensitive = true,              // handle case typos
        UnknownTypeHandling = JsonUnknownTypeHandling.JsonElement // tolerate unexpected value types
    };

    static SerializationExtensions()
    {
        jsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    public static T Deserialize<T>(this string data)
    {
        var serializer = new XmlSerializer(typeof(T));

        using var buffer = new StringReader(data);
        using var reader = XmlReader.Create(buffer);

        return (T)serializer.Deserialize(reader);
    }

    public static string Serialize(this object obj)
    {
        if (obj == null) return "";

        using var string_writer = new StringWriter();
        using var xml_writer = XmlWriter.Create(string_writer);

        var serializer = new XmlSerializer(obj.GetType());
        serializer.Serialize(xml_writer, obj);
        return string_writer.ToString();
    }

    public static string ToJson(this object obj, JsonSerializerOptions options = null)
        => JsonSerializer.Serialize(obj, options ?? SerializationExtensions.jsonOptions);

    public static T FromJson<T>(this string json, JsonSerializerOptions options = null)
        => JsonSerializer.Deserialize<T>(json, options ?? SerializationExtensions.jsonOptions);
}