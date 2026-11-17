# Eplicta.Ipx

Reads, writes and validates **IPX, Information Plan Exchange**: an open JSON format for information management plans (*informationshanteringsplan*). The specification and the JSON Schema are in the [Eplicta/ipx](https://github.com/Eplicta/ipx) repository.

```csharp
using Eplicta.Ipx;
using Eplicta.Ipx.Validation;

var document = IpxSerializer.Parse(File.ReadAllText("plan.ipx.json"));

var result = IpxValidator.Validate(document);
foreach (var error in result.Errors) Console.WriteLine(error);

foreach (var row in document.Rows)
{
    var retention = row.Retention[0];
    Console.WriteLine($"{row.Id}: {retention.Disposition}, {retention.Months} months");
}

File.WriteAllText("copy.ipx.json", IpxSerializer.ToJson(document));
```

* A document keeps everything in it, including properties this version does not know, explicit nulls and the order of properties, so what you write back is what you read.
* `IpxValidator` checks the JSON Schema first and the rules a schema cannot express second, and reports errors and warnings separately, each with a stable code and a JSON Pointer path.
* `IpxCanonicalJson` gives the canonical form of RFC 8785 for hashing.
* Everything the typed classes do not cover is reachable through the `Node` property of each class.

Targets .NET 8, 9, 10 and 11. Licensed under the MIT licence.
