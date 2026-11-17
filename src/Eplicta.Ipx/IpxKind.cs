namespace Eplicta.Ipx;

/// <summary>What a document is.</summary>
public enum IpxKind
{
    [IpxWire("plan")] Plan,
    [IpxWire("reference")] Reference,
    [IpxWire("recommendation")] Recommendation,
    [IpxWire("taxonomy")] Taxonomy
}
