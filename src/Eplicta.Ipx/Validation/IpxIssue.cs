namespace Eplicta.Ipx.Validation;

/// <summary>One finding of the validation: an error or a warning.</summary>
public sealed class IpxIssue
{
    public IpxIssue(string code, string path, string message)
    {
        Code = code;
        Path = path;
        Message = message;
    }

    /// <summary>A stable name for the kind of finding, one of the constants of <see cref="IpxIssueCodes"/>.</summary>
    public string Code { get; }

    /// <summary>A JSON Pointer to what the finding is about, such as /rows/3/retention/0/months.</summary>
    public string Path { get; }

    public string Message { get; }

    public override string ToString() => $"{Code} {Path}: {Message}";
}
