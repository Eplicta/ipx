namespace Eplicta.Ipx.Comparison;

/// <summary>The result of comparing documents against a taxonomy.</summary>
public sealed class IpxComparison
{
    public IpxComparison(IpxDocument taxonomy, IReadOnlyList<IpxDocument> documents, IReadOnlyList<IpxComparisonLine> lines)
    {
        Taxonomy = taxonomy;
        Documents = documents;
        Lines = lines;
    }

    public IpxDocument Taxonomy { get; }

    /// <summary>The compared documents. The first is the base the others are measured against.</summary>
    public IReadOnlyList<IpxDocument> Documents { get; }

    public IReadOnlyList<IpxComparisonLine> Lines { get; }
}
