namespace Eplicta.Ipx.Comparison;

/// <summary>A short text for a retention entry that can be compared at a glance. The text is language-neutral: triggers appear under their wire names.</summary>
public static class IpxRetentionText
{
    private const int MonthsPerYear = 12;

    /// <summary>
    /// <c>retain</c>, <c>handover</c>, <c>?</c> for unclear and review, <c>obsolete</c>, <c>dispose</c>, and for a period <c>6y/case-closed</c> or <c>30m</c>.
    /// A disposal at the trigger itself is <c>at/enrolment-end</c>. No entry is <c>-</c>.
    /// </summary>
    public static string Describe(IpxRetention retention)
    {
        if (retention is null) return "-";

        switch (retention.Disposition)
        {
            case IpxDisposition.Retain:
                return "retain";
            case IpxDisposition.Handover:
                return "handover";
            case IpxDisposition.Dispose:
                return DescribeDisposal(retention);
            default:
                return "?";
        }
    }

    private static string DescribeDisposal(IpxRetention retention)
    {
        var trigger = retention.Trigger ?? IpxTrigger.Unspecified;
        var named = trigger is not (IpxTrigger.Unspecified or IpxTrigger.None);
        var triggerText = named ? IpxWire.ToWire(trigger) : null;

        if (retention.Months is not int months)
        {
            if (trigger == IpxTrigger.Obsolete) return "obsolete";
            return named ? $"at/{triggerText}" : "dispose";
        }

        var span = months == 0 ? "at" : months % MonthsPerYear == 0 ? $"{months / MonthsPerYear}y" : $"{months}m";
        return named ? $"{span}/{triggerText}" : span;
    }
}
