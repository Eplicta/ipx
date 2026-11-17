# Legacy research viewer

`ihp.ps1` is the PowerShell console viewer the comparison rules of IPX grew out of. It reads an earlier, internal JSON format for researched retention plans, not IPX, and it expects a data folder next to it (`cities/`, `baselines/`, `structure/`) that is not part of this repository. It is kept here as a reference for how the comparison was first worked out, and it needs Windows PowerShell 5.1 or PowerShell 7.

Use the [`ipx` tool](../../src/Eplicta.Ipx.Tool/README.md) instead. It reads IPX, runs on Windows, Linux and macOS, and is tested:

| `ihp.ps1` | `ipx` |
|---|---|
| `compare -With a,b` | `ipx compare <taxonomy> <base> <a> <b>` |
| `coverage` | `ipx coverage <taxonomy> <plan>...` |
| `gaps -With a,b` | `ipx gaps <taxonomy> <a> <b>` |
| `missing` | `ipx missing <file>...` |
| `city <city>` | `ipx info <file>` |
| `validate` | `ipx validate <file>...` |

The rules are the same: per node the closest relation wins, the longest-keeping row stands for a cell, a node with no row takes the rule of its parent, and periods that count from different events are not compared. On the research corpus the coverage counts of `ipx coverage` equal those of `ihp.ps1 coverage`, and the cell values of `ipx compare` equal those of `ihp.ps1 compare`. `ipx` shows every line where a plan differs from the base, where `ihp.ps1 -OnlyDiffs` also hides nested nodes that no other plan has a rule for.
