using System.Text.Json.Nodes;

namespace Eplicta.Ipx;

/// <summary>The identity and the description of a document.</summary>
public sealed class IpxDocumentInfo : IpxObject
{
    public IpxDocumentInfo() : base(new JsonObject())
    {
    }

    public IpxDocumentInfo(JsonObject node) : base(node)
    {
    }

    public string Id
    {
        get => GetString("id");
        set => SetString("id", value);
    }

    public string Key
    {
        get => GetString("key");
        set => SetString("key", value);
    }

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

    public string Version
    {
        get => GetString("version");
        set => SetString("version", value);
    }

    public string Status
    {
        get => GetString("status");
        set => SetString("status", value);
    }

    public string Language
    {
        get => GetString("language");
        set => SetString("language", value);
    }

    public string NoteLanguage
    {
        get => GetString("noteLanguage");
        set => SetString("noteLanguage", value);
    }

    public string CreatedOn
    {
        get => GetString("createdOn");
        set => SetString("createdOn", value);
    }

    public string UpdatedOn
    {
        get => GetString("updatedOn");
        set => SetString("updatedOn", value);
    }

    public string ValidFrom
    {
        get => GetString("validFrom");
        set => SetString("validFrom", value);
    }

    public string ValidTo
    {
        get => GetString("validTo");
        set => SetString("validTo", value);
    }

    public IpxExt Ext => GetExt();

    public IpxExt EnsureExt() => EnsureExtension();
}
