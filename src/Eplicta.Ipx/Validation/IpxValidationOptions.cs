namespace Eplicta.Ipx.Validation;

/// <summary>What the validation may be told about things outside the document.</summary>
public sealed class IpxValidationOptions
{
    /// <summary>The ids of the nodes of the taxonomy the rows map into. When given, a mapping to any other node is a warning.</summary>
    public IEnumerable<string> TaxonomyNodeIds { get; init; }
}
