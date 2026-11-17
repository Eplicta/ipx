using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;

namespace Eplicta.Ipx.Validation;

internal static class IpxSchema
{
    private const string ResourceName = "Eplicta.Ipx.ipx-1.0.0.schema.json";

    private static readonly Lazy<JsonSchema> Schema = new(() => Load(false));
    private static readonly Lazy<JsonSchema> LenientSchema = new(() => Load(true));

    /// <summary>Checks the instance against the schema of this version.</summary>
    public static List<IpxIssue> Check(JsonNode instance) => Check(instance, Schema.Value);

    /// <summary>Checks the instance against the schema with closed enumerations and the ban on unknown properties lifted. Used for a document of a newer minor version, which may add both.</summary>
    public static List<IpxIssue> CheckLenient(JsonNode instance) => Check(instance, LenientSchema.Value);

    private static List<IpxIssue> Check(JsonNode instance, JsonSchema schema)
    {
        var results = schema.Evaluate(JsonSerializer.SerializeToElement(instance), new EvaluationOptions { OutputFormat = OutputFormat.List });
        var issues = new List<IpxIssue>();
        if (results.IsValid) return issues;

        Collect(results, issues);
        if (issues.Count == 0) issues.Add(new IpxIssue(IpxIssueCodes.Schema, "/", "The document does not match the schema."));
        return issues;
    }

    private static void Collect(EvaluationResults results, List<IpxIssue> issues)
    {
        if (results.Errors is not null)
        {
            var path = results.InstanceLocation.ToString();
            foreach (var error in results.Errors)
            {
                issues.Add(new IpxIssue(IpxIssueCodes.Schema, path.Length == 0 ? "/" : path, $"{error.Key}: {error.Value}"));
            }
        }

        if (results.Details is null) return;

        foreach (var detail in results.Details)
        {
            Collect(detail, issues);
        }
    }

    private static JsonSchema Load(bool lenient)
    {
        using var stream = typeof(IpxSchema).Assembly.GetManifestResourceStream(ResourceName) ?? throw new InvalidOperationException($"The schema {ResourceName} is not embedded in the assembly.");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var text = reader.ReadToEnd();
        if (!lenient) return JsonSchema.FromText(text);

        var node = JsonNode.Parse(text);
        if (node is JsonObject root && root["$id"] is JsonValue id && id.TryGetValue<string>(out var uri)) root["$id"] = uri.Replace(".json", "-lenient.json");
        Relax(node);
        return JsonSchema.FromText(node.ToJsonString());
    }

    private static void Relax(JsonNode node)
    {
        switch (node)
        {
            case JsonObject obj:
                obj.Remove("enum");
                if (obj["additionalProperties"] is JsonValue value && value.TryGetValue<bool>(out var allowed) && !allowed) obj.Remove("additionalProperties");
                foreach (var child in obj.Select(x => x.Value).ToList())
                {
                    if (child is not null) Relax(child);
                }

                break;
            case JsonArray array:
                foreach (var child in array)
                {
                    if (child is not null) Relax(child);
                }

                break;
        }
    }
}
