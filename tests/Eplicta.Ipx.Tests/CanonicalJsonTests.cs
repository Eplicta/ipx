using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

namespace Eplicta.Ipx.Tests;

public class CanonicalJsonTests
{
    private static string Canonical(string json) => IpxCanonicalJson.Serialize(JsonNode.Parse(json));

    [Fact]
    public void Booleans_and_null_are_written_as_literals()
    {
        Assert.Equal("[true,false,null]", Canonical("[ true , false , null ]"));
    }

    [Fact]
    public void Indentation_and_property_order_do_not_change_the_result()
    {
        var compact = Canonical("{\"b\":1,\"a\":{\"d\":2,\"c\":[3,{\"z\":1,\"y\":2}]}}");
        var spread = Canonical("{\n  \"a\": { \"c\": [3, { \"y\": 2, \"z\": 1 }], \"d\": 2 },\n  \"b\": 1\n}");

        Assert.Equal("{\"a\":{\"c\":[3,{\"y\":2,\"z\":1}],\"d\":2},\"b\":1}", compact);
        Assert.Equal(compact, spread);
    }

    [Theory]
    [InlineData(0x0000000000000000UL, "0")]
    [InlineData(0x8000000000000000UL, "0")]
    [InlineData(0x0000000000000001UL, "5e-324")]
    [InlineData(0x8000000000000001UL, "-5e-324")]
    [InlineData(0x7fefffffffffffffUL, "1.7976931348623157e+308")]
    [InlineData(0xffefffffffffffffUL, "-1.7976931348623157e+308")]
    [InlineData(0x4340000000000000UL, "9007199254740992")]
    [InlineData(0xc340000000000000UL, "-9007199254740992")]
    [InlineData(0x4430000000000000UL, "295147905179352830000")]
    [InlineData(0x44b52d02c7e14af5UL, "9.999999999999997e+22")]
    [InlineData(0x44b52d02c7e14af6UL, "1e+23")]
    [InlineData(0x44b52d02c7e14af7UL, "1.0000000000000001e+23")]
    [InlineData(0x444b1ae4d6e2ef4eUL, "999999999999999700000")]
    [InlineData(0x444b1ae4d6e2ef4fUL, "999999999999999900000")]
    [InlineData(0x444b1ae4d6e2ef50UL, "1e+21")]
    [InlineData(0x3eb0c6f7a0b5ed8cUL, "9.999999999999997e-7")]
    [InlineData(0x3eb0c6f7a0b5ed8dUL, "0.000001")]
    [InlineData(0x41b3de4355555553UL, "333333333.3333332")]
    [InlineData(0x41b3de4355555555UL, "333333333.3333333")]
    [InlineData(0x41b3de4355555557UL, "333333333.33333343")]
    [InlineData(0xbecbf647612f3696UL, "-0.0000033333333333333333")]
    [InlineData(0x43143ff3c1cb0959UL, "1424953923781206.2")]
    public void Numbers_follow_the_ecmascript_form_of_rfc_8785(ulong bits, string expected)
    {
        Assert.Equal(expected, IpxCanonicalJson.FormatNumber(BitConverter.UInt64BitsToDouble(bits)));
    }

    [Fact]
    public void Numbers_in_a_document_use_the_same_form()
    {
        Assert.Equal("[1,2.5,-3,0.000001,100000000000000000000]", Canonical("[1.0, 2.50, -3, 1e-6, 1e20]"));
    }

    [Fact]
    public void Sha256_is_the_hash_of_the_canonical_text()
    {
        var node = JsonNode.Parse("{ \"b\": 1, \"a\": 2 }");

        var expected = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("{\"a\":2,\"b\":1}"))).ToLowerInvariant();

        Assert.Equal(expected, IpxCanonicalJson.Sha256(node));
    }

    [Fact]
    public void Strings_escape_only_what_json_requires()
    {
        Assert.Equal("\"a\\\"b\\\\c\\n\\u0001é\u007f\"", Canonical("\"a\\\"b\\\\c\\n\\u0001é\u007f\""));
    }

    [Fact]
    public void Strings_sort_by_utf16_code_units()
    {
        Assert.Equal("{\"B\":1,\"a\":2,\"é\":3}", Canonical("{\"é\":3,\"a\":2,\"B\":1}"));
    }
}
