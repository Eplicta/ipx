using System.Text;
using System.Text.Json.Nodes;

namespace Eplicta.Ipx.Tests;

public class SerializerTests
{
    private const string Header = "\"format\":\"ipx\",\"formatVersion\":\"1.0.0\",\"kind\":\"plan\",";
    private const string DocumentPart = "\"document\":{\"id\":\"11111111-1111-4111-8111-111111111111\",\"name\":{\"en\":\"T\"},\"language\":\"sv\"}";

    private static string Json(string header, string rest = "") => "{" + header + DocumentPart + rest + "}";

    [Fact]
    public void Example_survives_a_round_trip_unchanged()
    {
        var original = TestFiles.Example();

        var written = IpxSerializer.ToJson(IpxSerializer.Parse(original));

        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(original), JsonNode.Parse(written)));
    }

    [Fact]
    public void Parse_accepts_a_newer_minor_version()
    {
        var document = IpxSerializer.Parse(Json("\"format\":\"ipx\",\"formatVersion\":\"1.7.0\",\"kind\":\"plan\","));

        Assert.Equal("1.7.0", document.FormatVersion);
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("\"text\"")]
    [InlineData("{")]
    [InlineData("")]
    public void Parse_refuses_what_is_not_a_json_object(string text)
    {
        Assert.Throws<IpxFormatException>(() => IpxSerializer.Parse(text));
    }

    [Theory]
    [InlineData("\"format\":\"json\",\"formatVersion\":\"1.0.0\",\"kind\":\"plan\",")]
    [InlineData("\"formatVersion\":\"1.0.0\",\"kind\":\"plan\",")]
    [InlineData("\"format\":\"ipx\",\"formatVersion\":\"2.0.0\",\"kind\":\"plan\",")]
    [InlineData("\"format\":\"ipx\",\"formatVersion\":\"0.9.0\",\"kind\":\"plan\",")]
    [InlineData("\"format\":\"ipx\",\"formatVersion\":\"1.x\",\"kind\":\"plan\",")]
    [InlineData("\"format\":\"ipx\",\"kind\":\"plan\",")]
    [InlineData("\"format\":\"ipx\",\"formatVersion\":\"1.0.0\",")]
    [InlineData("\"format\":\"ipx\",\"formatVersion\":\"1.0.0\",\"kind\":\"calendar\",")]
    public void Parse_refuses_a_document_it_cannot_support(string header)
    {
        Assert.Throws<IpxFormatException>(() => IpxSerializer.Parse(Json(header)));
    }

    [Fact]
    public void Parse_refuses_a_repeated_property()
    {
        var text = "{" + Header + "\"kind\":\"taxonomy\"," + DocumentPart + "}";

        Assert.Throws<IpxFormatException>(() => IpxSerializer.Parse(text));
    }

    [Fact]
    public void Parse_reads_the_example()
    {
        var document = TestFiles.ExampleDocument();

        Assert.Equal(IpxKind.Plan, document.Kind);
        Assert.Equal("1.0.0", document.FormatVersion);
        Assert.Equal("0b6c8c58-4f7e-4f43-9a1b-2f1d7a9c3e10", document.Document.Id);
        Assert.Equal(3, document.Rows.Count);
        Assert.Equal(2, document.Rows[1].Retention.Count);
    }

    [Fact]
    public void Read_reads_from_a_stream()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(TestFiles.Example()));

        var document = IpxSerializer.Read(stream);

        Assert.Equal(IpxKind.Plan, document.Kind);
    }

    [Fact]
    public void RoundTrip_keeps_explicit_nulls_unknown_properties_and_order()
    {
        var text = Json("\"format\":\"ipx\",\"formatVersion\":\"1.1.0\",\"kind\":\"plan\",", ",\"futureThing\":{\"x\":[1,2,{\"y\":null}]}")
            .Replace("\"language\":\"sv\"", "\"language\":\"sv\",\"validFrom\":null,\"newInOneOne\":true");

        var written = IpxSerializer.ToJson(IpxSerializer.Parse(text));

        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(text), JsonNode.Parse(written)));
        Assert.Contains("\"validFrom\": null", written);
        Assert.True(written.IndexOf("\"language\"", StringComparison.Ordinal) < written.IndexOf("\"validFrom\"", StringComparison.Ordinal));
        Assert.True(written.IndexOf("\"format\"", StringComparison.Ordinal) < written.IndexOf("\"kind\"", StringComparison.Ordinal));
    }

    [Fact]
    public void RoundTrip_keeps_the_text_of_a_number()
    {
        var text = Json(Header, ",\"rows\":[{\"id\":\"r\",\"forms\":[],\"retention\":[{\"disposition\":\"dispose\",\"period\":{\"amount\":6.0,\"unit\":\"year\"}}]}]");

        var written = IpxSerializer.ToJson(IpxSerializer.Parse(text));

        Assert.Contains("\"amount\": 6.0", written);
    }

    [Fact]
    public void ToJson_escapes_only_what_json_requires()
    {
        var text = Json(Header).Replace("{\"en\":\"T\"}", "{\"en\":\"a\\\"b\\\\c\\u0001\\n\"}");

        var written = IpxSerializer.ToJson(IpxSerializer.Parse(text));

        Assert.Contains("\"a\\\"b\\\\c\\u0001\\n\"", written);
    }

    [Fact]
    public void ToJson_writes_space_and_format_characters_as_they_were_read()
    {
        var narrow = char.ConvertFromUtf32(0x202F);
        var nonBreaking = char.ConvertFromUtf32(0x00A0);
        var separator = char.ConvertFromUtf32(0x2028);
        var text = Json(Header).Replace("{\"en\":\"T\"}", "{\"en\":\"a" + narrow + "b" + nonBreaking + "c" + separator + "d\"}");

        var written = IpxSerializer.ToJson(IpxSerializer.Parse(text));

        Assert.Contains("a" + narrow + "b" + nonBreaking + "c" + separator + "d", written);
        Assert.DoesNotContain("\\u202", written);
    }

    [Fact]
    public void ToJson_does_not_escape_swedish_letters()
    {
        var text = Json(Header).Replace("{\"en\":\"T\"}", "{\"sv\":\"Ärende på skolan\"}");

        var written = IpxSerializer.ToJson(IpxSerializer.Parse(text));

        Assert.Contains("Ärende på skolan", written);
    }

    [Fact]
    public void ToJson_is_indented_with_two_spaces_and_line_feeds()
    {
        var written = IpxSerializer.ToJson(IpxSerializer.Parse(Json(Header)));

        Assert.StartsWith("{\n  \"format\": \"ipx\",", written);
        Assert.DoesNotContain("\r", written);
        Assert.EndsWith("}\n", written);
    }

    [Fact]
    public void Write_has_no_byte_order_mark()
    {
        using var stream = new MemoryStream();

        IpxSerializer.Write(IpxSerializer.Parse(Json(Header)), stream);

        Assert.Equal((byte)'{', stream.ToArray()[0]);
    }
}
