namespace Eplicta.Ipx.Validation;

/// <summary>The outcome of validating a document. Errors make it invalid; warnings do not.</summary>
public sealed class IpxValidationResult
{
    public IpxValidationResult(IReadOnlyList<IpxIssue> errors, IReadOnlyList<IpxIssue> warnings)
    {
        Errors = errors;
        Warnings = warnings;
    }

    public IReadOnlyList<IpxIssue> Errors { get; }

    public IReadOnlyList<IpxIssue> Warnings { get; }

    public bool IsValid => Errors.Count == 0;
}
