namespace Eplicta.Ipx.Comparison;

/// <summary>How the retention of one document at a node compares with the retention of the base document at the same node.</summary>
public enum IpxDelta
{
    /// <summary>The same disposition, and the same period when both have one.</summary>
    Same,

    /// <summary>The document disposes of records sooner than the base.</summary>
    Shorter,

    /// <summary>The document keeps records longer than the base.</summary>
    Longer,

    /// <summary>Both dispose, but the periods count from different events, or only one of them has a period. Triggers are never converted into each other.</summary>
    NotComparable,

    /// <summary>One of the two says it is unclear.</summary>
    Unclear,

    /// <summary>Different dispositions that have no order, such as a handover against a disposal.</summary>
    Differs,

    /// <summary>The document has no rule at the node.</summary>
    NoRule,

    /// <summary>The document has a rule at the node and the base has none.</summary>
    NoBase
}
