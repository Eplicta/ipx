using System.Text.Json.Nodes;

namespace Eplicta.Ipx;

/// <summary>One place in the classification tree.</summary>
public sealed class IpxNode : IpxObject
{
    public IpxNode() : base(new JsonObject())
    {
    }

    public IpxNode(JsonObject node) : base(node)
    {
    }

    public string Id
    {
        get => GetString("id");
        set => SetString("id", value);
    }

    public string Uid
    {
        get => GetString("uid");
        set => SetString("uid", value);
    }

    public string SourceId
    {
        get => GetString("sourceId");
        set => SetString("sourceId", value);
    }

    public string Uri
    {
        get => GetString("uri");
        set => SetString("uri", value);
    }

    /// <summary>The code people see, such as 3.5.1. It may change when rows are reordered, which is why it is not the identity.</summary>
    public string Code
    {
        get => GetString("code");
        set => SetString("code", value);
    }

    /// <summary>The id of the parent node. Null for a root; setting null removes the property, SetNull writes an explicit null.</summary>
    public string Parent
    {
        get => GetString("parent");
        set => SetString("parent", value);
    }

    public string NodeKind
    {
        get => GetString("nodeKind");
        set => SetString("nodeKind", value);
    }

    public string Level
    {
        get => GetString("level");
        set => SetString("level", value);
    }

    public IpxForm? Form
    {
        get => GetEnumOrNull<IpxForm>("form");
        set => SetEnum("form", value);
    }

    /// <summary>When the node starts to apply, as a date or a partial date. A node that always applied has none.</summary>
    public string ValidFrom
    {
        get => GetString("validFrom");
        set => SetString("validFrom", value);
    }

    /// <summary>The last day the node applies, inclusive, as a date or a partial date.</summary>
    public string ValidTo
    {
        get => GetString("validTo");
        set => SetString("validTo", value);
    }

    public IpxText Title
    {
        get => GetText("title");
        set => SetObject("title", value);
    }

    public IpxText Description
    {
        get => GetText("description");
        set => SetObject("description", value);
    }

    public string Concept
    {
        get => GetString("concept");
        set => SetString("concept", value);
    }

    public string CaseRegistration
    {
        get => GetString("caseRegistration");
        set => SetString("caseRegistration", value);
    }

    /// <summary>The id of the system records under the node are kept in.</summary>
    public string System
    {
        get => GetString("system");
        set => SetString("system", value);
    }

    public IpxList<IpxReference> References => GetList("references", node => new IpxReference(node));

    public IpxSourceRef Source => GetObject("source", node => new IpxSourceRef(node));

    public IpxExt Ext => GetExt();

    public IpxExt EnsureExt() => EnsureExtension();
}
