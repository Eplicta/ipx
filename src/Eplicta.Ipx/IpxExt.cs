using System.Text.Json.Nodes;

namespace Eplicta.Ipx;

/// <summary>The extension data of an object: anything the specification does not define, under reverse-domain keys such as org.example.vendor.</summary>
public sealed class IpxExt : IpxObject
{
    public IpxExt() : base(new JsonObject())
    {
    }

    public IpxExt(JsonObject node) : base(node)
    {
    }

    public IEnumerable<string> Keys => Node.Select(x => x.Key).ToArray();

    public JsonNode Get(string key) => Node[key];

    public void Set(string key, JsonNode value) => Node[key] = value;

    public bool Remove(string key) => Node.Remove(key);
}
