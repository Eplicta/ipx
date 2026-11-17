using System.Text.Json.Nodes;

namespace Eplicta.Ipx.Tool.Commands;

internal sealed class MissingCommand : ICommand
{
    private const int KindWidth = 26;
    private const int NextWidth = 18;
    private const int DetailWidth = 90;

    public string Name => "missing";

    public string Summary => "List what the documents say is missing, with the next step.";

    public string Usage => "ipx missing <file>... [--next-step <step>]\n\nReads provenance.missing of each file: a plan that is not public, a school form not covered, and so on. --next-step keeps only the entries with that next step, such as contact-city.";

    public IReadOnlyCollection<string> Flags => [];

    public IReadOnlyCollection<string> Options => ["next-step"];

    public int Run(CommandArguments arguments, Output output)
    {
        if (arguments.Positional.Count == 0) throw new UsageException("Give at least one file. " + Usage);

        var filter = arguments.Option("next-step");
        var perStep = new SortedDictionary<string, int>(StringComparer.Ordinal);

        foreach (var path in arguments.Positional)
        {
            var document = DocumentFiles.Read(path);
            if (document.Provenance?.Node["missing"] is not JsonArray missing) continue;

            output.Line(DocumentFiles.Label(document), OutputColor.Cyan);
            foreach (var entry in missing.OfType<JsonObject>())
            {
                var next = Text(entry, "nextStep") ?? "?";
                if (filter is not null && next != filter) continue;

                perStep[next] = perStep.GetValueOrDefault(next) + 1;
                output.Write("  " + Output.Fit(Text(entry, "kind") ?? "?", KindWidth) + " " + Output.Fit(next, NextWidth) + " ", OutputColor.Yellow);
                output.Line(Output.Fit(Text(entry, "detail") ?? string.Empty, DetailWidth).TrimEnd());
            }
        }

        output.Line();
        output.Line("Totals by next step:", OutputColor.Cyan);
        foreach (var pair in perStep)
        {
            output.Line($"  {pair.Key,-18} {pair.Value}");
        }

        return 0;
    }

    private static string Text(JsonObject owner, string name) => owner[name] is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;
}
