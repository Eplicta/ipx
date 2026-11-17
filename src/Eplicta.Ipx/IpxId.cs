using System.Text.RegularExpressions;

namespace Eplicta.Ipx;

/// <summary>The identifiers of IPX: nodes, rows, systems and the entries of provenance.</summary>
public static partial class IpxId
{
    /// <summary>Whether the text is an identifier: 1 to 128 ASCII letters, digits and <c>. _ : -</c>, starting with a letter or a digit.</summary>
    public static bool IsValid(string id) => id is not null && Pattern().IsMatch(id);

    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9._:-]{0,127}$")]
    private static partial Regex Pattern();
}
