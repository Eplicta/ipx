using System.Globalization;
using System.Text.Json.Nodes;
using Eplicta.Ipx.Validation;

namespace Eplicta.Ipx.Tests;

public class BuilderTests
{
    private const string Id = "0b6c8c58-4f7e-4f43-9a1b-2f1d7a9c3e10";

    private static IpxBuilder Minimal() => new IpxBuilder(IpxKind.Plan).SetId(Id).SetName("sv", "Plan").SetLanguage("sv");

    private static IpxBuilder School()
    {
        return new IpxBuilder(IpxKind.Plan)
            .SetId(Id)
            .SetKey("example-school-plan")
            .SetName("sv", "Exempelskolans plan").SetName("en", "Example School plan")
            .SetLanguage("sv")
            .SetNoteLanguage("en")
            .SetStatus("draft")
            .SetCreatedOn(new DateOnly(2026, 11, 17))
            .SetValidFrom("2027-01")
            .SetGenerator("FortDocs", "1.0")
            .SetMunicipality("0380", "Uppsala", "Uppsala län")
            .SetOrganisation("Exempelskolan AB", o =>
            {
                o.LegalForm = "aktiebolag";
                o.Forms.Add(IpxForm.CompulsorySchool);
                o.Regime = "smaller-16f";
            })
            .AddResponsibility("Rektor", "Adopts the plan.")
            .AddOperator("Utbildningsnämnden", [IpxForm.CompulsorySchool], "The committee.")
            .AddSystem("sys-diary", "Diary", s => s.Kind = "diary")
            .AddNode("governance", n =>
            {
                n.Code = "1";
                n.Title = IpxText.Create("sv", "Styra");
            })
            .AddNode("pupils", n => n.Title = IpxText.Create("sv", "Bedriva skola"))
            .AddRow("r1", IpxRowKind.RecordType, r =>
            {
                r.NodeId = "governance";
                r.Name = IpxText.Create("en", "Board minutes");
                r.Forms.Add(IpxForm.Shared);
                r.Registration = IpxRegistration.Registered;
                r.Retention.Add(IpxRetention.Retain());
            })
            .AddRow("r2", IpxRowKind.CaseType, r =>
            {
                r.NodeId = "pupils";
                r.Forms.Add(IpxForm.CompulsorySchool);
                r.Retention.Add(IpxRetention.Dispose(6, IpxPeriodUnit.Year, IpxTrigger.CaseClosed));
            })
            .AddOrigin(o => o.Standard = "fortdocs")
            .AddConversion(c =>
            {
                c.On = "2026-11-17";
                c.From = "fortdocs";
                c.To = "ipx";
            })
            .SetExtension("app.fortdocs.test", JsonValue.Create(1));
    }

    [Fact]
    public void A_built_plan_is_valid_without_warnings()
    {
        var document = School().BuildValidated();

        var result = IpxValidator.Validate(document);

        Assert.Empty(result.Errors);
        Assert.Empty(result.Warnings);
        Assert.Equal(2, document.Rows.Count);
        Assert.Equal("Uppsala", document.Municipality.Name);
        Assert.Equal("Exempelskolan AB", document.Organisation.Name);
        Assert.Equal(72, document.Rows[1].Retention[0].Months);
    }

    [Fact]
    public void The_text_survives_being_written_and_read_again()
    {
        var document = School().Build();

        var json = IpxSerializer.ToJson(document);
        var again = IpxSerializer.Parse(json);

        Assert.Equal(json, IpxSerializer.ToJson(again));
        Assert.Equal(IpxCanonicalJson.Serialize(document.Node), IpxCanonicalJson.Serialize(again.Node));
    }

    [Fact]
    public void Properties_are_written_in_the_order_of_the_schema_whatever_the_order_they_were_set()
    {
        var document = Minimal()
            .SetValidFrom("2027")
            .SetKey("k")
            .AddRow("r1", IpxRowKind.RecordType, r =>
            {
                r.Registration = IpxRegistration.Registered;
                r.NodeId = "n1";
                r.Name = IpxText.Create("en", "Name");
                r.Retention.Add(IpxRetention.Retain());
            })
            .AddNode("n1")
            .AddSystem("s1", "System")
            .Build();

        Assert.Equal(["format", "formatVersion", "kind", "document", "systems", "nodes", "rows"], document.Node.Select(x => x.Key));
        Assert.Equal(["id", "key", "name", "language", "validFrom"], document.Document.Node.Select(x => x.Key));
        Assert.Equal(["id", "rowKind", "forms", "name", "node", "registration", "retention"], document.Rows[0].Node.Select(x => x.Key));
    }

    [Fact]
    public void Properties_the_schema_does_not_list_keep_their_place_after_the_listed_ones()
    {
        var document = Minimal().Configure(d => d.Document.Node["x-extra"] = 1).SetKey("k").Build();

        Assert.Equal(["id", "key", "name", "language", "x-extra"], document.Document.Node.Select(x => x.Key));
    }

    [Fact]
    public void The_same_input_gives_the_same_text()
    {
        Assert.Equal(IpxSerializer.ToJson(School().Build()), IpxSerializer.ToJson(School().Build()));
    }

    [Fact]
    public void A_document_without_an_id_gets_one_that_stays_the_same_when_built_again()
    {
        var builder = new IpxBuilder(IpxKind.Taxonomy).SetName("en", "Tree").SetLanguage("en");

        var first = builder.Build();
        var second = builder.Build();

        Assert.True(Guid.TryParse(first.Document.Id, out _));
        Assert.Equal(first.Document.Id, second.Document.Id);
    }

    [Fact]
    public void Building_gives_copies_so_a_later_change_to_the_builder_does_not_reach_the_first_document()
    {
        var builder = Minimal();
        var first = builder.Build();

        builder.AddNode("n1");
        var second = builder.Build();

        Assert.Empty(first.Nodes);
        Assert.Single(second.Nodes);
    }

    [Fact]
    public void An_added_node_is_a_copy_of_the_one_given()
    {
        var node = new IpxNode { Id = "n1", Code = "1" };
        var document = Minimal().AddNode(node).Build();

        node.Code = "changed";

        Assert.Equal("1", document.Nodes[0].Code);
    }

    [Fact]
    public void The_same_text_can_be_used_in_two_builders()
    {
        var name = IpxText.Create("sv", "Delad");

        var first = new IpxBuilder(IpxKind.Plan).SetName(name).SetLanguage("sv").Build();
        var second = new IpxBuilder(IpxKind.Plan).SetName(name).SetLanguage("sv").Build();

        Assert.Equal("Delad", first.Document.Name["sv"]);
        Assert.Equal("Delad", second.Document.Name["sv"]);
    }

    [Fact]
    public void A_missing_name_or_language_is_reported_together()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => new IpxBuilder(IpxKind.Plan).Build());

        Assert.Contains("a name", exception.Message);
        Assert.Contains("a language", exception.Message);
        Assert.Throws<InvalidOperationException>(() => new IpxBuilder(IpxKind.Plan).SetName("sv", "Plan").Build());
    }

    [Theory]
    [InlineData("node")]
    [InlineData("row")]
    [InlineData("system")]
    public void A_duplicate_id_is_refused_when_it_is_added(string what)
    {
        var builder = Minimal().AddNode("n1").AddRow("r1", IpxRowKind.Rule).AddSystem("s1", "System");

        Action add = what switch
        {
            "node" => () => builder.AddNode("n1"),
            "row" => () => builder.AddRow("r1", IpxRowKind.Rule),
            _ => () => builder.AddSystem("s1", "Other")
        };

        var exception = Assert.Throws<ArgumentException>(add);
        Assert.Contains(what, exception.Message);
    }

    [Fact]
    public void An_item_without_an_id_is_refused()
    {
        Assert.Throws<ArgumentException>(() => Minimal().AddNode(new IpxNode()));
    }

    [Fact]
    public void BuildValidated_throws_with_every_error_when_the_content_is_wrong()
    {
        var builder = Minimal().AddRow("r1", IpxRowKind.Rule, r =>
        {
            r.NodeId = "missing";
            r.Forms.Add(IpxForm.Shared);
            r.Retention.Add(IpxRetention.Retain());
        });

        var exception = Assert.Throws<IpxValidationException>(() => builder.BuildValidated());

        Assert.Contains(exception.Result.Errors, x => x.Code == IpxIssueCodes.MissingReference);
        Assert.Contains("not valid", exception.Message);
        Assert.NotNull(builder.Build());
    }

    [Fact]
    public void Dates_are_written_as_iso_dates_whatever_the_culture()
    {
        var current = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("ar-SA");
            var document = Minimal().SetCreatedOn(new DateOnly(2026, 11, 17)).SetUpdatedOn(new DateOnly(2026, 12, 1)).SetValidFrom(new DateOnly(2027, 1, 1)).SetValidTo(new DateOnly(2028, 1, 1)).Build();

            Assert.Equal("2026-11-17", document.Document.CreatedOn);
            Assert.Equal("2026-12-01", document.Document.UpdatedOn);
            Assert.Equal("2027-01-01", document.Document.ValidFrom);
            Assert.Equal("2028-01-01", document.Document.ValidTo);
        }
        finally
        {
            CultureInfo.CurrentCulture = current;
        }
    }

    [Fact]
    public void An_extension_value_is_copied()
    {
        var value = new JsonObject { ["a"] = 1 };
        var document = Minimal().SetExtension("app.example", value).Build();

        value["a"] = 2;

        Assert.Equal(1, (int)document.Ext.Get("app.example")["a"]);
    }

    [Fact]
    public void Origins_and_conversions_are_recorded_under_provenance()
    {
        var document = Minimal()
            .AddOrigin(o =>
            {
                o.Standard = "skr";
                o.Version = "2026";
            })
            .AddConversion(c =>
            {
                c.On = "2026-11-17";
                c.From = "skr";
                c.To = "ipx";
                c.Losses.Add("layout");
            })
            .Build();

        Assert.Equal("skr", document.Provenance.Origins[0].Standard);
        Assert.Equal("layout", document.Provenance.Conversions[0].Losses[0]);
    }

    [Theory]
    [InlineData(6, IpxPeriodUnit.Year, 72)]
    [InlineData(2.5, IpxPeriodUnit.Year, 30)]
    [InlineData(18, IpxPeriodUnit.Month, 18)]
    [InlineData(45, IpxPeriodUnit.Day, 2)]
    public void A_period_gives_its_months_rounded_up(double amount, IpxPeriodUnit unit, int months)
    {
        Assert.Equal(months, IpxPeriod.Create(amount, unit).ToMonths());
        Assert.Equal(months, IpxRetention.Dispose(amount, unit, IpxTrigger.CaseClosed).Months);
        Assert.Equal(months, IpxRetention.Handover(amount, unit, IpxTrigger.CaseClosed).Months);
    }

    [Fact]
    public void Retention_factories_set_the_disposition_and_the_trigger()
    {
        var retain = IpxRetention.Retain();
        var dispose = IpxRetention.Dispose(IpxTrigger.Superseded);
        var handover = IpxRetention.Handover(10, IpxPeriodUnit.Year, IpxTrigger.YearEnd);

        Assert.Equal(IpxDisposition.Retain, retain.Disposition);
        Assert.Equal(IpxTrigger.None, retain.Trigger);
        Assert.Null(retain.Months);
        Assert.Equal(IpxDisposition.Dispose, dispose.Disposition);
        Assert.Equal(IpxTrigger.Superseded, dispose.Trigger);
        Assert.Null(dispose.Period);
        Assert.Equal(IpxDisposition.Handover, handover.Disposition);
        Assert.Equal(120, handover.Months);
    }

    [Fact]
    public void Text_in_one_language_can_be_made_in_one_call()
    {
        Assert.Equal("Hej", IpxText.Create("sv", "Hej")["sv"]);
    }

    [Fact]
    public void A_row_without_forms_gets_an_empty_list_so_it_is_valid()
    {
        var document = Minimal().AddNode("n1", n => n.Title = IpxText.Create("en", "Node")).AddRow("r1", IpxRowKind.Process, r =>
        {
            r.NodeId = "n1";
            r.Retention.Add(IpxRetention.Retain());
        }).BuildValidated();

        Assert.Empty(document.Rows[0].Forms);
        Assert.True(document.Rows[0].Has("forms"));
    }

    [Fact]
    public void A_node_has_typed_validity_dates()
    {
        var document = Minimal().AddNode("n1", n =>
        {
            n.Title = IpxText.Create("en", "Node");
            n.ValidFrom = "2025";
            n.ValidTo = "2026-03-04";
        }).BuildValidated();

        Assert.Equal("2025", document.Nodes[0].ValidFrom);
        Assert.Equal("2026-03-04", document.Nodes[0].ValidTo);
    }

    [Theory]
    [InlineData("a", true)]
    [InlineData("0.1:a_b-c", true)]
    [InlineData("A1234567890", true)]
    [InlineData("-leading", false)]
    [InlineData("has space", false)]
    [InlineData("åäö", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void An_id_is_checked_the_way_the_schema_does(string id, bool valid)
    {
        Assert.Equal(valid, IpxId.IsValid(id));
    }

    [Fact]
    public void An_id_of_128_characters_is_valid_and_one_of_129_is_not()
    {
        Assert.True(IpxId.IsValid(new string('a', 128)));
        Assert.False(IpxId.IsValid(new string('a', 129)));
    }
}
