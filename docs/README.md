# Documentation

| Page | For |
|---|---|
| [Specification](../spec/ipx-spec.md) | The format itself: every property, the retention model, provenance, validation and round trips. Start here to understand IPX or to write your own implementation. |
| [Getting started](getting-started.md) | Install `Eplicta.Ipx`, read a document, validate it, build one from code, write it. |
| [Building documents](building-documents.md) | `IpxBuilder`: produce a document from code, with factories for retention and a validated build. |
| [Working with the model](working-with-the-model.md) | How the typed classes relate to the JSON: unknown content, null and absent, enumerations, lists, extension data. |
| [Comparing plans](comparing.md) | `IpxComparer` and the `ipx` tool: compare plans against a base through a shared taxonomy, and the rules for doing so. |
| [Validation](validation.md) | The two validation steps, the issue codes, and how to read a result. |
| [Conformance corpus](conformance.md) | A set of valid, refused and invalid documents that every implementation should classify the same way. |
| [Releasing](releasing.md) | How a version of the package is published. |

The JSON Schema is [`schema/ipx-1.0.0.schema.json`](../schema/ipx-1.0.0.schema.json). A complete example is [`examples/school-plan.ipx.json`](../examples/school-plan.ipx.json).
