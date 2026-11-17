using System.Text.Json.Nodes;

namespace Eplicta.Ipx.Tool.Commands;

internal sealed class InfoCommand : ICommand
{
    public string Name => "info";

    public string Summary => "Show what a document is and how much it holds.";

    public string Usage => "ipx info <file> [--language <tag>]\n\nShows the kind, the name, the municipality and the number of nodes, rows and provenance entries, and the rows per school form.";

    public IReadOnlyCollection<string> Flags => [];

    public IReadOnlyCollection<string> Options => ["language"];

    public int Run(CommandArguments arguments, Output output)
    {
        if (arguments.Positional.Count != 1) throw new UsageException("Give one file. " + Usage);

        var language = arguments.Option("language");
        var document = DocumentFiles.Read(arguments.Positional[0]);
        var info = document.Document;

        output.Line(DocumentFiles.TextOf(info?.Name, language) ?? "(no name)", OutputColor.White);
        output.Line($"  kind {(document.Kind is { } kind ? IpxWire.ToWire(kind) : "?")}, IPX {document.FormatVersion}, status {info?.Status ?? "?"}, language {info?.Language ?? "?"}");

        if (document.Municipality is { } municipality) output.Line($"  municipality {municipality.Name} ({municipality.Code})");
        if (document.Organisation is { } organisation) output.Line($"  organisation {organisation.Name}");

        output.Line($"  {document.Nodes.Count} nodes, {document.Rows.Count} rows, {document.Systems.Count} systems");

        var provenance = document.Provenance;
        if (provenance is not null)
        {
            output.Line($"  provenance: {Length(provenance, "sources")} sources, {Length(provenance, "plans")} plans, {Length(provenance, "missing")} missing, {provenance.Origins.Count} origins, {provenance.Conversions.Count} conversions");
        }

        var perForm = new SortedDictionary<string, int>(StringComparer.Ordinal);
        foreach (var row in document.Rows)
        {
            foreach (var form in row.Forms)
            {
                perForm[form] = perForm.GetValueOrDefault(form) + 1;
            }
        }

        foreach (var pair in perForm)
        {
            output.Line($"  rows for {pair.Key,-24} {pair.Value}", OutputColor.Gray);
        }

        return 0;
    }

    private static int Length(IpxProvenance provenance, string name) => provenance.Node[name] is JsonArray array ? array.Count : 0;
}
