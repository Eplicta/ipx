namespace Eplicta.Ipx;

/// <summary>The text cannot be read as an IPX document this library supports: it is not JSON, it is not IPX, or it is of a major version or a kind that is not known.</summary>
public sealed class IpxFormatException : Exception
{
    public IpxFormatException(string message) : base(message)
    {
    }

    public IpxFormatException(string message, Exception inner) : base(message, inner)
    {
    }
}
