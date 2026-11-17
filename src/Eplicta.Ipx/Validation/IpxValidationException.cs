namespace Eplicta.Ipx.Validation;

/// <summary>A document that was meant to be valid is not. The result holds every error.</summary>
public sealed class IpxValidationException : Exception
{
    private const int ShownErrors = 5;

    public IpxValidationException(IpxValidationResult result) : base(Describe(result))
    {
        Result = result;
    }

    public IpxValidationResult Result { get; }

    private static string Describe(IpxValidationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var shown = string.Join("; ", result.Errors.Take(ShownErrors));
        var more = result.Errors.Count > ShownErrors ? $" (and {result.Errors.Count - ShownErrors} more)" : string.Empty;
        return $"The IPX document is not valid: {shown}{more}";
    }
}
