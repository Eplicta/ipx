using System.Collections;
using System.Text.Json.Nodes;

namespace Eplicta.Ipx;

/// <summary>A live view of a JSON array of objects. Reading creates nothing; the array is created in the document when the first item is added.</summary>
public sealed class IpxList<T> : IReadOnlyList<T> where T : IpxObject
{
    private readonly JsonObject _owner;
    private readonly string _name;
    private readonly Func<JsonObject, T> _create;

    internal IpxList(JsonObject owner, string name, Func<JsonObject, T> create)
    {
        _owner = owner;
        _name = name;
        _create = create;
    }

    public int Count => Existing()?.Count ?? 0;

    public T this[int index] => _create(Existing()[index].AsObject());

    public void Add(T item)
    {
        ArgumentNullException.ThrowIfNull(item);
        Ensure().Add(item.Node);
    }

    public void Clear() => Existing()?.Clear();

    public bool Remove(T item)
    {
        var array = Existing();
        if (array is null) return false;

        for (var index = 0; index < array.Count; index++)
        {
            if (!ReferenceEquals(array[index], item.Node)) continue;
            array.RemoveAt(index);
            return true;
        }

        return false;
    }

    public IEnumerator<T> GetEnumerator()
    {
        var array = Existing();
        if (array is null) yield break;

        for (var index = 0; index < array.Count; index++)
        {
            yield return _create(array[index].AsObject());
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
