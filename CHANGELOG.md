# Changelog

## 1.0.0 (draft, not yet released)

First version of the specification and the JSON Schema, with an example and a conformance corpus. See section 14 of [`spec/ipx-spec.md`](spec/ipx-spec.md) for what the format changes from the internal research format it grew out of.

The `Eplicta.Ipx` package for .NET 8, 9, 10 and 11:

* Reads and writes documents, keeping unknown properties, explicit nulls, property order and the text of numbers, and writing only the escapes JSON requires.
* Validates against the embedded schema and against the rules a schema cannot express, with stable issue codes and JSON Pointer paths.
* Gives the canonical form of RFC 8785 for hashing.
* Typed access to documents, nodes, rows, retention, mappings, references, source references and provenance origins and conversions; everything else through `Node`.
