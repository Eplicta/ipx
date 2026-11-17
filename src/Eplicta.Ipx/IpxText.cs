using System.Text.Json.Nodes;

namespace Eplicta.Ipx;

/// <summary>Text in one or more languages, keyed by language tag.</summary>
public sealed class IpxText : IpxObject
{
    public IpxText() : base(new JsonObject())
    {
    }

    public IpxText(JsonObject node) : base(node)
    {
    }

    /// <summary>The text in a language, or null when there is none. Setting null removes the language.</summary>
    public string this[string language]
    {
        get => GetString(language);
        set => SetString(language, value);
    }

    public IEnumerable<string> Languages => Node.Select(x => x.Key).ToArray();

    public static IpxText Create(params (string Language, string Text)[] items)
    {
        var text = new IpxText();
        foreach (var (language, value) in items)
        {
            text[language] = value;
        }

        return text;
    }
}
