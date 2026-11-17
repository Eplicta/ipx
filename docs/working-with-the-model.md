# Working with the model

The classes in `Eplicta.Ipx` are views over the JSON, not copies of it. `IpxRow`, `IpxRetention` and the others read and write the underlying `JsonObject`. That is how the library keeps its promise from section 4 of the specification: a reader that saves a document keeps everything it does not understand.

## Everything is reachable

Each class has a `Node` property, the `JsonObject` it is over. The typed properties cover the parts of the format that applications use most: the document, systems, nodes, rows, retention, mappings, references, source references, `asWritten`, and the origins and conversions of the provenance. Everything else, such as the sources, the plans and what is missing in the provenance, or the `organisation` of a plan, is reached through `Node` and is kept as it is:

```csharp
var sources = document.Provenance.Node["sources"];
```

## Unknown content

A property this version does not know, for example one added by a later minor version, is not lost when you change something else and write the document:

```csharp
document.Rows[0].CurrentNote = "Edited by the application";
var written = IpxSerializer.ToJson(document);
```

## Null and absent

In IPX, `null` means *not known* and an absent property means *not stated*. The typed properties read both as `null`, because for most code that is what matters. To tell them apart:

```csharp
retention.Has("months");     // true when present, even as null
retention.IsNull("months");  // true only for an explicit JSON null
retention.SetNull("months"); // writes a JSON null
retention.Months = null;     // removes the property
```

## Enumerations

Every enumeration of the format is a C# enum with its wire name, converted by `IpxWire`:

```csharp
IpxWire.ToWire(IpxTrigger.SchoolYearEnd);                    // "school-year-end"
IpxWire.TryParse<IpxTrigger>("school-year-end", out var t);  // true
```

A value the library does not know, which a newer minor version may add, reads as the neutral value and the original stays in the document: `Unclear` for a disposition and `Other` for a trigger. For enumerations without a neutral value, such as `IpxRelation`, it reads as `null`.

## Lists

`Rows`, `Nodes`, `Systems`, `Retention`, `Mappings` and `References` are live views of the JSON arrays. Reading one creates nothing; the array appears in the document when you add the first item. `Forms` and `Losses` are live lists of strings.

An item can belong to one list at a time, because a JSON node has one parent. To move an item, remove it from its list first.

## Text in several languages

`IpxText` holds one string per language tag:

```csharp
var name = IpxText.Create(("sv", "Plan"), ("en", "Plan in English"));
name["en"] = "Plan, edited";
```

## Extension data

Data the specification does not define goes under `ext`, keyed by a reverse-domain name:

```csharp
row.EnsureExt().Set("org.example.vendor", new JsonObject { ["code"] = 7 });
```

`Ext` returns `null` when a row has none, and `EnsureExt()` creates it. A reader that saves a document keeps `ext` content it does not know.

## Canonical form

`IpxCanonicalJson.Serialize(node)` gives the canonical text of RFC 8785, and `IpxCanonicalJson.Sha256(node)` its hash. Two values that are the same JSON value have the same canonical text, whatever their spacing or the order of their properties, so the hash tells whether content has changed.
