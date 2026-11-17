# Validation

`IpxValidator.Validate` follows the order in section 10 of the specification.

1. **Schema.** The embedded JSON Schema checks types, required properties, enumeration values, patterns, and that no unknown property sits outside `ext`.
2. **The rules a schema cannot check.** Unique ids, references that must resolve, parent chains that must not loop, and a taxonomy that must not have rows.

The second step runs only on a document that passed the first, so a result never mixes the two kinds of error.

```csharp
var result = IpxValidator.Validate(document);

if (!result.IsValid)
{
    foreach (var error in result.Errors) Console.WriteLine($"{error.Code} {error.Path}: {error.Message}");
}
```

Errors make a document invalid. Warnings never do. `Path` is a JSON Pointer such as `/rows/3/retention/0/months`.

## Codes

The codes are constants of `IpxIssueCodes` and do not change within a major version.

| Code | Kind | Meaning |
|---|---|---|
| `schema` | error | A structural error found by the JSON Schema. |
| `duplicate-id` | error | Two items in one list have the same id. |
| `missing-reference` | error | An id refers to something that does not exist: a parent, a system, a node, a plan in the provenance, a source. |
| `parent-loop` | error | The parent chain of a node loops. |
| `taxonomy-has-rows` | error | A taxonomy document has rows. |
| `months-mismatch` | warning | The months of a retention entry are not its period converted to months, counting a day as one thirtieth of a month and rounding up. |
| `retain-with-period` | warning | A `retain` entry has a period. |
| `mapping-not-in-taxonomy` | warning | A mapping points at a node that is not in the taxonomy you gave. |
| `delegates-to-unknown-plan` | warning | A plan hands topics to a plan that is not listed. |
| `newer-minor-version` | warning | The document is of a newer minor version than this validator knows, so properties it added are not checked. |

## Checking mappings against a taxonomy

Rows of a `reference` or a `plan` point into a taxonomy with `mappings`. The taxonomy is a separate document, so the validator can only check the mappings if you tell it which nodes exist:

```csharp
var taxonomy = IpxSerializer.Parse(File.ReadAllText("taxonomy.ipx.json"));
var options = new IpxValidationOptions { TaxonomyNodeIds = taxonomy.Nodes.Select(n => n.Id).ToList() };

var result = IpxValidator.Validate(document, options);
```

## A newer minor version

A document of version 1.3.0 may have properties that 1.0.0 does not define. The embedded schema is the 1.0.0 schema and forbids unknown properties on purpose, so that a misspelt property is found. Validating a newer document against it can therefore report errors for properties that are valid in the document's own version. Validate against the schema of the version the document declares.
