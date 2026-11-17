using System.Text.Json.Nodes;

namespace Eplicta.Ipx;

/// <summary>What happens to the information and when. A row has more than one entry when the source gives separate periods, for example for granted and refused cases.</summary>
public sealed class IpxRetention : IpxObject
{
    public IpxRetention() : base(new JsonObject())
    {
    }

    public IpxRetention(JsonObject node) : base(node)
    {
    }

    /// <summary>What happens. A value this library does not know reads as Unclear; the original stays in the document.</summary>
    public IpxDisposition Disposition
    {
        get => IpxWire.TryParse<IpxDisposition>(GetString("disposition"), out var value) ? value : IpxDisposition.Unclear;
        set => SetString("disposition", IpxWire.ToWire(value));
    }

    public string DispositionText
    {
        get => GetString("dispositionText");
        set => SetString("dispositionText", value);
    }

    public IpxPeriod Period
    {
        get => GetObject("period", node => new IpxPeriod(node));
        set => SetObject("period", value);
    }

    /// <summary>The period in whole months, rounded up. Null when the source gives no period or the property is missing; see IsNull to tell the two apart.</summary>
    public int? Months
    {
        get => GetInt("months");
        set => SetInt("months", value);
    }

    /// <summary>What the period counts from. A value this library does not know reads as Other; the original stays in the document.</summary>
    public IpxTrigger? Trigger
    {
        get => GetEnumOrNeutral("trigger", IpxTrigger.Other);
        set => SetEnum("trigger", value);
    }

    public string TriggerText
    {
        get => GetString("triggerText");
        set => SetString("triggerText", value);
    }

    public bool? TriggerInferred
    {
        get => GetBool("triggerInferred");
        set => SetBool("triggerInferred", value);
    }

    public IpxQualifier? Qualifier
    {
        get => GetEnumOrNull<IpxQualifier>("qualifier");
        set => SetEnum("qualifier", value);
    }

    public string LatestEvent
    {
        get => GetString("latestEvent");
        set => SetString("latestEvent", value);
    }

    public string Condition
    {
        get => GetString("condition");
        set => SetString("condition", value);
    }

    public IpxOutcome? Outcome
    {
        get => GetEnumOrNull<IpxOutcome>("outcome");
        set => SetEnum("outcome", value);
    }

    public string RuleKey
    {
        get => GetString("ruleKey");
        set => SetString("ruleKey", value);
    }

    public string LegalBasis
    {
        get => GetString("legalBasis");
        set => SetString("legalBasis", value);
    }

    public string Note
    {
        get => GetString("note");
        set => SetString("note", value);
    }

    public IpxExt Ext => GetExt();

    public IpxExt EnsureExt() => EnsureExtension();

    /// <summary>Kept, with no period. The trigger says what a later review or handover is counted from, and is none for a plain retain.</summary>
    public static IpxRetention Retain(IpxTrigger trigger = IpxTrigger.None)
    {
        return new IpxRetention { Disposition = IpxDisposition.Retain, Trigger = trigger };
    }

    /// <summary>Disposed of when the period has passed, counted from the trigger. The months are filled in from the period.</summary>
    public static IpxRetention Dispose(double amount, IpxPeriodUnit unit, IpxTrigger trigger)
    {
        var period = IpxPeriod.Create(amount, unit);
        return new IpxRetention { Disposition = IpxDisposition.Dispose, Period = period, Months = period.ToMonths(), Trigger = trigger };
    }

    /// <summary>Disposed of at the trigger itself, with no period.</summary>
    public static IpxRetention Dispose(IpxTrigger trigger)
    {
        return new IpxRetention { Disposition = IpxDisposition.Dispose, Trigger = trigger };
    }

    /// <summary>Handed over to another body, such as an archive authority, when the period has passed, counted from the trigger.</summary>
    public static IpxRetention Handover(double amount, IpxPeriodUnit unit, IpxTrigger trigger)
    {
        var period = IpxPeriod.Create(amount, unit);
        return new IpxRetention { Disposition = IpxDisposition.Handover, Period = period, Months = period.ToMonths(), Trigger = trigger };
    }
}
