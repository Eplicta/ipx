namespace Eplicta.Ipx.Tool.Commands;

internal sealed class ValidateCommand : ICommand
{
    public string Name => "validate";

    public string Summary => "Check documents against the schema and the other rules.";

    public string Usage => "ipx validate <file>...\n\nPrints the errors and warnings of each file. The exit code is 1 when any file has an error. Warnings do not change it.";

    public IReadOnlyCollection<string> Flags => [];

    public IReadOnlyCollection<string> Options => [];

    public int Run(CommandArguments arguments, Output output)
    {
        if (arguments.Positional.Count == 0) throw new UsageException("Give at least one file. " + Usage);

        var failed = false;
        foreach (var path in arguments.Positional)
        {
            var document = DocumentFiles.Read(path);
            var result = DocumentFiles.Validate(document);

            if (result.IsValid)
            {
                output.Line(result.Warnings.Count == 0 ? $"{path}: valid" : $"{path}: valid, {Count(result.Warnings.Count, "warning")}", result.Warnings.Count == 0 ? OutputColor.Green : OutputColor.Yellow);
            }
            else
            {
                failed = true;
                output.Line($"{path}: {Count(result.Errors.Count, "error")}, {Count(result.Warnings.Count, "warning")}", OutputColor.Red);
            }

            foreach (var error in result.Errors)
            {
                output.Line($"  error {error}", OutputColor.Red);
            }

            foreach (var warning in result.Warnings)
            {
                output.Line($"  warning {warning}", OutputColor.Yellow);
            }
        }

        return failed ? 1 : 0;
    }

    private static string Count(int count, string noun) => count == 1 ? $"1 {noun}" : $"{count} {noun}s";
}
