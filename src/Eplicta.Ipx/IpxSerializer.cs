using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Eplicta.Ipx;

/// <summary>Reads and writes IPX documents. Reading keeps everything in the text, including properties this library does not know, explicit nulls, property order and the text of numbers, so a document written back is the document that was read.</summary>
public static class IpxSerializer
{
    private const int SupportedMajorVersion = 1;

    private static readonly Regex VersionPattern = new(@"^(?<major>[0-9]+)\.[0-9]+\.[0-9]+$", RegexOptions.CultureInvariant);

    private static readonly JsonDocumentOptions ReadOptions = new()
    {
        CommentHandling = JsonCommentHandling.Disallow,
        AllowTrailingCommas = false
    };

    /// <summary>Reads a document from text. Throws <see cref="IpxFormatException"/> when the text is not an IPX document this library supports; it does not check the document against the schema, see IpxValidator for that.</summary>
    public static IpxDocument Parse(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        var repeated = FirstRepeatedProperty(json);
        if (repeated is not null) throw new IpxFormatException($"The property '{repeated}' occurs twice in one object.");

        JsonNode root;
        try
        {
            root = JsonNode.Parse(json, null, ReadOptions);
        }
        catch (Exception exception) when (exception is JsonException or ArgumentException)
        {
            throw new IpxFormatException("The text is not valid JSON.", exception);
        }

        return FromNode(root);
    }

    /// <summary>Reads a document from a stream of UTF-8 text. A byte-order mark is accepted.</summary>
    public static IpxDocument Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var reader = new StreamReader(stream, Encoding.UTF8, true, 4096, true);
        return Parse(reader.ReadToEnd());
    }

    /// <summary>Writes a document as indented JSON with two-space indentation and line feeds, and a line feed at the end. Only what JSON requires is escaped: quotes, backslashes and control characters. Every other character, including letters outside ASCII and space or format characters such as the narrow no-break space, is written as it was read.</summary>
    public static string ToJson(IpxDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        return IpxJsonWriter.Write(document.Node);
    }

    /// <summary>Writes a document as UTF-8 without a byte-order mark.</summary>
    public static void Write(IpxDocument document, Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var bytes = new UTF8Encoding(false).GetBytes(ToJson(document));
        stream.Write(bytes, 0, bytes.Length);
    }

    private static IpxDocument FromNode(JsonNode root)
    {
        if (root is not JsonObject node) throw new IpxFormatException("An IPX document is a JSON object.");

        if (node["format"] is not JsonValue format || !format.TryGetValue<string>(out var marker) || marker != IpxDocument.FormatName)
        {
            throw new IpxFormatException("The format property is not 'ipx'.");
        }

        if (node["formatVersion"] is not JsonValue version || !version.TryGetValue<string>(out var text))
        {
            throw new IpxFormatException("The formatVersion property is missing.");
        }

        var match = VersionPattern.Match(text);
        if (!match.Success) throw new IpxFormatException($"The version '{text}' is not of the form major.minor.patch.");
        if (int.Parse(match.Groups["major"].Value) != SupportedMajorVersion) throw new IpxFormatException($"Version {text} is not supported; this library reads version {SupportedMajorVersion}.x.");

        var document = new IpxDocument(node);
        if (document.Kind is null) throw new IpxFormatException("The kind is missing or is not one this library knows.");
        return document;
    }

    private static string FirstRepeatedProperty(string json)
    {
        var names = new Stack<HashSet<string>>();
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json), new JsonReaderOptions { CommentHandling = JsonCommentHandling.Disallow });

        try
        {
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.StartObject:
                        names.Push(new HashSet<string>(StringComparer.Ordinal));
                        break;
                    case JsonTokenType.EndObject:
                        names.Pop();
                        break;
                    case JsonTokenType.PropertyName:
                        var name = reader.GetString();
                        if (!names.Peek().Add(name)) return name;
                        break;
                }
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }
}
