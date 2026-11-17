using System.Text.Json.Nodes;

namespace Eplicta.Ipx;

/// <summary>The document or the system that a document was converted from.</summary>
public sealed class IpxOrigin : IpxObject
{
    public IpxOrigin() : base(new JsonObject())
    {
    }

    public IpxOrigin(JsonObject node) : base(node)
    {
    }

    /// <summary>What the original was: a standard, a format or a reverse-domain name.</summary>
    public string Standard
    {
        get => GetString("standard");
        set => SetString("standard", value);
    }

    public string Version
    {
        get => GetString("version");
        set => SetString("version", value);
    }

    public string MediaType
    {
        get => GetString("mediaType");
        set => SetString("mediaType", value);
    }

    public string Uri
    {
        get => GetString("uri");
        set => SetString("uri", value);
    }

    public string Sha256
    {
        get => GetString("sha256");
        set => SetString("sha256", value);
    }

    /// <summary>The original, base64-encoded, when it is small enough to keep.</summary>
    public string Content
    {
        get => GetString("content");
        set => SetString("content", value);
    }

    public string Note
    {
        get => GetString("note");
        set => SetString("note", value);
    }
}
