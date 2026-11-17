using System.Text.Json.Nodes;
using Eplicta.Ipx.Validation;

namespace Eplicta.Ipx.Tests;

public class ValidatorTests
{
    public static IEnumerable<object[]> SchemaDefects()
    {
        yield return Defect("an unknown top-level property", n => n["extra"] = 1);
        yield return Defect("a wrong format marker", n => n["format"] = "ipy");
        yield return Defect("a bad disposition", n => n["rows"][0]["retention"][0]["disposition"] = "burn");
        yield return Defect("a bad trigger", n => n["rows"][0]["retention"][0]["trigger"] = "whenever");
        yield return Defect("negative months", n => n["rows"][1]["retention"][0]["months"] = -3);
        yield return Defect("a row without retention", n => ((JsonObject)n["rows"][0]).Remove("retention"));
        yield return Defect("an empty retention list", n => n["rows"][0]["retention"] = new JsonArray());
        yield return Defect("an unknown school form", n => n["rows"][0]["forms"] = new JsonArray("university"));
        yield return Defect("an ext key that is not a reverse-domain name", n => n["rows"][0]["ext"] = new JsonObject { ["research"] = new JsonObject() });
        yield return Defect("text that is not a language map", n => n["document"]["name"] = "Plain text");
        yield return Defect("a date in the wrong form", n => n["document"]["updatedOn"] = "17/11/2026");
        yield return Defect("a period unit that does not exist", n => n["rows"][1]["retention"][0]["period"]["unit"] = "decade");
    }

    public static IEnumerable<object[]> SemanticDefects()
    {
        yield return Semantic("a duplicate row id", IpxIssueCodes.DuplicateId, n => n["rows"][1]["id"] = "r1");
        yield return Semantic("a duplicate node id", IpxIssueCodes.DuplicateId, n => n["nodes"][1]["id"] = "governance");
        yield return Semantic("a row under a node that does not exist", IpxIssueCodes.MissingReference, n => n["rows"][0]["node"] = "ghost");
        yield return Semantic("a row in a system that does not exist", IpxIssueCodes.MissingReference, n => n["rows"][0]["system"] = "sys-none");
        yield return Semantic("a node with a parent that does not exist", IpxIssueCodes.MissingReference, n => n["nodes"][1]["parent"] = "ghost");
        yield return Semantic("nodes that are each other's parent", IpxIssueCodes.ParentLoop, n => n["nodes"][0]["parent"] = "governance.board-minutes");
        yield return Semantic("a taxonomy with rows", IpxIssueCodes.TaxonomyHasRows, n => n["kind"] = "taxonomy");
    }

    public static IEnumerable<object[]> Warnings()
    {
        yield return Warning("months that do not match the period", IpxIssueCodes.MonthsMismatch, n => n["rows"][1]["retention"][0]["months"] = 5);
        yield return Warning("a period on a retain entry", IpxIssueCodes.RetainWithPeriod, n => n["rows"][0]["retention"][0]["months"] = 12);
        yield return Warning("a delegation to a plan that is not listed", IpxIssueCodes.DelegatesToUnknownPlan, n => n["provenance"]["plans"] = new JsonArray(new JsonObject { ["id"] = "p1", ["name"] = "A plan", ["delegatesTo"] = new JsonArray(new JsonObject { ["planId"] = "p9" }) }));
        yield return Warning("a newer minor version", IpxIssueCodes.NewerMinorVersion, n => n["formatVersion"] = "1.7.0");
    }

    private static object[] Defect(string name, Action<JsonObject> change) => [name, change];

    private static object[] Semantic(string name, string code, Action<JsonObject> change) => [name, code, change];

    private static object[] Warning(string name, string code, Action<JsonObject> change) => [name, code, change];

    private static IpxValidationResult Validate(Action<JsonObject> change, IpxValidationOptions options = null)
    {
        var node = TestFiles.ExampleNode();
        change(node);
        return IpxValidator.Validate(new IpxDocument(node), options);
    }

    [Fact]
    public void Errors_carry_the_path_of_what_is_wrong()
    {
        var result = Validate(n => n["rows"][0]["retention"][0]["disposition"] = "burn");

        Assert.Contains(result.Errors, e => e.Path.StartsWith("/rows/0/retention/0", StringComparison.Ordinal));
    }

    [Fact]
    public void Example_is_valid_without_warnings()
    {
        var result = IpxValidator.Validate(TestFiles.ExampleDocument());

        Assert.True(result.IsValid, string.Join("; ", result.Errors.Select(e => $"{e.Path}: {e.Message}")));
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Mapping_to_a_node_outside_the_taxonomy_is_a_warning_when_the_taxonomy_is_known()
    {
        var unknown = Validate(_ => { }, new IpxValidationOptions { TaxonomyNodeIds = ["something-else"] });
        var known = Validate(_ => { }, new IpxValidationOptions { TaxonomyNodeIds = ["compulsory-school.issue-final-grades"] });

        Assert.Contains(unknown.Warnings, w => w.Code == IpxIssueCodes.MappingNotInTaxonomy);
        Assert.DoesNotContain(known.Warnings, w => w.Code == IpxIssueCodes.MappingNotInTaxonomy);
        Assert.True(unknown.IsValid);
    }

    [Theory]
    [MemberData(nameof(SchemaDefects))]
    public void Schema_defects_are_errors_and_stop_the_semantic_rules(string defect, Action<JsonObject> change)
    {
        var result = Validate(change);

        Assert.False(result.IsValid, defect);
        Assert.All(result.Errors, e => Assert.Equal(IpxIssueCodes.Schema, e.Code));
    }

    [Theory]
    [MemberData(nameof(SemanticDefects))]
    public void Semantic_defects_are_errors_with_their_own_code(string defect, string code, Action<JsonObject> change)
    {
        var result = Validate(change);

        Assert.False(result.IsValid, defect);
        Assert.Contains(result.Errors, e => e.Code == code);
        Assert.DoesNotContain(result.Errors, e => e.Code == IpxIssueCodes.Schema);
    }

    [Theory]
    [MemberData(nameof(Warnings))]
    public void Warnings_do_not_make_a_document_invalid(string defect, string code, Action<JsonObject> change)
    {
        var result = Validate(change);

        Assert.True(result.IsValid, defect);
        Assert.Contains(result.Warnings, w => w.Code == code);
    }
}
