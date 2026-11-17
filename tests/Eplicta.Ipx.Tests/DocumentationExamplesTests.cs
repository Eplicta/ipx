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
