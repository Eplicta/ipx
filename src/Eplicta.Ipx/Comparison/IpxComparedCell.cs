namespace Eplicta.Ipx.Comparison;

/// <summary>What one document says at one node of the taxonomy.</summary>
public sealed class IpxComparedCell
{
    public IpxComparedCell(IpxDocument document, IReadOnlyList<IpxRow> rows, IpxRelation? relation, IpxRetention retention, string text, bool inherited, bool notAStandingRule)
    {
        Document = document;
        Rows = rows;
        Relation = relation;
        Retention = retention;
        Text = text;
        Inherited = inherited;
        NotAStandingRule = notAStandingRule;
    }

    public IpxDocument Document { get; }

    /// <summary>The rows the cell is made of: the ones at the closest relation to the node. Empty when the document has no rule there.</summary>
    public IReadOnlyList<IpxRow> Rows { get; }

    /// <summary>How the rows relate to the node: exact, covers, part or similar. Null when there are no rows.</summary>
    public IpxRelation? Relation { get; }

    /// <summary>The retention that stands for the cell: the longest-keeping entry of the longest-keeping row. Null when there are no rows or the rows have no entries.</summary>
    public IpxRetention Retention { get; }

    /// <summary>A short, language-neutral text for the retention, or a range such as <c>2y…6y</c> when several rows differ. See <see cref="IpxRetentionText"/>.</summary>
    public string Text { get; }

    public bool HasRule => Rows.Count > 0;

    /// <summary>More than one row makes up the cell.</summary>
    public bool Multiple => Rows.Count > 1;

    /// <summary>The node has no row of its own, so the cell is the rule of its parent.</summary>
    public bool Inherited { get; }

    /// <summary>The best row is not a standing plan rule: a one-off decision, a GDPR limit or a platform statement.</summary>
    public bool NotAStandingRule { get; }

    /// <summary>How the cell compares with the base document. Null for the base itself.</summary>
    public IpxDelta? Delta { get; internal set; }
}
