namespace Eplicta.Ipx.Comparison;

/// <summary>What a comparison includes.</summary>
public sealed class IpxCompareOptions
{
    /// <summary>Only the taxonomy nodes of this school form. Null includes every form.</summary>
    public IpxForm? Form { get; set; }

    /// <summary>Only the lines where at least one document has a rule and at least one document differs from the base.</summary>
    public bool OnlyDifferences { get; set; }
}
