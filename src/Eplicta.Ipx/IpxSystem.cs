using System.Text.Json.Nodes;

namespace Eplicta.Ipx;

/// <summary>An information system that records are kept in.</summary>
public sealed class IpxSystem : IpxObject
{
    public IpxSystem() : base(new JsonObject())
    {
    }

    public IpxSystem(JsonObject node) : base(node)
    {
    }

    public string Id
    {
        get => GetString("id");
        set => SetString("id", value);
    }

    public string Name
    {
        get => GetString("name");
        set => SetString("name", value);
    }

    public string Kind
    {
        get => GetString("kind");
        set => SetString("kind", value);
    }

    public string Purpose
    {
        get => GetString("purpose");
        set => SetString("purpose", value);
    }

    public string Confidentiality
    {
        get => GetString("confidentiality");
        set => SetString("confidentiality", value);
    }

    public bool? ConfirmedPublic
    {
        get => GetBool("confirmedPublic");
        set => SetBool("confirmedPublic", value);
    }

    public string Uri
    {
        get => GetString("uri");
        set => SetString("uri", value);
    }

    public string Note
    {
        get => GetString("note");
        set => SetString("note", value);
    }

    public IpxExt Ext => GetExt();

    public IpxExt EnsureExt() => EnsureExtension();
}
