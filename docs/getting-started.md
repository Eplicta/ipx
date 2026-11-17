# Getting started

`Eplicta.Ipx` reads, writes and validates IPX documents. It targets .NET 8, 9, 10 and 11 and depends only on `JsonSchema.Net`.

```
dotnet add package Eplicta.Ipx
```

## Read and validate a document

```csharp
using Eplicta.Ipx;
using Eplicta.Ipx.Validation;

var document = IpxSerializer.Parse(File.ReadAllText("plan.ipx.json"));

var result = IpxValidator.Validate(document);
foreach (var error in result.Errors) Console.WriteLine(error);
foreach (var warning in result.Warnings) Console.WriteLine(warning);
```

`Parse` throws an `IpxFormatException` when the text cannot be an IPX document this library supports: it is not JSON, it is not an object, the `format` marker is not `ipx`, the major version is not 1, or the `kind` is unknown. It does not check the document against the schema; `IpxValidator` does that.

## Read the rows

```csharp
foreach (var row in document.Rows.Where(r => r.RowKind == IpxRowKind.CaseType))
{
    foreach (var entry in row.Retention)
    {
        var months = entry.Months is int value ? $"{value} months" : "no period";
        Console.WriteLine($"{row.Id} {entry.Outcome}: {entry.Disposition}, {months}");
    }
}
```

For the example document this prints:

```
r2 Grant: Dispose, 72 months
r2 Refusal: Dispose, 24 months
```

## Build a document from code

```csharp
var plan = IpxDocument.Create(IpxKind.Plan, Guid.NewGuid().ToString(), IpxText.Create(("sv", "Exempelskolans plan"), ("en", "Example School plan")), "sv");

var governance = new IpxNode { Id = "governance", Code = "1" };
governance.Title = IpxText.Create(("sv", "Styra verksamheten"), ("en", "Govern the operation"));
plan.Nodes.Add(governance);

var minutes = new IpxRow { Id = "board-minutes", RowKind = IpxRowKind.RecordType, NodeId = "governance", Registration = IpxRegistration.Registered };
minutes.Name = IpxText.Create(("sv", "Styrelseprotokoll"), ("en", "Board minutes"));
minutes.Forms.Add("shared");
minutes.Retention.Add(new IpxRetention { Disposition = IpxDisposition.Retain, Trigger = IpxTrigger.None });
plan.Rows.Add(minutes);

var result = IpxValidator.Validate(plan);
```

## Write it

```csharp
File.WriteAllText("copy.ipx.json", IpxSerializer.ToJson(plan));
```

`ToJson` returns the document indented with two spaces and with line feeds, and a line feed at the end. `File.WriteAllText` writes it as UTF-8 without a byte-order mark; to write to a stream use `IpxSerializer.Write(document, stream)`, which does the same.

Only what JSON requires is escaped. Every other character, including letters outside ASCII and space characters such as the narrow no-break space, is written as it was read, so reading a document and writing it back changes nothing.

## Next

* [Working with the model](working-with-the-model.md): what the classes do and do not cover.
* [Validation](validation.md): what the codes mean.
