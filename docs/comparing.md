# Comparing plans

IPX is meant to make plans comparable: the same question asked of many plans gives answers that can be set side by side. The library compares plans with `IpxComparer`, and the `ipx` tool does the same from the command line.

## How plans become comparable

Each plan has its own tree and its own rows, so plans cannot be compared row against row. They are compared through a shared **taxonomy**: a document of kind `taxonomy` whose leaf nodes are the things worth comparing, such as "Hold development talks". A row says which taxonomy nodes it corresponds to in `mappings`, and how closely: `exact`, `covers`, `part` or `similar` (section 7.2 of the specification). The comparer reads plans only through those mappings.

## From code

```csharp
using Eplicta.Ipx.Comparison;

var taxonomy = IpxSerializer.Parse(File.ReadAllText("taxonomy.ipx.json"));
var baseline = IpxSerializer.Parse(File.ReadAllText("fortdocs.ipx.json"));
var other = IpxSerializer.Parse(File.ReadAllText("uppsala.ipx.json"));

var comparison = IpxComparer.Compare(taxonomy, [baseline, other], new IpxCompareOptions { Form = IpxForm.CompulsorySchool });

foreach (var line in comparison.Lines)
{
    var cell = line.Cells[1];
    Console.WriteLine($"{line.Node.Id}: base {line.Cells[0].Text}, other {cell.Text} ({cell.Delta})");
}
```

The first document is the **base**: every other document is measured against it. The result has one line for each leaf node of the taxonomy and one cell on each line for each document.

| Property of a cell | Meaning |
|---|---|
| `Text` | A short, language-neutral text: `retain`, `6y/case-closed` (six years from the closing of the case), `30m`, `obsolete`, `handover`, `?` for unclear, `-` for no rule, or a range such as `2y…6y` when several rows differ. |
| `Delta` | How the cell compares with the base: `Same`, `Shorter`, `Longer`, `NotComparable`, `Unclear`, `Differs`, `NoRule` or `NoBase`. Null for the base itself. |
| `Rows`, `Relation` | The rows the cell is made of, and how closely they correspond to the node. |
| `Retention` | The retention entry that stands for the cell. |
| `Multiple`, `Inherited`, `NotAStandingRule` | Whether several rows make up the cell, whether it is the rule of the parent node, and whether the best row is a one-off decision, a GDPR limit or a platform statement. |

`IpxCompareOptions.Form` limits the lines to one school form, and `OnlyDifferences` keeps only the lines where a document differs from the base and at least one has a rule.

## The rules

1. **Which rows count.** Rows that are current and that state a retention. Rows of the types `instruction`, `promise`, `placeholder`, `general-principle` and `recommendation-quote` do not count.
2. **The closest relation wins.** When a node has rows at several relations, only the rows at the closest one are used: `exact` before `covers` before `part` before `similar`.
3. **The longest-keeping row stands for the cell.** A retain keeps longest, then the longest period. A row with several retention entries is represented by its longest-keeping entry. When several rows differ, the text shows the range.
4. **A node without a row takes the rule of its parent**, when the parent is not a root of the tree. The cell is then marked `Inherited`.
5. **Triggers are never converted into each other.** Six years from the end of a case is not comparable with six years from the end of the calendar year, so two periods are compared only when they count from the same event, or when one of them does not say what it counts from. Otherwise the delta is `NotComparable`.

How a document compares with the base:

| Delta | When |
|---|---|
| `NoRule` | The document has no rule at the node. |
| `NoBase` | The document has a rule and the base has none. |
| `Unclear` | Either side is unclear or only a review. |
| `Same` / `Differs` | Both sides hand over (`Same`), or one hands over and the other does not (`Differs`). |
| `Same` | Both retain, or both dispose after the same period from the same trigger. |
| `Shorter` / `Longer` | The base retains and the document disposes (`Shorter`), the other way round (`Longer`), or both dispose and the document's period is shorter or longer. |
| `NotComparable` | Both dispose, but the periods count from different events, or only one of them has a period. |

## The `ipx` tool

```
dotnet tool install --global Eplicta.Ipx.Tool

ipx compare taxonomy.ipx.json fortdocs.ipx.json uppsala.ipx.json stockholm.ipx.json --form compulsory-school
```

Each line is a leaf node and each column a plan; the first plan is the base. The colour of the other columns follows the delta: green same, yellow shorter, magenta longer, orange not comparable, red unclear or different, grey no rule. A mark after a cell tells how it was found: `+` several rows, `~` covered by a broader row, `/` covers only part of the node, `≈` only similar, `°` not a standing plan rule, `^` inherited from the parent node.

| Command | What it does |
|---|---|
| `ipx compare <taxonomy> <base> <plan>...` | The table above. `--only-differences`, `--form <form>`, `--language <tag>`, `--no-legend`. |
| `ipx coverage <taxonomy> <plan>...` | For each school form, how many leaf nodes each plan has a rule for. |
| `ipx gaps <taxonomy> <plan> <plan>...` | The nodes where plans disagree or split a rule over several rows. |
| `ipx missing <file>...` | What the documents say is missing, with the next step. |
| `ipx info <file>` | What a document is and how much it holds. |
| `ipx validate <file>...` | Validation. Exit code 1 when a document has an error. |

The exit code is 0 for success, 1 when the check made by `validate` fails, and 2 for wrong usage or a file that cannot be read. Colour is used only when the output is a terminal and the `NO_COLOR` variable is not set.
