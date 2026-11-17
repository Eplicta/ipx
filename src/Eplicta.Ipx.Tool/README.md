# ipx

A command-line tool for IPX documents (Information Plan Exchange). It works the same on Windows, Linux and macOS.

```
dotnet tool install --global Eplicta.Ipx.Tool
```

| Command | What it does |
|---|---|
| `ipx validate <file>...` | Checks documents against the schema and the other rules. Exit code 1 when a document has an error. |
| `ipx info <file>` | Shows the kind, name, municipality and how much a document holds. |
| `ipx compare <taxonomy> <base> <plan>...` | Compares the retention of plans against a base, node by node, through a shared taxonomy. |
| `ipx coverage <taxonomy> <plan>...` | Counts, per school form, the nodes each plan has a rule for. |
| `ipx gaps <taxonomy> <plan> <plan>...` | Lists the nodes where plans disagree or split a rule over several rows. |
| `ipx missing <file>...` | Lists what documents say is missing, with the next step. |

Run `ipx help <command>` for the arguments. Colour is used when the output is a terminal and the `NO_COLOR` variable is not set.

Licensed under the MIT licence. The specification and the library are at <https://github.com/Eplicta/ipx>.
