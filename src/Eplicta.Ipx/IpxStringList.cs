using System.Collections;
using System.Text.Json.Nodes;

namespace Eplicta.Ipx;

/// <summary>A live view of a JSON array of strings, such as the school forms of a row.</summary>
public sealed class IpxStringList : IReadOnlyList<string>
{
    private readonly JsonObject _owner;
    private readonly string _name;

    internal IpxStringList(JsonObject owner, string name)
    {
        _owner = owner;
        _name = name;
    }

    public int Count => Existing()?.Count ?? 0;

    public string this[int index] => (string)Existing()[index];

    public void Add(string item)
    {
        ArgumentNullException.ThrowIfNull(item);
        Ensure().Add(item);
    }

    public void Clear() => Existing()?.Clear();

    public bool Remove(string item)
    {
        var array = Existing();
        if (array is null) return false;

        for (var index = 0; index < array.Count; index++)
        {
            if ((string)array[index] != item) continue;
            array.RemoveAt(index);
            return true;
        }

        return false;
    }

    public IEnumerator<string> GetEnumerator()
    {
        var array = Existing();
        if (array is null) yield break;

        for (var index = 0; index < array.Count; index++)
        {
            yield return (string)array[index];
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private JsonArray Existing() => _owner[_name] as JsonArray;

    private JsonArray Ensure()
    {
        if (_owner[_name] is JsonArray array) return array;

        array = new JsonArray();
        _owner[_name] = array;
        return array;
    }
}
