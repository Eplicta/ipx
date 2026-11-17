namespace Eplicta.Ipx.Comparison;

/// <summary>One leaf node of the taxonomy and what each compared document says there.</summary>
public sealed class IpxComparisonLine
{
    public IpxComparisonLine(IpxNode node, int depth, IReadOnlyList<IpxComparedCell> cells)
    {
        Node = node;
        Depth = depth;
        Cells = cells;
    }

    public IpxNode Node { get; }

    /// <summary>The number of ancestors the node has in the taxonomy.</summary>
    public int Depth { get; }

    /// <summary>One cell per compared document, in the order the documents were given. The first is the base.</summary>
    public IReadOnlyList<IpxComparedCell> Cells { get; }

    public bool HasAnyRule => Cells.Any(x => x.HasRule);

    /// <summary>A document other than the base does not have the same retention as the base.</summary>
    public bool IsDifferent => Cells.Skip(1).Any(x => x.Delta != IpxDelta.Same);
}
