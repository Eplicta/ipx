# Changelog

## 1.0.0 (draft, not yet released)

Changes made after an independent review of the specification, before 1.0.0:

* A document of a newer minor version validates with a warning instead of failing on a property or enumeration value it does not know.
* `ext` is allowed on every object, so a converter can keep foreign data wherever it was found.
* Enumeration values are kebab-case throughout: `record-type` and `case-type` replace `recordType` and `caseType`.
* `organisation.pupilTotal.sourceId` is `source`.
* Two triggers are added: `financial-year-end` (`year-end` is the calendar year) and `from-receipt` (`from-creation` is only creation).
* `mappings[].node` and `suggestedNode.id` are ids, not free strings.
* Dates must be real calendar dates (error `invalid-date`); a partial date stands for the whole period; `validTo` is inclusive.
* `node.source.ref` is checked; the `retain-with-period` warning covers `period`.
* The schema address is `https://eplicta.github.io/ipx/schema/1.0.0/ipx.schema.json` and the media type is `application/vnd.eplicta.ipx+json`.
* The specification defines null against absent, the defaults, the scope of id uniqueness, what several retention entries mean, hashing and encoding, and what a minor version may add.
* The conformance corpus has warnings and 10 more cases.

First version of the specification and the JSON Schema, with an example and a conformance corpus. See section 14 of [`spec/ipx-spec.md`](spec/ipx-spec.md) for what the format changes from the internal research format it grew out of.

The `Eplicta.Ipx` package for .NET 8, 9, 10 and 11:

* Reads and writes documents, keeping unknown properties, explicit nulls, property order and the text of numbers, and writing only the escapes JSON requires.
* Validates against the embedded schema (a document of a newer minor version is checked leniently and gets a warning) and against the rules a schema cannot express, with stable issue codes and JSON Pointer paths.
* Gives the canonical form of RFC 8785 for hashing.
* `IpxBuilder` builds a document from code as one chain of calls, with factories for retention entries, copies of what is added, ids checked as they are added, properties written in the order of the schema, and `BuildValidated` for output that must be valid.
* `IpxComparer` compares documents against a base node by node through a shared taxonomy, with deltas that never convert one trigger into another.
* The `Eplicta.Ipx.Tool` package, the `ipx` command: validate, info, compare, coverage, gaps and missing.
* Typed access to documents, nodes, rows, retention, mappings, references, source references municipality, organisation, responsibilities, operators, and provenance origins and conversions; everything else through `Node`.
