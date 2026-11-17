using System.Text;
using System.Text.Json.Nodes;

namespace Eplicta.Ipx;

/// <summary>Puts the properties of a document in the order the schema lists them, so that what the builder writes reads the same way every time. Properties the schema does not list keep their order and follow the listed ones.</summary>
internal static class IpxPropertyOrder
{
    private const string ResourceName = "Eplicta.Ipx.ipx-1.0.0.schema.json";
    private const string DefinitionPrefix = "#/$defs/";

    private static readonly Lazy<JsonObject> Schema = new(Load);

    public static void Apply(JsonObject document)
    {
        Walk(document, Schema.Value, Schema.Value);
    }

    private static void Walk(JsonNode value, JsonNode schemaNode, JsonObject root)
    {
        var schema = Resolve(schemaNode, root);
        if (schema is null) return;

        if (schema["oneOf"] is JsonArray alternatives)
        {
            foreach (var alternative in alternatives)
            {
                var resolved = Resolve(alternative, root);
                if (resolved is null || !Describes(resolved, value)) continue;

                Walk(value, resolved, root);
                return;
            }

            return;
        }

        if (value is JsonObject obj && schema["properties"] is JsonObject properties)
        {
            Reorder(obj, properties);
            foreach (var pair in obj.ToList())
            {
                if (pair.Value is not null && properties[pair.Key] is { } child) Walk(pair.Value, child, root);
            }
        }
        else if (value is JsonArray array && schema["items"] is { } items)
        {
            foreach (var element in array)
            {
                if (element is not null) Walk(element, items, root);
            }
        }
    }

    private static bool Describes(JsonObject schema, JsonNode value)
    {
        return value switch
        {
            JsonObject => schema["properties"] is not null,
            JsonArray => schema["items"] is not null,
            _ => false
        };
    }

    private static void Reorder(JsonObject obj, JsonObject properties)
    {
        var entries = obj.ToList();
        var ordered = new List<KeyValuePair<string, JsonNode>>(entries.Count);
        foreach (var listed in properties)
        {
            var index = entries.FindIndex(x => x.Key == listed.Key);
            if (index < 0) continue;

            ordered.Add(entries[index]);
            entries.RemoveAt(index);
        }

        ordered.AddRange(entries);

        obj.Clear();
        foreach (var entry in ordered)
        {
            obj[entry.Key] = entry.Value;
        }
    }

    private static JsonObject Resolve(JsonNode schema, JsonObject root)
    {
        if (schema is not JsonObject current) return null;
        if (current["$ref"] is not JsonValue reference || !reference.TryGetValue<string>(out var target) || !target.StartsWith(DefinitionPrefix, StringComparison.Ordinal)) return current;

        return root["$defs"]?[target[DefinitionPrefix.Length..]] as JsonObject;
    }

    private static JsonObject Load()
    {
        using var stream = typeof(IpxPropertyOrder).Assembly.GetManifestResourceStream(ResourceName) ?? throw new InvalidOperationException($"The schema {ResourceName} is not embedded in the assembly.");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return (JsonObject)JsonNode.Parse(reader.ReadToEnd());
    }
}
