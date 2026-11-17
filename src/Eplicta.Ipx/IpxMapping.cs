using System.Text.Json.Nodes;

namespace Eplicta.Ipx;

/// <summary>How a row corresponds to a node in the taxonomy.</summary>
public sealed class IpxMapping : IpxObject
{
    public IpxMapping() : base(new JsonObject())
    {
    }

    public IpxMapping(JsonObject node) : base(node)
    {
    }

    /// <summary>The id of a node in the taxonomy.</summary>
    public string TaxonomyNode
    {
        get => GetString("node");
        set => SetString("node", value);
    }

    public IpxRelation? Relation
    {
        get => GetEnumOrNull<IpxRelation>("relation");
        set => SetEnum("relation", value);
    }
}
