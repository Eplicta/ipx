using Eplicta.Ipx.Comparison;

namespace Eplicta.Ipx.Tool.Commands;

internal sealed class GapsCommand : ICommand
{
    private const int NodeWidth = 62;

    public string Name => "gaps";

    public string Summary => "List the nodes where plans disagree, or split a rule over several rows.";

    public string Usage => "ipx gaps <taxonomy> <plan> <plan>... [--form <form>]\n\nA node is listed when at least two plans have a rule at it and they differ, or one of them has several rows for it.";

    public IReadOnlyCollection<string> Flags => [];

    public IReadOnlyCollection<string> Options => ["form"];

    public int Run(CommandArguments arguments, Output output)
    {
        if (arguments.Positional.Count < 3) throw new UsageException("Give a taxonomy and at least two plans. " + Usage);

        var taxonomy = DocumentFiles.Read(arguments.Positional[0]);
        var documents = arguments.Positional.Skip(1).Select(DocumentFiles.Read).ToList();

        var options = new IpxCompareOptions();
        if (arguments.Option("form") is { } formText)
        {
            if (!IpxWire.TryParse<IpxForm>(formText, out var form)) throw new UsageException($"'{formText}' is not a school form.");
            options.Form = form;
        }

        var comparison = IpxComparer.Compare(taxonomy, documents, options);
        var listed = 0;

        foreach (var line in comparison.Lines)
        {
            var have = line.Cells.Where(x => x.HasRule).ToList();
            if (have.Count < 2) continue;
            if (have.Select(x => x.Text).Distinct().Count() < 2 && !have.Any(x => x.Multiple)) continue;

            listed++;
            output.Write(Output.Fit(line.Node.Id, NodeWidth));
            output.Line(string.Join("  ", line.Cells.Select((x, i) => $"{DocumentFiles.Label(documents[i])}={CellView.Text(x)}")), OutputColor.Yellow);
        }

        output.Line();
        output.Line($"{listed} nodes listed.", OutputColor.Gray);
        return 0;
    }
}
