using System.Text.Json.Nodes;

namespace Eplicta.Ipx;

/// <summary>Where in a source a row or a node was read.</summary>
public sealed class IpxSourceRef : IpxObject
{
    public IpxSourceRef() : base(new JsonObject())
    {
    }

    public IpxSourceRef(JsonObject node) : base(node)
    {
    }

    /// <summary>The id of an entry in the sources of the provenance.</summary>
    public string Ref
    {
        get => GetString("ref");
        set => SetString("ref", value);
    }

    public int? Page
    {
        get => GetInt("page");
        set => SetInt("page", value);
    }

    public int? PrintedPage
    {
        get => GetInt("printedPage");
        set => SetInt("printedPage", value);
    }

    public int? ContinuesOnPage
    {
        get => GetInt("continuesOnPage");
        set => SetInt("continuesOnPage", value);
    }

    public string Section
    {
        get => GetString("section");
        set => SetString("section", value);
    }

    public string Locator
    {
        get => GetString("locator");
        set => SetString("locator", value);
    }
}
