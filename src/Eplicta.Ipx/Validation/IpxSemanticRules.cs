using System.Text.Json.Nodes;

namespace Eplicta.Ipx.Validation;

internal static class IpxSemanticRules
{
    private const int SupportedMinorVersion = 0;
    private const double DaysPerMonth = 30;
    private const double MonthsPerYear = 12;

    public static IpxValidationResult Check(JsonObject root, IpxValidationOptions options)
    {
        var errors = new List<IpxIssue>();
        var warnings = new List<IpxIssue>();

        var provenance = root["provenance"] as JsonObject;
        var nodes = Items(root, "nodes");
        var rows = Items(root, "rows");
        var systems = Items(root, "systems");
        var sources = Items(provenance, "sources");
        var plans = Items(provenance, "plans");
        var missing = Items(provenance, "missing");

        CheckDuplicates("/nodes", nodes, errors);
        CheckDuplicates("/rows", rows, errors);
        CheckDuplicates("/systems", systems, errors);
        CheckDuplicates("/provenance/sources", sources, errors);
        CheckDuplicates("/provenance/plans", plans, errors);
        CheckDuplicates("/provenance/missing", missing, errors);

        if (Text(root, "kind") == "taxonomy" && rows.Count > 0)
        {
            errors.Add(new IpxIssue(IpxIssueCodes.TaxonomyHasRows, "/rows", "A taxonomy document has no rows."));
        }

        var nodeIds = Ids(nodes);
        var systemIds = Ids(systems);
        var sourceIds = Ids(sources);
        var planIds = Ids(plans);
        var taxonomyIds = options?.TaxonomyNodeIds is null ? null : new HashSet<string>(options.TaxonomyNodeIds, StringComparer.Ordinal);

        CheckNodes(nodes, nodeIds, systemIds, errors);
        CheckRows(rows, nodeIds, systemIds, planIds, sourceIds, taxonomyIds, errors, warnings);
        CheckPlans(plans, planIds, sourceIds, errors, warnings);
        CheckPupilTotal(root, sourceIds, errors);
        CheckVersion(root, warnings);

        return new IpxValidationResult(errors, warnings);
    }

    private static void CheckDuplicates(string path, List<JsonObject> items, List<IpxIssue> errors)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < items.Count; index++)
        {
            var id = Text(items[index], "id");
            if (id is not null && !seen.Add(id)) errors.Add(new IpxIssue(IpxIssueCodes.DuplicateId, $"{path}/{index}/id", $"The id '{id}' is used twice."));
        }
    }

    private static void CheckNodes(List<JsonObject> nodes, HashSet<string> nodeIds, HashSet<string> systemIds, List<IpxIssue> errors)
    {
        var parents = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var index = 0; index < nodes.Count; index++)
        {
            var id = Text(nodes[index], "id");
            var parent = Text(nodes[index], "parent");
            if (id is not null) parents[id] = parent;

            if (parent is not null && !nodeIds.Contains(parent)) errors.Add(new IpxIssue(IpxIssueCodes.MissingReference, $"/nodes/{index}/parent", $"The parent '{parent}' does not exist."));

            var system = Text(nodes[index], "system");
            if (system is not null && !systemIds.Contains(system)) errors.Add(new IpxIssue(IpxIssueCodes.MissingReference, $"/nodes/{index}/system", $"The system '{system}' does not exist."));
        }

        CheckParentLoops(nodes, parents, errors);
    }

    private static void CheckParentLoops(List<JsonObject> nodes, Dictionary<string, string> parents, List<IpxIssue> errors)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < nodes.Count; index++)
        {
            var current = Text(nodes[index], "id");
            var path = new List<string>();
            while (current is not null && parents.ContainsKey(current) && !visited.Contains(current))
            {
                if (path.Contains(current))
                {
                    errors.Add(new IpxIssue(IpxIssueCodes.ParentLoop, $"/nodes/{index}/parent", $"The parent chain of '{current}' loops."));
                    break;
                }

                path.Add(current);
                current = parents[current];
            }

            visited.UnionWith(path);
        }
    }

    private static void CheckRows(List<JsonObject> rows, HashSet<string> nodeIds, HashSet<string> systemIds, HashSet<string> planIds, HashSet<string> sourceIds, HashSet<string> taxonomyIds, List<IpxIssue> errors, List<IpxIssue> warnings)
    {
        for (var index = 0; index < rows.Count; index++)
        {
            var row = rows[index];
            var path = $"/rows/{index}";

            Require(row, "node", nodeIds, path, errors);
            Require(row, "system", systemIds, path, errors);
            Require(row, "plan", planIds, path, errors, "is not in provenance.plans");
            if (row["source"] is JsonObject source) Require(source, "ref", sourceIds, path + "/source", errors, "is not in provenance.sources");

            if (taxonomyIds is not null) CheckMappings(row, path, taxonomyIds, warnings);
            CheckRetention(row, path, warnings);
        }
    }

    private static void CheckMappings(JsonObject row, string path, HashSet<string> taxonomyIds, List<IpxIssue> warnings)
    {
        var mappings = Items(row, "mappings");
        for (var index = 0; index < mappings.Count; index++)
        {
            var node = Text(mappings[index], "node");
            if (node is not null && !taxonomyIds.Contains(node)) warnings.Add(new IpxIssue(IpxIssueCodes.MappingNotInTaxonomy, $"{path}/mappings/{index}/node", $"The node '{node}' is not in the taxonomy."));
        }
    }

    private static void CheckRetention(JsonObject row, string path, List<IpxIssue> warnings)
    {
        var entries = Items(row, "retention");
        for (var index = 0; index < entries.Count; index++)
        {
            var entry = entries[index];
            var entryPath = $"{path}/retention/{index}";
            var months = Whole(entry, "months");

            if (entry["period"] is JsonObject period && months is not null && MonthsOf(period) is { } expected && expected != months)
            {
                warnings.Add(new IpxIssue(IpxIssueCodes.MonthsMismatch, entryPath + "/months", $"The period is {expected} months, not {months}."));
            }

            if (Text(entry, "disposition") == "retain" && months is not null)
            {
                warnings.Add(new IpxIssue(IpxIssueCodes.RetainWithPeriod, entryPath + "/months", "A retain entry has a period."));
            }
        }
    }

    private static void CheckPlans(List<JsonObject> plans, HashSet<string> planIds, HashSet<string> sourceIds, List<IpxIssue> errors, List<IpxIssue> warnings)
    {
        for (var index = 0; index < plans.Count; index++)
        {
            var plan = plans[index];
            var path = $"/provenance/plans/{index}";

            if (plan["sourceIds"] is JsonArray ids)
            {
                for (var position = 0; position < ids.Count; position++)
                {
                    var id = (string)ids[position];
                    if (!sourceIds.Contains(id)) errors.Add(new IpxIssue(IpxIssueCodes.MissingReference, $"{path}/sourceIds/{position}", $"The source '{id}' is not in provenance.sources."));
                }
            }

            var delegations = Items(plan, "delegatesTo");
            for (var position = 0; position < delegations.Count; position++)
            {
                var target = Text(delegations[position], "planId");
                if (target is not null && !planIds.Contains(target)) warnings.Add(new IpxIssue(IpxIssueCodes.DelegatesToUnknownPlan, $"{path}/delegatesTo/{position}/planId", $"The plan '{target}' is not in provenance.plans."));
            }
        }
    }

    private static void CheckPupilTotal(JsonObject root, HashSet<string> sourceIds, List<IpxIssue> errors)
    {
        if (root["organisation"]?["pupilTotal"] is JsonObject total) Require(total, "sourceId", sourceIds, "/organisation/pupilTotal", errors, "is not in provenance.sources");
    }

    private static void CheckVersion(JsonObject root, List<IpxIssue> warnings)
    {
        var parts = (Text(root, "formatVersion") ?? "").Split('.');
        if (parts.Length == 3 && int.TryParse(parts[1], out var minor) && minor > SupportedMinorVersion)
        {
            warnings.Add(new IpxIssue(IpxIssueCodes.NewerMinorVersion, "/formatVersion", $"Version {string.Join('.', parts)} is newer than this validator knows; properties it added are not checked."));
        }
    }

    private static void Require(JsonObject owner, string property, HashSet<string> ids, string path, List<IpxIssue> errors, string reason = "does not exist")
    {
        var value = Text(owner, property);
        if (value is not null && !ids.Contains(value)) errors.Add(new IpxIssue(IpxIssueCodes.MissingReference, $"{path}/{property}", $"'{value}' {reason}."));
    }

    private static int? MonthsOf(JsonObject period)
    {
        if (!(period["amount"] is JsonValue amountValue && amountValue.TryGetValue<double>(out var amount))) return null;

        return Text(period, "unit") switch
        {
            "year" => (int)Math.Ceiling(amount * MonthsPerYear),
            "month" => (int)Math.Ceiling(amount),
            "day" => (int)Math.Ceiling(amount / DaysPerMonth),
            _ => null
        };
    }

    private static int? Whole(JsonObject owner, string property) => owner[property] is JsonValue value && value.TryGetValue<int>(out var number) ? number : null;

    private static string Text(JsonNode owner, string property) => owner?[property] is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;

    private static HashSet<string> Ids(List<JsonObject> items) => items.Select(x => Text(x, "id")).Where(x => x is not null).ToHashSet(StringComparer.Ordinal);

    private static List<JsonObject> Items(JsonNode owner, string property) => owner?[property] is JsonArray array ? array.OfType<JsonObject>().ToList() : [];
}
