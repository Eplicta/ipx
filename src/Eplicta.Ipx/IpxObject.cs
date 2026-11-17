using System.Text.Json.Nodes;

namespace Eplicta.Ipx;

/// <summary>
/// Base of every typed view over a part of an IPX document. A view reads and writes the underlying JSON, so everything it does not know about, such as properties added by a newer minor version, is kept exactly as it was.
/// </summary>
public abstract class IpxObject
{
    protected IpxObject(JsonObject node)
    {
        Node = node ?? throw new ArgumentNullException(nameof(node));
    }

    /// <summary>The JSON object this view is over. Anything the view has no property for can be reached here.</summary>
    public JsonObject Node { get; }

    /// <summary>Whether the property is present, even as a JSON null.</summary>
    public bool Has(string name) => Node.ContainsKey(name);

    /// <summary>Whether the property is present with the value JSON null, which in IPX means not known.</summary>
    public bool IsNull(string name) => Node.TryGetPropertyValue(name, out var value) && value is null;

    /// <summary>Writes a JSON null, which in IPX means not known. Setting a property to null through a typed property removes it instead.</summary>
    public void SetNull(string name) => Node[name] = null;

    protected string GetString(string name) => Node[name] is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;

    protected void SetString(string name, string value)
    {
        if (value is null) Node.Remove(name);
        else Node[name] = value;
    }

    protected bool? GetBool(string name) => Node[name] is JsonValue value && value.TryGetValue<bool>(out var flag) ? flag : null;

    protected void SetBool(string name, bool? value)
    {
        if (value is null) Node.Remove(name);
        else Node[name] = value.Value;
    }

    protected int? GetInt(string name) => Node[name] is JsonValue value && value.TryGetValue<int>(out var number) ? number : null;

    protected void SetInt(string name, int? value)
    {
        if (value is null) Node.Remove(name);
        else Node[name] = value.Value;
    }

    protected double? GetDouble(string name) => Node[name] is JsonValue value && value.TryGetValue<double>(out var number) ? number : null;

    protected void SetDouble(string name, double? value)
    {
        if (value is null) Node.Remove(name);
        else Node[name] = value.Value;
    }

    protected T GetEnum<T>(string name) where T : struct, Enum
    {
        var text = GetString(name);
        return IpxWire.TryParse<T>(text, out var value) ? value : default;
    }

    protected T? GetEnumOrNull<T>(string name) where T : struct, Enum
    {
        var text = GetString(name);
        return IpxWire.TryParse<T>(text, out var value) ? value : null;
    }

    protected T? GetEnumOrNeutral<T>(string name, T neutral) where T : struct, Enum
    {
        var text = GetString(name);
        if (text is null) return null;
        return IpxWire.TryParse<T>(text, out var value) ? value : neutral;
    }

    protected void SetEnum<T>(string name, T? value) where T : struct, Enum
    {
        if (value is null) Node.Remove(name);
        else Node[name] = IpxWire.ToWire(value.Value);
    }

    protected T GetObject<T>(string name, Func<JsonObject, T> create) where T : class => Node[name] is JsonObject node ? create(node) : null;

    protected T EnsureObject<T>(string name, Func<JsonObject, T> create) where T : class
    {
        if (Node[name] is not JsonObject node)
        {
            node = new JsonObject();
            Node[name] = node;
        }

        return create(node);
    }

    protected void SetObject(string name, IpxObject value)
    {
        if (value is null) Node.Remove(name);
        else Node[name] = value.Node;
    }

    protected IpxList<T> GetList<T>(string name, Func<JsonObject, T> create) where T : IpxObject => new(Node, name, create);

    protected IpxStringList GetStringList(string name) => new(Node, name);

    protected IpxText GetText(string name) => GetObject(name, node => new IpxText(node));

    protected IpxExt GetExt() => GetObject("ext", node => new IpxExt(node));

    protected IpxExt EnsureExtension() => EnsureObject("ext", node => new IpxExt(node));
}
