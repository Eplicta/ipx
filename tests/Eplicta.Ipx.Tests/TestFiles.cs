using System.Text.Json.Nodes;

namespace Eplicta.Ipx.Tests;

internal static class TestFiles
{
    public static string Read(string relativePath) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, relativePath));

    public static string Example() => Read("examples/school-plan.ipx.json");

    public static IpxDocument ExampleDocument() => IpxSerializer.Parse(Example());

    public static JsonObject ExampleNode() => (JsonObject)JsonNode.Parse(Example());
}
