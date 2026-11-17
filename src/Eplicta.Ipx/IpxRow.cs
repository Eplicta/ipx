using System.Text.Json.Nodes;

namespace Eplicta.Ipx;

/// <summary>A row: what is kept, where, and for how long.</summary>
public sealed class IpxRow : IpxObject
{
    public IpxRow() : base(new JsonObject())
    {
    }

    public IpxRow(JsonObject node) : base(node)
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

    /// <summary>What the row describes. Rule when the property is absent.</summary>
    public IpxRowKind RowKind
    {
        get => IpxWire.TryParse<IpxRowKind>(GetString("rowKind"), out var value) ? value : IpxRowKind.Rule;
        set => SetString("rowKind", IpxWire.ToWire(value));
    }

    /// <summary>The id of the plan in the provenance that the row is attributed to.</summary>
    public string Plan
    {
        get => GetString("plan");
        set => SetString("plan", value);
    }

    /// <summary>The school forms the row applies to, by their names in the document, such as compulsory-school.</summary>
    public IpxStringList Forms => GetStringList("forms");

    public IpxText Name
    {
        get => GetText("name");
        set => SetObject("name", value);
    }

    public IpxText Description
    {
        get => GetText("description");
        set => SetObject("description", value);
    }

    /// <summary>The id of the node in this document's own tree that the row sits under.</summary>
    public string NodeId
    {
        get => GetString("node");
        set => SetString("node", value);
    }

    public IpxList<IpxMapping> Mappings => GetList("mappings", node => new IpxMapping(node));

    public IpxAsWritten AsWritten
    {
        get => GetObject("asWritten", node => new IpxAsWritten(node));
        set => SetObject("asWritten", value);
    }

    public IpxRegistration? Registration
    {
        get => GetEnumOrNull<IpxRegistration>("registration");
        set => SetEnum("registration", value);
    }

    public string RegistrationText
    {
        get => GetString("registrationText");
        set => SetString("registrationText", value);
    }

    public bool? PersonalData
    {
        get => GetBool("personalData");
        set => SetBool("personalData", value);
    }

    public string Secrecy
    {
        get => GetString("secrecy");
        set => SetString("secrecy", value);
    }

    /// <summary>The id of the system the records are kept in.</summary>
    public string System
    {
        get => GetString("system");
        set => SetString("system", value);
    }

    public IpxList<IpxReference> References => GetList("references", node => new IpxReference(node));

    public IpxList<IpxRetention> Retention => GetList("retention", node => new IpxRetention(node));

    public IpxSourceRef Source
    {
        get => GetObject("source", node => new IpxSourceRef(node));
        set => SetObject("source", value);
    }

    public IpxConfidence? Confidence
    {
        get => GetEnumOrNull<IpxConfidence>("confidence");
        set => SetEnum("confidence", value);
    }

    /// <summary>False when the row no longer applies to new documents. True when the property is absent.</summary>
    public bool Current
    {
        get => GetBool("current") ?? true;
        set => SetBool("current", value);
    }

    public string CurrentNote
    {
        get => GetString("currentNote");
        set => SetString("currentNote", value);
    }

    public string RuleType
    {
        get => GetString("ruleType");
        set => SetString("ruleType", value);
    }

    public bool? OutOfScope
    {
        get => GetBool("outOfScope");
        set => SetBool("outOfScope", value);
    }

    public IpxExt Ext => GetExt();

    public IpxExt EnsureExt() => EnsureExtension();
}
