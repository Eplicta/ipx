using System.Text.Json.Nodes;
using Eplicta.Ipx.Tool;

namespace Eplicta.Ipx.Tests;

public sealed class CommandLineTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "ipx-tool-" + Guid.NewGuid().ToString("N"));

    public CommandLineTests()
    {
        Directory.CreateDirectory(_directory);
    }

    public void Dispose()
    {
        Directory.Delete(_directory, true);
    }

    private string Save(string name, IpxDocument document)
    {
        var path = Path.Combine(_directory, name);
        File.WriteAllText(path, IpxSerializer.ToJson(document));
        return path;
    }

    private string Taxonomy()
    {
        var builder = new IpxBuilder(IpxKind.Taxonomy).SetName("en", "Tree").SetLanguage("en");
        builder.AddNode("top", n =>
        {
            n.NodeKind = "group";
            n.Title = IpxText.Create("en", "Top");
        });
        builder.AddNode("leaf-a", n =>
        {
            n.NodeKind = "leaf";
            n.Parent = "top";
            n.Form = IpxForm.CompulsorySchool;
            n.Title = IpxText.Create("en", "Admission");
        });
        builder.AddNode("leaf-b", n =>
        {
            n.NodeKind = "leaf";
            n.Parent = "top";
            n.Form = IpxForm.CompulsorySchool;
            n.Title = IpxText.Create("en", "Grades");
        });
        return Save("taxonomy.ipx.json", builder.Build());
    }

    private string Plan(string file, string city, params (string Node, IpxRetention Retention)[] rows)
    {
        var builder = new IpxBuilder(IpxKind.Plan).SetName("en", city + " plan").SetLanguage("en").SetMunicipality("0380", city);
        var counter = 0;
        foreach (var (node, retention) in rows)
        {
            var row = new IpxRow { Id = "r" + counter++ };
            row.Forms.Add(IpxForm.CompulsorySchool);
            row.Retention.Add(retention);
            row.Mappings.Add(new IpxMapping { TaxonomyNode = node, Relation = IpxRelation.Exact });
            builder.AddRow(row);
        }

        return Save(file, builder.Build());
    }

    private static (int Code, string Out, string Err) Run(bool color, params string[] args)
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var code = IpxCommandLine.Run(args, output, error, color);
        return (code, output.ToString(), error.ToString());
    }

    private static (int Code, string Out, string Err) Run(params string[] args) => Run(false, args);

    private static IpxRetention Years(double years) => IpxRetention.Dispose(years, IpxPeriodUnit.Year, IpxTrigger.CaseClosed);

    [Fact]
    public void No_command_shows_the_commands_and_fails()
    {
        var (code, _, error) = Run();

        Assert.Equal(2, code);
        Assert.Contains("compare", error);
    }

    [Fact]
    public void Help_lists_the_commands_and_help_for_a_command_shows_its_usage()
    {
        var (code, output, _) = Run("help");
        var (topicCode, topic, _) = Run("help", "compare");
        var (flagCode, flag, _) = Run("compare", "--help");

        Assert.Equal(0, code);
        Assert.Contains("validate", output);
        Assert.Equal(0, topicCode);
        Assert.Contains("--only-differences", topic);
        Assert.Equal(0, flagCode);
        Assert.Contains("--only-differences", flag);
    }

    [Fact]
    public void An_unknown_command_or_option_is_a_usage_error()
    {
        var (code, _, error) = Run("frobnicate");
        var (optionCode, _, optionError) = Run("validate", "--nonsense", "x");

        Assert.Equal(2, code);
        Assert.Contains("frobnicate", error);
        Assert.Equal(2, optionCode);
        Assert.Contains("--nonsense", optionError);
    }

    [Fact]
    public void An_option_without_its_value_is_a_usage_error()
    {
        var (code, _, error) = Run("compare", "a", "b", "--form");

        Assert.Equal(2, code);
        Assert.Contains("--form", error);
    }

    [Fact]
    public void Validate_accepts_a_valid_document()
    {
        var path = Plan("a.ipx.json", "Uppsala", ("leaf-a", Years(6)));

        var (code, output, _) = Run("validate", path);

        Assert.Equal(0, code);
        Assert.Contains("valid", output);
    }

    [Fact]
    public void Validate_fails_with_the_errors_of_an_invalid_document()
    {
        var builder = new IpxBuilder(IpxKind.Plan).SetName("en", "Plan").SetLanguage("en");
        builder.AddRow("r1", IpxRowKind.Rule, r =>
        {
            r.NodeId = "missing";
            r.Forms.Add(IpxForm.Shared);
            r.Retention.Add(IpxRetention.Retain());
        });
        var path = Save("bad.ipx.json", builder.Build());

        var (code, output, _) = Run("validate", path);

        Assert.Equal(1, code);
        Assert.Contains("error", output);
        Assert.Contains("1 error", output);
    }

    [Fact]
    public void Validate_checks_every_file_and_fails_when_one_is_invalid()
    {
        var good = Plan("good.ipx.json", "Uppsala", ("leaf-a", Years(6)));
        var builder = new IpxBuilder(IpxKind.Plan).SetName("en", "Plan").SetLanguage("en");
        builder.AddRow("r1", IpxRowKind.Rule, r => r.NodeId = "missing");
        var bad = Save("bad.ipx.json", builder.Build());

        var (code, output, _) = Run("validate", good, bad);

        Assert.Equal(1, code);
        Assert.Contains("good.ipx.json: valid", output);
    }

    [Fact]
    public void An_unreadable_or_foreign_file_is_a_usage_error_that_names_the_file()
    {
        var notIpx = Path.Combine(_directory, "other.json");
        File.WriteAllText(notIpx, "{\"format\":\"something-else\"}");
        var missing = Path.Combine(_directory, "absent.json");

        var (code, _, error) = Run("validate", notIpx);
        var (missingCode, _, missingError) = Run("validate", missing);

        Assert.Equal(2, code);
        Assert.Contains("other.json", error);
        Assert.Equal(2, missingCode);
        Assert.Contains("absent.json", missingError);
    }

    [Fact]
    public void Validate_needs_a_file()
    {
        Assert.Equal(2, Run("validate").Code);
    }

    [Fact]
    public void Info_shows_what_the_document_holds()
    {
        var path = Plan("a.ipx.json", "Uppsala", ("leaf-a", Years(6)), ("leaf-b", Years(2)));

        var (code, output, _) = Run("info", path);

        Assert.Equal(0, code);
        Assert.Contains("Uppsala plan", output);
        Assert.Contains("kind plan", output);
        Assert.Contains("Uppsala (0380)", output);
        Assert.Contains("2 rows", output);
        Assert.Contains("compulsory-school", output);
    }

    [Fact]
    public void Compare_shows_each_plan_against_the_base()
    {
        var taxonomy = Taxonomy();
        var baseline = Plan("base.ipx.json", "Basetown", ("leaf-a", Years(6)), ("leaf-b", Years(6)));
        var other = Plan("other.ipx.json", "Othertown", ("leaf-a", Years(6)), ("leaf-b", Years(2)));

        var (code, output, _) = Run("compare", taxonomy, baseline, other);

        Assert.Equal(0, code);
        Assert.Contains("BASE Basetown", output);
        Assert.Contains("Othertown", output);
        Assert.Contains("Admission", output);
        Assert.Contains("Grades", output);
        Assert.Contains("6y/case-closed", output);
        Assert.Contains("2y/case-closed", output);
        Assert.Contains("compulsory-school", output);
    }

    [Fact]
    public void Compare_can_show_only_the_differences()
    {
        var taxonomy = Taxonomy();
        var baseline = Plan("base.ipx.json", "Basetown", ("leaf-a", Years(6)), ("leaf-b", Years(6)));
        var other = Plan("other.ipx.json", "Othertown", ("leaf-a", Years(6)), ("leaf-b", Years(2)));

        var (_, output, _) = Run("compare", taxonomy, baseline, other, "--only-differences", "--no-legend");

        Assert.Contains("Grades", output);
        Assert.DoesNotContain("Admission", output);
    }

    [Fact]
    public void Compare_limits_to_a_form_and_refuses_one_that_does_not_exist()
    {
        var taxonomy = Taxonomy();
        var baseline = Plan("base.ipx.json", "Basetown", ("leaf-a", Years(6)));

        var (_, output, _) = Run("compare", taxonomy, baseline, "--form", "preschool", "--no-legend");
        var (code, _, error) = Run("compare", taxonomy, baseline, "--form", "nonsense");

        Assert.DoesNotContain("Admission", output);
        Assert.Equal(2, code);
        Assert.Contains("nonsense", error);
    }

    [Fact]
    public void Compare_uses_colour_only_when_asked_to()
    {
        var taxonomy = Taxonomy();
        var baseline = Plan("base.ipx.json", "Basetown", ("leaf-a", Years(6)));
        var other = Plan("other.ipx.json", "Othertown", ("leaf-a", Years(2)));

        var plain = Run(false, "compare", taxonomy, baseline, other).Out;
        var coloured = Run(true, "compare", taxonomy, baseline, other).Out;

        Assert.DoesNotContain("\u001b", plain, StringComparison.Ordinal);
        Assert.Contains("\u001b", coloured, StringComparison.Ordinal);
    }

    [Fact]
    public void Compare_needs_a_taxonomy_and_a_base()
    {
        Assert.Equal(2, Run("compare", Taxonomy()).Code);
    }

    [Fact]
    public void Coverage_counts_the_nodes_each_plan_has_a_rule_for()
    {
        var taxonomy = Taxonomy();
        var one = Plan("one.ipx.json", "Onetown", ("leaf-a", Years(6)));
        var two = Plan("two.ipx.json", "Twotown", ("leaf-a", Years(6)), ("leaf-b", Years(2)));

        var (code, output, _) = Run("coverage", taxonomy, one, two);

        Assert.Equal(0, code);
        Assert.Contains("1/2", output);
        Assert.Contains("2/2", output);
    }

    [Fact]
    public void Gaps_lists_the_nodes_where_plans_disagree()
    {
        var taxonomy = Taxonomy();
        var one = Plan("one.ipx.json", "Onetown", ("leaf-a", Years(6)), ("leaf-b", Years(2)));
        var two = Plan("two.ipx.json", "Twotown", ("leaf-a", Years(6)), ("leaf-b", Years(4)));

        var (code, output, _) = Run("gaps", taxonomy, one, two);

        Assert.Equal(0, code);
        Assert.Contains("leaf-b", output);
        Assert.DoesNotContain("leaf-a ", output);
        Assert.Contains("1 nodes listed", output);
    }

    [Fact]
    public void Missing_lists_what_the_documents_say_is_missing()
    {
        var builder = new IpxBuilder(IpxKind.Plan).SetName("en", "Plan").SetLanguage("en").SetMunicipality("0380", "Uppsala");
        builder.Configure(d => d.EnsureProvenance().Node["missing"] = new JsonArray(
            new JsonObject { ["id"] = "m1", ["kind"] = "plan-not-public", ["nextStep"] = "contact-city", ["detail"] = "The plan is not published." },
            new JsonObject { ["id"] = "m2", ["kind"] = "plan-not-found", ["nextStep"] = "retry-later", ["detail"] = "No plan found." }));
        var path = Save("missing.ipx.json", builder.Build());

        var (_, all, _) = Run("missing", path);
        var (_, filtered, _) = Run("missing", path, "--next-step", "contact-city");

        Assert.Contains("plan-not-public", all);
        Assert.Contains("plan-not-found", all);
        Assert.Contains("plan-not-public", filtered);
        Assert.DoesNotContain("plan-not-found", filtered);
        Assert.Contains("contact-city", filtered);
    }
}
