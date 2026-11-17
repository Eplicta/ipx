using System.Text.Json.Nodes;
using Eplicta.Ipx.Validation;

namespace Eplicta.Ipx.Tests;

public class ConformanceTests
{
    public static IEnumerable<object[]> Cases()
    {
        var manifest = (JsonObject)JsonNode.Parse(TestFiles.Read("conformance/manifest.json"));
        foreach (var item in (JsonArray)manifest["cases"])
        {
            var codes = item["codes"] is JsonArray array ? array.Select(c => (string)c).ToArray() : [];
            var warnings = item["warnings"] is JsonArray listed ? listed.Select(c => (string)c).ToArray() : [];
            yield return [(string)item["file"], (string)item["outcome"], string.Join(",", codes), string.Join(",", warnings)];
        }
    }

    public static IEnumerable<object[]> ReadableFiles() => Cases().Where(x => (string)x[1] != "refused").Select(x => new[] { x[0] });

    [Fact]
    public void The_manifest_lists_every_file_in_the_corpus()
    {
        var manifest = (JsonObject)JsonNode.Parse(TestFiles.Read("conformance/manifest.json"));
        var listed = ((JsonArray)manifest["cases"]).Select(c => ((string)c["file"]).Replace('/', Path.DirectorySeparatorChar)).ToHashSet();
        var root = Path.Combine(AppContext.BaseDirectory, "conformance");

        var onDisk = Directory.GetFiles(root, "*.json", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(root, f))
            .Where(f => f != "manifest.json")
            .ToHashSet();

        Assert.Equal(onDisk.OrderBy(x => x), listed.OrderBy(x => x));
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Every_case_is_classified_as_the_manifest_says(string file, string outcome, string expectedCodes, string expectedWarnings)
    {
        var text = TestFiles.Read("conformance/" + file);

        if (outcome == "refused")
        {
            Assert.Throws<IpxFormatException>(() => IpxSerializer.Parse(text));
            return;
        }

        var result = IpxValidator.Validate(IpxSerializer.Parse(text));
        if (outcome == "valid")
        {
            Assert.True(result.IsValid, file + ": " + string.Join("; ", result.Errors.Select(e => $"{e.Path}: {e.Message}")));
            foreach (var code in expectedWarnings.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                Assert.Contains(result.Warnings, w => w.Code == code);
            }

            return;
        }

        Assert.Equal("invalid", outcome);
        Assert.False(result.IsValid, file);
        foreach (var code in expectedCodes.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            Assert.Contains(result.Errors, e => e.Code == code);
        }
    }

    [Theory]
    [MemberData(nameof(ReadableFiles))]
    public void Every_readable_case_survives_a_round_trip(string file)
    {
        var text = TestFiles.Read("conformance/" + file);

        var written = IpxSerializer.ToJson(IpxSerializer.Parse(text));

        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(text), JsonNode.Parse(written)), file);
    }
}
