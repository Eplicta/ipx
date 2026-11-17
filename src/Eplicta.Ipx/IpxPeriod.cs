using System.Text.Json.Nodes;

namespace Eplicta.Ipx;

/// <summary>A retention period as the source states it.</summary>
public sealed class IpxPeriod : IpxObject
{
    private const double DaysPerMonth = 30;
    private const double MonthsPerYear = 12;

    public IpxPeriod() : base(new JsonObject())
    {
    }

    public IpxPeriod(JsonObject node) : base(node)
    {
    }

    public double? Amount
    {
        get => GetDouble("amount");
        set => SetDouble("amount", value);
    }

    public IpxPeriodUnit? Unit
    {
        get => GetEnumOrNull<IpxPeriodUnit>("unit");
        set => SetEnum("unit", value);
    }

    /// <summary>A period of an amount of days, months or years.</summary>
    public static IpxPeriod Create(double amount, IpxPeriodUnit unit)
    {
        return new IpxPeriod { Amount = amount, Unit = unit };
    }

    /// <summary>The period in whole months, rounded up, as the <c>months</c> property of a retention entry holds it. Null when the amount or the unit is missing. A month is counted as 30 days.</summary>
    public int? ToMonths() => MonthsOf(Node);

    internal static int? MonthsOf(JsonObject period)
    {
        if (period["amount"] is not JsonValue amountValue || !amountValue.TryGetValue<double>(out var amount)) return null;

        var unit = period["unit"] is JsonValue unitValue && unitValue.TryGetValue<string>(out var text) ? text : null;
        return unit switch
        {
            "year" => (int)Math.Ceiling(amount * MonthsPerYear),
            "month" => (int)Math.Ceiling(amount),
            "day" => (int)Math.Ceiling(amount / DaysPerMonth),
            _ => null
        };
    }
}
