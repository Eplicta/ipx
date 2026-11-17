using Eplicta.Ipx.Comparison;

namespace Eplicta.Ipx.Tool.Commands;

internal sealed class CompareCommand : ICommand
{
    private const int DefaultTitleWidth = 44;
    private const int DefaultColumnWidth = 17;

    public string Name => "compare";

    public string Summary => "Compare the retention of plans against a base, node by node.";

    public string Usage => """
        ipx compare <taxonomy> <base> [<other>...] [--form <form>] [--only-differences] [--language <tag>] [--title-width <n>] [--column-width <n>]

        The taxonomy is a document whose nodes the rows of the plans map to. The base is the plan the others are measured against; the others follow in the order given.
        Each line is one leaf node. A cell shows what the plan keeps and for how long, such as 6y/case-closed (six years from the closing of the case), retain, or 2y…6y when several rows differ.
        The colour of the other plans says how they compare with the base: green same, yellow shorter, magenta longer, orange not comparable (different triggers), red unclear or different, grey no rule.
        Periods that count from different events are never converted into each other.
        --form limits the lines to one school form, by its wire name such as compulsory-school.
        """;

    public IReadOnlyCollection<string> Flags => ["only-differences", "no-legend"];

    public IReadOnlyCollection<string> Options => ["form", "language", "title-width", "column-width"];

    public int Run(CommandArguments arguments, Output output)
    {
        if (arguments.Positional.Count < 2) throw new UsageException("Give a taxonomy and a base, and any other plans. " + Usage);

        var language = arguments.Option("language");
        var titleWidth = Width(arguments.Option("title-width"), DefaultTitleWidth, "--title-width");
        var columnWidth = Width(arguments.Option("column-width"), DefaultColumnWidth, "--column-width");

        var taxonomy = DocumentFiles.Read(arguments.Positional[0]);
        var documents = arguments.Positional.Skip(1).Select(DocumentFiles.Read).ToList();

        var options = new IpxCompareOptions { OnlyDifferences = arguments.Flag("only-differences") };
        if (arguments.Option("form") is { } formText)
        {
            if (!IpxWire.TryParse<IpxForm>(formText, out var form)) throw new UsageException($"'{formText}' is not a school form.");
            options.Form = form;
        }

        var comparison = IpxComparer.Compare(taxonomy, documents, options);

        if (!arguments.Flag("no-legend"))
        {
            output.Line(CellView.Legend, OutputColor.Gray);
            output.Line("Triggers are not converted into each other; periods that count from different events are not comparable.", OutputColor.Gray);
            output.Line();
        }

        var header = Output.Fit("Node", titleWidth);
        for (var i = 0; i < documents.Count; i++)
        {
            var label = DocumentFiles.Label(documents[i]);
            header += " " + Output.Fit(i == 0 ? "BASE " + label : label, columnWidth);
        }

        foreach (var group in comparison.Lines.GroupBy(x => CellView.FormName(x.Node)))
        {
            output.Line($"{group.Key}  ({group.Count()} nodes)", OutputColor.Cyan);
            output.Line(header, OutputColor.Gray);

            foreach (var line in group)
            {
                var title = new string(' ', Math.Max(0, line.Depth - 1) * 2) + (DocumentFiles.TextOf(line.Node.Title, language) ?? line.Node.Id);
                output.Write(Output.Fit(title, titleWidth));
                for (var i = 0; i < line.Cells.Count; i++)
                {
                    output.Write(" " + Output.Fit(CellView.Text(line.Cells[i]), columnWidth), CellView.Color(line.Cells[i], i == 0));
                }

                output.Line();
            }

            output.Line();
        }

        return 0;
    }

    private static int Width(string value, int fallback, string name)
    {
        if (value is null) return fallback;
        if (!int.TryParse(value, out var width) || width < 4) throw new UsageException($"{name} must be a whole number of at least 4.");

        return width;
    }
}
