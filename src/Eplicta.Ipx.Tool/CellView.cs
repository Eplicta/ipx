using Eplicta.Ipx.Comparison;

namespace Eplicta.Ipx.Tool;

/// <summary>How a compared cell is shown: its text with the marks that say how it was found, and its colour.</summary>
internal static class CellView
{
    private const int LongMonths = 84;
    private const int MediumMonths = 36;

    public const string Legend = "Marks: + several rows, ~ covered by a broader row, / covers only part, ≈ only similar, ° not a standing plan rule (one-off decision, GDPR limit, platform statement), ^ inherited from the parent node.";

    public static string Text(IpxComparedCell cell)
    {
        if (!cell.HasRule) return cell.Text;

        var marks = string.Empty;
        if (cell.Multiple) marks += "+";
        if (cell.Relation == IpxRelation.Covers) marks += "~";
        if (cell.Relation == IpxRelation.Part) marks += "/";
        if (cell.Relation == IpxRelation.Similar) marks += "≈";
        if (cell.NotAStandingRule) marks += "°";
        if (cell.Inherited) marks += "^";

        return cell.Text + marks;
    }

    public static OutputColor Color(IpxComparedCell cell, bool isBase)
    {
        if (!isBase) return cell.Delta is { } delta ? DeltaColor(delta) : OutputColor.Default;

        return RetentionColor(cell.Retention);
    }

    public static OutputColor DeltaColor(IpxDelta delta) => delta switch
    {
        IpxDelta.Same => OutputColor.Green,
        IpxDelta.Shorter => OutputColor.Yellow,
        IpxDelta.Longer => OutputColor.Magenta,
        IpxDelta.NotComparable => OutputColor.DarkYellow,
        IpxDelta.Unclear => OutputColor.Red,
        IpxDelta.Differs => OutputColor.Red,
        IpxDelta.NoRule => OutputColor.Gray,
        _ => OutputColor.Gray
    };

    public static OutputColor RetentionColor(IpxRetention retention)
    {
        if (retention is null) return OutputColor.Gray;

        switch (retention.Disposition)
        {
            case IpxDisposition.Retain:
                return OutputColor.Magenta;
            case IpxDisposition.Handover:
                return OutputColor.Cyan;
            case IpxDisposition.Dispose:
                if (retention.Months is not int months) return OutputColor.DarkYellow;
                if (months >= LongMonths) return OutputColor.Yellow;
                return months >= MediumMonths ? OutputColor.Green : OutputColor.Cyan;
            default:
                return OutputColor.Red;
        }
    }

    /// <summary>The wire name of the form of a node, or a dash for a node that has none.</summary>
    public static string FormName(IpxNode node) => node.Form is { } form ? IpxWire.ToWire(form) : "-";
}
