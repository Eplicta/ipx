namespace Eplicta.Ipx.Tool;

/// <summary>The command was used wrongly, or a file it was given cannot be read. The message is shown to the user as it is.</summary>
internal sealed class UsageException : Exception
{
    public UsageException(string message) : base(message)
    {
    }
}
