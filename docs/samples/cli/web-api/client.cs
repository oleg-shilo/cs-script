//css_ng csc 
using System;
using System.Text;
using System.IO;
using System.Text.Json;
using System.Net.Http;
using System.Net.Http.Headers;

var client = new HttpClient();
var data = client.GetStringAsync("http://localhost:5000/WeatherForecast").Result;

Console.WriteLine(data.ToPrettyJson());

static class Extensions
{
    public static string ToPrettyJson(this string json)
    {
        var doc = JsonDocument.Parse(json);

        using var stream = new MemoryStream();

        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
            doc.WriteTo(writer);

        return Encoding.UTF8.GetString(stream.ToArray());
    }
}