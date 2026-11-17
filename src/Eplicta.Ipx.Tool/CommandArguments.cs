namespace Eplicta.Ipx.Tool;

/// <summary>The words after the command name: positional values, flags and options that take a value.</summary>
internal sealed class CommandArguments
{
    private readonly Dictionary<string, string> _options = new(StringComparer.Ordinal);
    private readonly HashSet<string> _flags = new(StringComparer.Ordinal);

    private CommandArguments(List<string> positional)
    {
        Positional = positional;
    }

    public IReadOnlyList<string> Positional { get; }

    public bool Flag(string name) => _flags.Contains(name);

    public string Option(string name) => _options.GetValueOrDefault(name);

    /// <summary>Reads the words. A name in <paramref name="flags"/> stands alone, a name in <paramref name="options"/> takes the next word as its value. Any other word that starts with two dashes is an error.</summary>
    public static CommandArguments Parse(IEnumerable<string> words, IReadOnlyCollection<string> flags, IReadOnlyCollection<string> options)
    {
        var positional = new List<string>();
        var result = new CommandArguments(positional);
        var queue = new Queue<string>(words);

        while (queue.Count > 0)
        {
            var word = queue.Dequeue();
            if (!word.StartsWith("--", StringComparison.Ordinal))
            {
                positional.Add(word);
                continue;
            }

            var name = word[2..];
            if (flags.Contains(name))
            {
                result._flags.Add(name);
            }
            else if (options.Contains(name))
            {
                if (queue.Count == 0) throw new UsageException($"The option --{name} needs a value.");
                result._options[name] = queue.Dequeue();
            }
            else
            {
                throw new UsageException($"Unknown option --{name}.");
            }
        }

        return result;
    }
}
