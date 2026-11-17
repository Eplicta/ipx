# IPX: Information Plan Exchange

IPX is a JSON format for exchanging an information management plan (*informationshanteringsplan*) between systems: what an organisation keeps, where it is kept, and for how long. It can also carry the retention rules that municipalities and national bodies publish, so that plans can be compared.

IPX is an open format proposed by [Eplicta](https://github.com/Eplicta). It is not a standard of any standards body, and nobody else has adopted it yet.

**Status: version 1.0.0, draft.** The specification, the schema and the .NET library are written and tested against a corpus of 11,230 rows. Nothing has been released yet, and the format may still change before 1.0.0 is tagged.

## What is here

| Path | What |
|---|---|
| [`spec/ipx-spec.md`](spec/ipx-spec.md) | The full specification: conventions, versioning, every property, the retention model, provenance, validation, round trips. |
| [`schema/ipx-1.0.0.schema.json`](schema/ipx-1.0.0.schema.json) | A JSON Schema (draft 2020-12) for IPX 1.0.0. |
| [`examples/`](examples/) | Documents that validate against the schema. |
| [`conformance/`](conformance/) | Valid, refused and invalid documents that every implementation should classify the same way. |
| [`src/Eplicta.Ipx/`](src/Eplicta.Ipx/) | The .NET library: read, write and validate IPX. |
| [`tests/`](tests/) | The tests of the library, including the conformance corpus. |
| [`docs/`](docs/) | Guides for the library, validation, the conformance corpus and releasing. |
| [`CHANGELOG.md`](CHANGELOG.md) | What changed between versions. |

## The .NET library

`Eplicta.Ipx` targets .NET 8, 9, 10 and 11. Once a version is published it is installed with:

```
dotnet add package Eplicta.Ipx
```

```csharp
var document = IpxSerializer.Parse(File.ReadAllText("plan.ipx.json"));
var result = IpxValidator.Validate(document);
```

See [Getting started](docs/getting-started.md). The library keeps everything in a document it does not understand, so what you write back is what you read.

## A first look

A plan is a tree of nodes and a list of rows. Each row says what is kept and what happens to it:

```json
{
  "id": "r2",
  "rowKind": "caseType",
  "forms": ["compulsory-school"],
  "name": { "sv": "Ansökan om skolplats", "en": "Application for a school place" },
  "node": "pupils.admission",
  "registration": "registered",
  "personalData": true,
  "retention": [
    { "disposition": "dispose", "period": { "amount": 6, "unit": "year" }, "months": 72, "trigger": "case-closed", "outcome": "grant" },
    { "disposition": "dispose", "period": { "amount": 2, "unit": "year" }, "months": 24, "trigger": "case-closed", "outcome": "refusal" }
  ]
}
```

The complete document is in [`examples/school-plan.ipx.json`](examples/school-plan.ipx.json).

## Validating a document

The schema works with any validator for JSON Schema draft 2020-12. With Python:

```
python -m pip install jsonschema
```

```python
import json
from jsonschema import Draft202012Validator

schema = json.load(open("schema/ipx-1.0.0.schema.json", encoding="utf-8"))
document = json.load(open("examples/school-plan.ipx.json", encoding="utf-8"))

errors = list(Draft202012Validator(schema).iter_errors(document))
print("valid" if not errors else errors[0].message)
```

A schema cannot check everything. Section 10 of the specification lists the rules that need code, such as unique ids and references that must resolve.

## Design in short

* One document kind for four purposes: `plan`, `reference` (the rules one authority has published), `recommendation` and `taxonomy`.
* Retention keeps what the source says: the period as written, the trigger, the qualifier, the condition and the source's own words. Different triggers are never converted into each other.
* Everything that came from somewhere is traceable: sources, the plans rows are attributed to, what is missing, and what was converted.
* Extra data goes under `ext`. A reader that saves a document keeps everything it does not understand.
* A document converted to another format and back should come back unchanged. Section 11 of the specification says how.

## Related standards

IPX does not replace Riksarkivet's schemas, Noark 5, MoReq2010 or RiC-O. They exchange archives and classification structures; none covers a plan with verbatim wording, retention triggers, sources and a comparison tree. Section 12 of the specification relates them to IPX.

## Licence

* The specification, the schema and the examples (`spec/`, `schema/`, `examples/`) are licensed under [Creative Commons Attribution 4.0 International](spec/LICENSE).
* The code (`src/`, `tests/` and the workflows) is licensed under the [MIT licence](LICENSE).

## Feedback

Open an issue for a question, an error in the specification or a case the format cannot express. A change that breaks existing documents needs a new major version; see section 4 of the specification.
