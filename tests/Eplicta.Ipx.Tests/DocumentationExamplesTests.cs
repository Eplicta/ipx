using Eplicta.Ipx.Validation;

namespace Eplicta.Ipx.Tests;

public class DocumentationExamplesTests
{
    [Fact]
    public void Building_a_plan_from_code_gives_a_valid_document()
    {
        var plan = IpxDocument.Create(IpxKind.Plan, Guid.NewGuid().ToString(), IpxText.Create(("sv", "Exempelskolans plan"), ("en", "Example School plan")), "sv");

        var governance = new IpxNode { Id = "governance", Code = "1" };
        governance.Title = IpxText.Create(("sv", "Styra verksamheten"), ("en", "Govern the operation"));
        plan.Nodes.Add(governance);

        var minutes = new IpxRow { Id = "board-minutes", RowKind = IpxRowKind.RecordType, NodeId = "governance", Registration = IpxRegistration.Registered };
        minutes.Name = IpxText.Create(("sv", "Styrelseprotokoll"), ("en", "Board minutes"));
        minutes.Forms.Add("shared");
        minutes.Retention.Add(new IpxRetention { Disposition = IpxDisposition.Retain, Trigger = IpxTrigger.None });
        plan.Rows.Add(minutes);

        var result = IpxValidator.Validate(plan);

        Assert.True(result.IsValid);
        Assert.Contains("\"rowKind\": \"recordType\"", IpxSerializer.ToJson(plan));
    }

    [Fact]
    public void The_builder_example_gives_a_valid_document()
    {
        var plan = new IpxBuilder(IpxKind.Plan)
            .SetName("sv", "Exempelskolans plan")
            .SetName("en", "Example School plan")
            .SetLanguage("sv")
            .SetOrganisation("Exempelskolan AB", o => o.Forms.Add(IpxForm.CompulsorySchool))
            .AddNode("governance", n =>
            {
                n.Code = "1";
                n.Title = IpxText.Create(("sv", "Styra verksamheten"), ("en", "Govern the operation"));
            })
            .AddRow("board-minutes", IpxRowKind.RecordType, r =>
            {
                r.Name = IpxText.Create(("sv", "Styrelseprotokoll"), ("en", "Board minutes"));
                r.NodeId = "governance";
                r.Forms.Add(IpxForm.Shared);
                r.Registration = IpxRegistration.Registered;
                r.Retention.Add(IpxRetention.Retain());
            })
            .AddRow("admission", IpxRowKind.CaseType, r =>
            {
                r.Name = IpxText.Create(("sv", "Ansökan om skolplats"), ("en", "Application for a school place"));
                r.NodeId = "governance";
                r.Forms.Add(IpxForm.CompulsorySchool);
                r.Retention.Add(IpxRetention.Dispose(6, IpxPeriodUnit.Year, IpxTrigger.CaseClosed));
            })
            .BuildValidated();

        Assert.Equal(2, plan.Rows.Count);
        Assert.Contains("\"months\": 72", IpxSerializer.ToJson(plan));
    }

    [Fact]
    public void The_getting_started_builder_example_gives_a_valid_document()
    {
        var plan = new IpxBuilder(IpxKind.Plan)
            .SetName("sv", "Exempelskolans plan")
            .SetName("en", "Example School plan")
            .SetLanguage("sv")
            .AddNode("governance", n => n.Title = IpxText.Create(("sv", "Styra verksamheten"), ("en", "Govern the operation")))
            .AddRow("board-minutes", IpxRowKind.RecordType, r =>
            {
                r.Name = IpxText.Create(("sv", "Styrelseprotokoll"), ("en", "Board minutes"));
                r.NodeId = "governance";
                r.Forms.Add(IpxForm.Shared);
                r.Retention.Add(IpxRetention.Retain());
            })
            .BuildValidated();

        Assert.Single(plan.Rows);
    }

    [Fact]
    public void The_retention_example_with_an_outcome_and_a_condition_validates()
    {
        var entry = IpxRetention.Dispose(2, IpxPeriodUnit.Year, IpxTrigger.CaseClosed);
        entry.Outcome = IpxOutcome.Refusal;
        entry.Condition = "Unless the decision is appealed.";

        var plan = new IpxBuilder(IpxKind.Plan)
            .SetName("sv", "Plan")
            .SetLanguage("sv")
            .AddRow("r1", IpxRowKind.CaseType, r =>
            {
                r.Forms.Add(IpxForm.Shared);
                r.Retention.Add(entry);
            })
            .BuildValidated();

        Assert.Equal(24, plan.Rows[0].Retention[0].Months);
    }

    [Fact]
    public void Reading_retention_from_the_example_gives_the_documented_values()
    {
        var document = TestFiles.ExampleDocument();
        var lines = new List<string>();

        foreach (var row in document.Rows.Where(r => r.RowKind == IpxRowKind.CaseType))
        {
            foreach (var entry in row.Retention)
            {
                var months = entry.Months is int value ? $"{value} months" : "no period";
                lines.Add($"{row.Id} {entry.Outcome}: {entry.Disposition}, {months}");
            }
        }

        Assert.Equal(["r2 Grant: Dispose, 72 months", "r2 Refusal: Dispose, 24 months"], lines);
    }

    [Fact]
    public void Unknown_content_survives_a_change_made_through_the_model()
    {
        var document = IpxSerializer.Parse(TestFiles.Example().Replace("\"id\": \"r1\",", "\"id\": \"r1\",\n      \"addedByNewerVersion\": { \"keep\": true },"));

        document.Rows[0].CurrentNote = "Edited by the application";
        var written = IpxSerializer.ToJson(document);

        Assert.Contains("\"addedByNewerVersion\": {", written);
        Assert.Contains("\"currentNote\": \"Edited by the application\"", written);
    }
}
