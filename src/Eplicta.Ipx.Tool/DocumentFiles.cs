using Eplicta.Ipx.Validation;

namespace Eplicta.Ipx.Tool;

internal static class DocumentFiles
{
    /// <summary>Reads an IPX document from a file. Throws <see cref="UsageException"/> with a message that names the file when it cannot be read as one.</summary>
    public static IpxDocument Read(string path)
    {
        string text;
        try
        {
            text = File.ReadAllText(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new UsageException($"{path}: {exception.Message}");
        }

        try
        {
            return IpxSerializer.Parse(text);
        }
        catch (IpxFormatException exception)
        {
            throw new UsageException($"{path}: {exception.Message}");
        }
    }

    /// <summary>The name the document is shown under: the name of the municipality for a plan of one, otherwise the document name.</summary>
    public static string Label(IpxDocument document)
    {
        var municipality = document.Municipality?.Name;
        if (!string.IsNullOrEmpty(municipality)) return municipality;

        return TextOf(document.Document?.Name, null) ?? document.Document?.Key ?? "?";
    }

    /// <summary>The text in the preferred language, else in English, else in the first language there is.</summary>
    public static string TextOf(IpxText text, string language)
    {
        if (text is null) return null;
        if (language is not null && text[language] is { } preferred) return preferred;
        if (text["en"] is { } english) return english;

        var first = text.Languages.FirstOrDefault();
        return first is null ? null : text[first];
    }

    public static IpxValidationResult Validate(IpxDocument document) => IpxValidator.Validate(document);
}
