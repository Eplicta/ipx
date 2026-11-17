# Building documents from code

`IpxBuilder` builds a document as one chain of calls. Each call sets or adds one thing and returns the builder; `Build` hands over the finished document. It is the way to produce IPX from the data of another system.

```csharp
using Eplicta.Ipx;

var plan = new IpxBuilder(IpxKind.Plan)
    .SetName("sv", "Exempelskolans plan")
    .SetName("en", "Example School plan")
    .SetLanguage("sv")
    .SetOrganisation("Exempelskolan AB", o => o.Forms.Add(IpxForm.CompulsorySchool))
    .AddNode("governance", n =>
    {
        n.Code = "1";
        n.Title = IpxText.Create(("sv", "Styra verksamheten"), ("en", "Govern the operation"));
    })
    .AddRow("board-minutes", IpxRowKind.RecordType, r =>
    {
        r.Name = IpxText.Create(("sv", "Styrelseprotokoll"), ("en", "Board minutes"));
        r.NodeId = "governance";
        r.Forms.Add(IpxForm.Shared);
        r.Registration = IpxRegistration.Registered;
        r.Retention.Add(IpxRetention.Retain());
    })
    .AddRow("admission", IpxRowKind.CaseType, r =>
    {
        r.Name = IpxText.Create(("sv", "Ansökan om skolplats"), ("en", "Application for a school place"));
        r.NodeId = "governance";
        r.Forms.Add(IpxForm.CompulsorySchool);
        r.Retention.Add(IpxRetention.Dispose(6, IpxPeriodUnit.Year, IpxTrigger.CaseClosed));
    })
    .BuildValidated();

File.WriteAllText("plan.ipx.json", IpxSerializer.ToJson(plan));
```

## What every document needs

A name in at least one language and the language of its content. `Build` throws an `InvalidOperationException` that names what is missing. The id is a UUID; when you do not set one, the builder makes one the first time you build, and keeps it for later builds.

## What the builder gives you

| Method | Sets |
|---|---|
| `SetId`, `SetKey`, `SetVersion`, `SetStatus` | The identity and state of the document. |
| `SetName`, `SetDescription` | Text in one language per call, or an `IpxText` that replaces all of it. |
| `SetLanguage`, `SetNoteLanguage` | The language of the content and of the notes. |
| `SetCreatedOn`, `SetUpdatedOn`, `SetValidFrom`, `SetValidTo` | Dates, as a `DateOnly` or, for the two validity dates, a partial date such as `"2027-01"`. |
| `SetGenerator` | The software that wrote the document. |
| `SetMunicipality`, `SetOrganisation` | Who the document is for. |
| `AddResponsibility`, `AddOperator` | Roles and the bodies that run school forms. |
| `AddSystem`, `AddNode`, `AddRow` | The parts of the plan. |
| `AddOrigin`, `AddConversion` | Where the content came from and what a conversion lost. |
| `SetExtension` | Data of your own under `ext`, keyed by a reverse-domain name. |
| `Configure` | Anything else: the action sees the document as built so far. |

`AddNode`, `AddRow` and `AddSystem` take an id and an action that fills in the rest, or an object you made yourself. The builder keeps a copy, so changing the object afterwards does not change the document. An id that is missing or already used is refused when you add it, not when you validate.

## Retention

The retention entry has factories for the common cases. They set the disposition, the period, the months and the trigger together.

```csharp
IpxRetention.Retain();                                                   // kept, no period
IpxRetention.Dispose(6, IpxPeriodUnit.Year, IpxTrigger.CaseClosed);      // disposed of 6 years after the case is closed
IpxRetention.Dispose(IpxTrigger.Superseded);                             // disposed of when superseded
IpxRetention.Handover(10, IpxPeriodUnit.Year, IpxTrigger.YearEnd);       // handed over 10 years after the end of the year
```

Months are the period rounded up, with a month counted as 30 days, the same rule the validator checks. For anything the factories do not cover, such as a qualifier, an outcome or the source's own words, set the property on the entry:

```csharp
var entry = IpxRetention.Dispose(2, IpxPeriodUnit.Year, IpxTrigger.CaseClosed);
entry.Outcome = IpxOutcome.Refusal;
entry.Condition = "Unless the decision is appealed.";
```

A row can hold several entries, one for each outcome or condition.

## Build or build validated

`Build` checks only that the document has a name and a language, so it can be used for a document you mean to finish later. `BuildValidated` also validates against the schema and the other rules, and throws an `IpxValidationException` whose `Result` holds every error. Warnings do not stop it. Use it where an invalid document should never leave the system, such as an export.

```csharp
try
{
    var document = builder.BuildValidated();
}
catch (IpxValidationException exception)
{
    foreach (var error in exception.Result.Errors) Console.WriteLine(error);
}
```

## The text you get

The builder writes the properties in the order the schema lists them, whatever order you set them in, so the same input gives the same text. Build twice and you get two separate documents; changing the builder after a build does not change the document you already have.
