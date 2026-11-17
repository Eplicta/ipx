# Conformance corpus

The `conformance/` folder holds documents that every implementation of IPX 1.0.x should classify the same way, whatever language it is written in. `Eplicta.Ipx` runs them in its own tests.

`conformance/manifest.json` lists every file and what an implementation must conclude:

| Outcome | What the implementation must do |
|---|---|
| `valid` | Read the document and find no errors. Warnings are allowed. |
| `refused` | Refuse to read it: it is not IPX, its major version is not supported, or its kind is unknown. |
| `invalid` | Read it and find errors, with each code listed in the manifest among them. |

The codes are those of the [validation page](validation.md). A document of the `invalid` outcome may have more errors than the listed codes.

A readable document must also survive a round trip: reading it and writing it back gives the same JSON value, including explicit nulls, properties the implementation does not know, and the order of properties.

## Adding a case

Add the file under `valid/`, `refused/` or `invalid/`, give it one defect if it is invalid, and list it in the manifest. A test checks that the manifest lists every file in the folder.
