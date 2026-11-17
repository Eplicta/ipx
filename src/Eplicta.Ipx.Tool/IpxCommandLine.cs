using Eplicta.Ipx.Tool.Commands;

namespace Eplicta.Ipx.Tool;

/// <summary>The ipx command line: picks the command from the first word and runs it.</summary>
internal static class IpxCommandLine
{
    private static readonly ICommand[] Available =
    [
        new ValidateCommand(),
        new InfoCommand(),
        new CompareCommand(),
        new CoverageCommand(),
        new GapsCommand(),
        new MissingCommand()
    ];

    public static int Run(string[] args, TextWriter output, TextWriter error, bool color)
    {
        var writer = new Output(output, error, color);

        if (args.Length == 0)
        {
            ShowHelp(writer, error);
            return 2;
        }

        var name = args[0];
        if (name is "help" or "--help" or "-h")
        {
            if (args.Length > 1 && Available.FirstOrDefault(x => x.Name == args[1]) is { } topic)
            {
                ShowCommand(writer, writer.Out, topic);
                return 0;
            }

            ShowHelp(writer, output);
            return 0;
        }

        var command = Available.FirstOrDefault(x => x.Name == name);
        if (command is null)
        {
            error.WriteLine($"Unknown command '{name}'. Run 'ipx help' for the commands.");
            return 2;
        }

        try
        {
            if (args.Skip(1).Any(x => x is "--help" or "-h"))
            {
                ShowCommand(writer, writer.Out, command);
                return 0;
            }

            var arguments = CommandArguments.Parse(args.Skip(1), command.Flags, command.Options);
            return command.Run(arguments, writer);
        }
        catch (UsageException exception)
        {
            error.WriteLine(exception.Message);
            return 2;
        }
    }

    private static void ShowHelp(Output writer, TextWriter target)
    {
        target.WriteLine("ipx: work with IPX documents (Information Plan Exchange)");
        target.WriteLine();
        target.WriteLine("Usage: ipx <command> [arguments]");
        target.WriteLine();
        foreach (var command in Available)
        {
            target.WriteLine($"  {command.Name,-10} {command.Summary}");
        }

        target.WriteLine();
        target.WriteLine("Run 'ipx help <command>' for the arguments of a command.");
        target.WriteLine("Exit codes: 0 success, 1 the check failed (for example a document is invalid), 2 wrong usage or an unreadable file.");
    }

    private static void ShowCommand(Output writer, TextWriter target, ICommand command)
    {
        target.WriteLine($"ipx {command.Name}: {command.Summary}");
        target.WriteLine();
        target.WriteLine(command.Usage);
    }
}
