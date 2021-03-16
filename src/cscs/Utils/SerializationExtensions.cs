using System.IO;
using System.Xml;
using System.Xml.Serialization;

static class SerializationExtensions
{
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
}