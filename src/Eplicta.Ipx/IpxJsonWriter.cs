using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;

namespace Eplicta.Ipx;

internal static class IpxJsonWriter
{
    private const string Indent = "  ";

    public static string Write(JsonNode node)
    {
        var builder = new StringBuilder();
        WriteNode(node, builder, 0);
        builder.Append('\n');
        return builder.ToString();
    }

    private static void WriteNode(JsonNode node, StringBuilder builder, int depth)
    {
        switch (node)
        {
            case null:
                builder.Append("null");
                break;
            case JsonObject jsonObject:
                WriteObject(jsonObject, builder, depth);
                break;
            case JsonArray array:
                WriteArray(array, builder, depth);
                break;
            default:
                WriteValue((JsonValue)node, builder);
                break;
        }
    }

    private static void WriteObject(JsonObject node, StringBuilder builder, int depth)
    {
        if (node.Count == 0)
        {
            builder.Append("{}");
            return;
        }

        builder.Append('{');
        var first = true;
        foreach (var property in node)
        {
            builder.Append(first ? "\n" : ",\n");
            first = false;
            AppendIndent(builder, depth + 1);
            WriteString(property.Key, builder);
            builder.Append(": ");
            WriteNode(property.Value, builder, depth + 1);
        }

        builder.Append('\n');
        AppendIndent(builder, depth);
        builder.Append('}');
    }

    private static void WriteArray(JsonArray node, StringBuilder builder, int depth)
    {
        if (node.Count == 0)
        {
            builder.Append("[]");
            return;
        }

        builder.Append('[');
        for (var index = 0; index < node.Count; index++)
        {
            builder.Append(index == 0 ? "\n" : ",\n");
            AppendIndent(builder, depth + 1);
            WriteNode(node[index], builder, depth + 1);
        }

        builder.Append('\n');
        AppendIndent(builder, depth);
        builder.Append(']');
    }

    private static void WriteValue(JsonValue node, StringBuilder builder)
    {
        if (node.TryGetValue<string>(out var text))
        {
            WriteString(text, builder);
            return;
        }

        builder.Append(node.ToJsonString());
    }

    private static void WriteString(string text, StringBuilder builder)
    {
        builder.Append('"');
        for (var index = 0; index < text.Length; index++)
        {
            var character = text[index];
            switch (character)
            {
                case '"': builder.Append("\\\""); break;
                case '\\': builder.Append("\\\\"); break;
                case '\b': builder.Append("\\b"); break;
                case '\t': builder.Append("\\t"); break;
                case '\n': builder.Append("\\n"); break;
                case '\f': builder.Append("\\f"); break;
                case '\r': builder.Append("\\r"); break;
                case < ' ':
                    AppendUnicodeEscape(builder, character);
                    break;
                default:
                    if (char.IsHighSurrogate(character) && index + 1 < text.Length && char.IsLowSurrogate(text[index + 1]))
                    {
                        builder.Append(character).Append(text[++index]);
                    }
                    else if (char.IsSurrogate(character))
                    {
                        AppendUnicodeEscape(builder, character);
                    }
                    else
                    {
                        builder.Append(character);
                    }

                    break;
            }
        }

        builder.Append('"');
    }

    private static void AppendUnicodeEscape(StringBuilder builder, char character) => builder.Append("\\u").Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));

    private static void AppendIndent(StringBuilder builder, int depth)
    {
        for (var level = 0; level < depth; level++)
        {
            builder.Append(Indent);
        }
    }
}
