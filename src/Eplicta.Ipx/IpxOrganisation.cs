using System.Text.Json.Nodes;

namespace Eplicta.Ipx;

/// <summary>The organisation a plan is for. Other properties, such as units and pupil totals, are reached through <see cref="IpxObject.Node"/>.</summary>
public sealed class IpxOrganisation : IpxObject
{
    public IpxOrganisation() : base(new JsonObject())
    {
    }

    public IpxOrganisation(JsonObject node) : base(node)
    {
    }

    public string Name
    {
        get => GetString("name");
        set => SetString("name", value);
    }

    public string LegalForm
    {
        get => GetString("legalForm");
        set => SetString("legalForm", value);
    }

    /// <summary>The school forms the organisation runs, as the wire names of <see cref="IpxForm"/>.</summary>
    public IpxStringList Forms => GetStringList("forms");

    /// <summary>Which retention regime applies: smaller-16f, larger-osl or public.</summary>
    public string Regime
    {
        get => GetString("regime");
        set => SetString("regime", value);
    }

    public string RegimeNote
    {
        get => GetString("regimeNote");
        set => SetString("regimeNote", value);
    }

    public IpxExt Ext => GetExt();

    public IpxExt EnsureExt() => EnsureExtension();
}
