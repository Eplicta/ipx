using Eplicta.Ipx.Comparison;

namespace Eplicta.Ipx.Tool.Commands;

internal sealed class CoverageCommand : ICommand
{
    private const int FormWidth = 24;
    private const int NodesWidth = 7;
    private const int ColumnWidth = 14;

    public string Name => "coverage";

    public string Summary => "Count, per school form, the leaf nodes each plan has a rule for.";

    public string Usage => "ipx coverage <taxonomy> <plan>...\n\nPrints, for each school form of the taxonomy and each plan, the number of leaf nodes with at least one rule out of all the leaf nodes of the form.";

    public IReadOnlyCollection<string> Flags => [];

    public IReadOnlyCollection<string> Options => [];

    public int Run(CommandArguments arguments, Output output)
    {
        if (arguments.Positional.Count < 2) throw new UsageException("Give a taxonomy and at least one plan. " + Usage);

        var taxonomy = DocumentFiles.Read(arguments.Positional[0]);
        var documents = arguments.Positional.Skip(1).Select(DocumentFiles.Read).ToList();
        var comparison = IpxComparer.Compare(taxonomy, documents);

        var header = Output.Fit("Form", FormWidth) + " " + Output.Fit("Nodes", NodesWidth);
        foreach (var document in documents)
        {
            header += " " + Output.Fit(DocumentFiles.Label(document), ColumnWidth);
        }

        output.Line(header, OutputColor.Gray);

        foreach (var group in comparison.Lines.GroupBy(x => CellView.FormName(x.Node)))
        {
            var row = Output.Fit(group.Key, FormWidth) + " " + Output.Fit(group.Count().ToString(), NodesWidth);
            output.Write(row);
            for (var i = 0; i < documents.Count; i++)
            {
                var have = group.Count(x => x.Cells[i].HasRule);
                output.Write(" " + Output.Fit($"{have}/{group.Count()}", ColumnWidth), have == 0 ? OutputColor.Gray : OutputColor.Default);
            }

            output.Line();
        }

        output.Line();
        output.Line("n/m = leaf nodes with a rule / leaf nodes in the taxonomy.", OutputColor.Gray);
        return 0;
    }
}
