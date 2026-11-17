namespace Eplicta.Ipx.Tool;

internal interface ICommand
{
    string Name { get; }

    string Summary { get; }

    string Usage { get; }

    /// <summary>The options that stand alone, without the two dashes.</summary>
    IReadOnlyCollection<string> Flags { get; }

    /// <summary>The options that take a value, without the two dashes.</summary>
    IReadOnlyCollection<string> Options { get; }

    /// <summary>Runs the command and returns the exit code: 0 for success, 1 when the check the command makes fails, 2 for a usage or file problem.</summary>
    int Run(CommandArguments arguments, Output output);
}
