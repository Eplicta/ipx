using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Eplicta.Ipx;

/// <summary>The canonical form of a JSON value as defined by the JSON Canonicalization Scheme, RFC 8785. Two values that are the same JSON value have the same canonical text, whatever their spacing and property order, so a hash of it says whether content has changed.</summary>
public static class IpxCanonicalJson
{
    private const int LargestPlainExponent = 21;
    private const int SmallestPlainExponent = -6;

    public static string Serialize(JsonNode node)
    {
        var builder = new StringBuilder();
        Write(node, builder);
        return builder.ToString();
    }

    /// <summary>The SHA-256 hash of the canonical text in UTF-8, as lower-case hexadecimal.</summary>
    public static string Sha256(JsonNode node) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Serialize(node)))).ToLowerInvariant();

    /// <summary>A number in the form RFC 8785 takes from ECMAScript: the shortest digits that read back as the same value, in plain notation between 1e-6 and 1e21, otherwise with an exponent.</summary>
    public static string FormatNumber(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(nameof(value), "JSON has no form for this number.");
        if (value == 0) return "0";

        var (digits, exponent) = Decompose(Math.Abs(value));
        var text = Layout(digits, exponent);
        return value < 0 ? "-" + text : text;
    }

    private static (string Digits, int Exponent) Decompose(double value)
    {
        var text = value.ToString("R", CultureInfo.InvariantCulture);
        var exponent = 0;
        var marker = text.IndexOfAny(['E', 'e']);
        if (marker >= 0)
        {
            exponent = int.Parse(text[(marker + 1)..], CultureInfo.InvariantCulture);
            text = text[..marker];
        }

        var point = text.IndexOf('.');
        var whole = point < 0 ? text : text[..point];
        var fraction = point < 0 ? "" : text[(point + 1)..];
        var digits = whole + fraction;
        var position = whole.Length + exponent;

        var leading = 0;
        while (leading < digits.Length - 1 && digits[leading] == '0') leading++;

        return (digits[leading..].TrimEnd('0'), position - leading);
    }

    private static string Layout(string digits, int position)
    {
        var count = digits.Length;
        if (count <= position && position <= LargestPlainExponent) return digits + new string('0', position - count);
        if (0 < position && position <= LargestPlainExponent) return digits[..position] + "." + digits[position..];
        if (SmallestPlainExponent < position && position <= 0) return "0." + new string('0', -position) + digits;

        var exponent = position - 1;
        var sign = exponent < 0 ? "-" : "+";
        var mantissa = count == 1 ? digits : digits[0] + "." + digits[1..];
        return mantissa + "e" + sign + Math.Abs(exponent).ToString(CultureInfo.InvariantCulture);
    }

    private static void Write(JsonNode node, StringBuilder builder)
    {
        switch (node)
        {
            case null:
                builder.Append("null");
                break;
            case JsonObject jsonObject:
                WriteObject(jsonObject, builder);
                break;
            case JsonArray array:
                WriteArray(array, builder);
                break;
            default:
                WriteValue((JsonValue)node, builder);
                break;
        }
    }

    private static void WriteObject(JsonObject node, StringBuilder builder)
    {
        builder.Append('{');
        var first = true;
        foreach (var property in node.OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            if (!first) builder.Append(',');
            first = false;
            WriteString(property.Key, builder);
            builder.Append(':');
            Write(property.Value, builder);
        }

        builder.Append('}');
    }

    private static void WriteArray(JsonArray node, StringBuilder builder)
    {
        builder.Append('[');
        for (var index = 0; index < node.Count; index++)
        {
            if (index > 0) builder.Append(',');
            Write(node[index], builder);
        }

        builder.Append(']');
    }

    private static void WriteValue(JsonValue node, StringBuilder builder)
    {
        var element = JsonSerializer.SerializeToElement(node);
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                WriteString(element.GetString(), builder);
                break;
            case JsonValueKind.Number:
                builder.Append(FormatNumber(element.GetDouble()));
                break;
            case JsonValueKind.True:
                builder.Append("true");
                break;
            case JsonValueKind.False:
                builder.Append("false");
                break;
            default:
                builder.Append("null");
                break;
        }
    }

    private static void WriteString(string text, StringBuilder builder)
    {
        builder.Append('"');
        foreach (var character in text)
        {
            switch (character)
            {
                case '"': builder.Append("\\\""); break;
                case '\\': builder.Append("\\\\"); break;
                case '\b': builder.Append("\\b"); break;
                case '\t': builder.Append("\\t"); break;
                case '\n': builder.Append("\\n"); break;
                case '\f': builder.Append("\\f"); break;
                case '\r': builder.Append("\\r"); break;
                case < ' ': builder.Append("\\u").Append(((int)character).ToString("x4", CultureInfo.InvariantCulture)); break;
                default: builder.Append(character); break;
            }
        }

        builder.Append('"');
    }
}
