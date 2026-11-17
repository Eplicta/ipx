using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;

namespace Eplicta.Ipx.Validation;

internal static class IpxSchema
{
    private const string ResourceName = "Eplicta.Ipx.ipx-1.0.0.schema.json";

    private static readonly Lazy<JsonSchema> Schema = new(Load);

    public static List<IpxIssue> Check(JsonNode instance)
    {
        var results = Schema.Value.Evaluate(JsonSerializer.SerializeToElement(instance), new EvaluationOptions { OutputFormat = OutputFormat.List });
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

    private static JsonSchema Load()
    {
        using var stream = typeof(IpxSchema).Assembly.GetManifestResourceStream(ResourceName) ?? throw new InvalidOperationException($"The schema {ResourceName} is not embedded in the assembly.");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return JsonSchema.FromText(reader.ReadToEnd());
    }
}
