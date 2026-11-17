using System.Text.Json.Nodes;

namespace Eplicta.Ipx;

/// <summary>An IPX document: a plan, a body of published rules, a recommendation or a classification tree.</summary>
public sealed class IpxDocument : IpxObject
{
    /// <summary>The marker every IPX document carries in its format property.</summary>
    public const string FormatName = "ipx";

    /// <summary>The version of the format this library writes.</summary>
    public const string CurrentVersion = "1.0.0";

    public IpxDocument(JsonObject node) : base(node)
    {
    }

    public string Format => GetString("format");

    public string FormatVersion
    {
        get => GetString("formatVersion");
        set => SetString("formatVersion", value);
    }

    /// <summary>The kind of document, or null when the property is missing or not a kind this library knows.</summary>
    public IpxKind? Kind
    {
        get => GetEnumOrNull<IpxKind>("kind");
        set => SetEnum("kind", value);
    }

    public IpxDocumentInfo Document => GetObject("document", node => new IpxDocumentInfo(node));

    public IpxList<IpxSystem> Systems => GetList("systems", node => new IpxSystem(node));

    public IpxList<IpxNode> Nodes => GetList("nodes", node => new IpxNode(node));

    public IpxList<IpxRow> Rows => GetList("rows", node => new IpxRow(node));

    public IpxProvenance Provenance => GetObject("provenance", node => new IpxProvenance(node));

    public IpxExt Ext => GetExt();

    /// <summary>Starts a new, empty document of the current version.</summary>
    public static IpxDocument Create(IpxKind kind, string id, IpxText name, string language)
    {
        var document = new IpxDocument(new JsonObject())
        {
            FormatVersion = CurrentVersion,
            Kind = kind
        };
        document.Node["format"] = FormatName;

        var info = new IpxDocumentInfo { Id = id, Name = name, Language = language };
        document.Node["document"] = info.Node;
        return document;
    }

    public IpxProvenance EnsureProvenance() => EnsureObject("provenance", node => new IpxProvenance(node));

    public IpxExt EnsureExt() => EnsureExtension();
}
