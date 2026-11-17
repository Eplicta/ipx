# IPX: Information Plan Exchange

**Version 1.0.0** (draft, not yet released). Schema: [`ipx-1.0.0.schema.json`](../schema/ipx-1.0.0.schema.json). Example: [`school-plan.ipx.json`](../examples/school-plan.ipx.json).

IPX is an open format proposed by Eplicta. It is not a standard of any standards body, and nothing in this document says that it is adopted by anyone else.

IPX is a JSON format for exchanging an information management plan (*informationshanteringsplan*): what a school or an authority keeps, where it is kept and for how long. It also carries the rules that municipalities and national bodies have published, so that plans can be compared and built from them.

The words MUST, SHOULD and MAY are used as in RFC 2119 and RFC 8174.

## 1. At a glance

* One JSON document is one plan, one body of published rules, one recommendation, or one classification tree. The `kind` property says which.
* A document is a tree of **nodes** (what the organisation does or what information exists) and a list of **rows** (what is kept for each node, in which system, for how long).
* Retention is modelled so that nothing the source says is lost: the period as written, the trigger, the qualifier, the condition and the source's own words.
* Everything that came from somewhere is traceable: sources, the plans rows are attributed to, what is missing, and what was converted.
* A reader that saves a document MUST keep everything it does not understand. Extra data goes under `ext`, never beside the defined properties.
* A JSON Schema checks the structure. Section 10 lists the rules a schema cannot check.

## 2. Files

| Item | Value |
|---|---|
| Encoding | UTF-8 without a byte-order mark |
| Line ends | LF recommended; any JSON whitespace is valid |
| File extension | `.ipx.json` |
| Media type | `application/vnd.eplicta.ipx+json` (not registered) |
| Marker | `"format": "ipx"` and `"formatVersion": "1.0.0"` in every document |
| Schema | `https://eplicta.github.io/ipx/schema/1.0.0/ipx.schema.json` |

The schema URL is the intended address. A document MAY name it in `$schema` so that editors can validate while typing.

## 3. Conventions

**Identifiers.** An `id` is a string of ASCII letters, digits and `. _ : -`, at most 128 characters, starting with a letter or digit. Ids are compared as written, letter by letter, and upper and lower case differ. An id is unique within its list: no two nodes, no two rows, no two systems, and so on, share an id, but a node and a row may. It SHOULD stay the same when a new version of the same plan is written. The node ids of a taxonomy MUST stay the same in every version of that taxonomy, and so does its `document.id`, whose versions are told apart by `document.version`, because the mappings of other documents refer to them. A `uid` is a lower-case UUID that is unique everywhere. A `sourceId` is the identifier the thing had in the system or standard it came from. A `code` is what people see, such as `3.5.1`; it may change when rows are reordered, which is why it is not the identity.

**Dates.** `yyyy-MM-dd`, and a real calendar date: `2026-02-30` is not a date. Where the day or month is not known, a year (`2026`) or a year and month (`2026-11`) is allowed, but only in the properties that say so (a *partial date*). A partial date stands for the whole period it names: as `validFrom` it means the first day of the period, and as `validTo` the last day, inclusive. Never an invented date. No time of day and no time zone is part of a date.

**Null and absent.** A property that is absent has not been stated, or has its stated default. A property that is `null` has been looked for and is not known; only the properties that the schema allows to be `null` can be. Null never means zero, false or empty, and `validTo: null` means that no end is known. A reader treats an absent property and a property with its default value as equal. A writer MUST NOT guess a date, a URL, a code or a retention period.

**Text.** A translatable value is an object keyed by language tag: `{ "sv": "...", "en": "..." }`. At least one language is required. A machine translation MUST NOT be presented as the original. Verbatim source text (`asWritten`) and notes are plain strings: the language of verbatim text is `document.language`, and the language of notes is `document.noteLanguage`, which defaults to `document.language`.

**Order.** Array order is display order. There is no separate sort property. The order of properties in an object has no meaning.

**Defaults.** Writers SHOULD omit a property rather than write its default. The defaults are: `rowKind` is `rule`, `ruleType` is `plan-rule`, `current` is true, `noteLanguage` is the document language, and `sha256Basis` is `file`. `personalData`, `outOfScope`, `triggerInferred` and `confirmedPublic` have no default: when absent they have not been stated, which is not the same as false.

**Extension.** The only place for data that this specification does not define is an `ext` object, keyed by a reverse-domain name of lower-case labels such as `app.fortdocs.research`. Every object this specification defines MAY carry an `ext`, except a `Text` object and `asWritten.cells`, whose keys are open already. See section 11.

**Personal data.** Documents MUST NOT contain personal names. Contacts and responsibilities are roles and functional addresses.

**Canonical form and hashes.** Where a hash of IPX content is needed, for example to see whether a preserved fragment is still current, it is SHA-256 in lower-case hexadecimal over the JSON Canonicalization Scheme (RFC 8785) serialization of the value as written, so an explicit default gives another hash than an absent property. The hash of a file is over its bytes. `sha256Basis: text-extract` means the hash of the plain text extracted from the file, as UTF-8 with LF line ends, which can only be reproduced with the same extraction. `origins.content` is base64 (RFC 4648, standard alphabet, with padding) and `origins.sha256` is the hash of the decoded bytes. The arrays of a document are ordered, so the same values in another order hash differently.

**Text and encoding.** Strings are compared as written. No Unicode normalisation is applied to ids, codes, text or `ext` keys, and line ends in verbatim text are kept. A reader SHOULD accept a leading byte-order mark; a writer MUST NOT write one. A property name that occurs twice in one object is an error, because RFC 8785 requires I-JSON. Language tags follow BCP 47, with a lower-case primary subtag.

## 4. Versioning and compatibility

The version is `major.minor.patch`.

| Change | Bump |
|---|---|
| A property or an enum value is removed or renamed; a property changes type; an existing value changes meaning; a required property or a document kind is added; a constraint is tightened (a pattern, a length, a minimum, a closed value removed) | major |
| A property or an enum value is added; a constraint is relaxed; a property is deprecated; a validator warning is added | minor |
| Wording of the specification is clarified; the schema is corrected to match it without rejecting a document that was valid | patch |

Rules for readers:

1. A reader that supports version 1.x MUST accept any 1.y.z document, including one with a newer minor version than it knows.
2. A reader MUST keep unknown properties and unknown `ext` content when it saves the document. It MUST NOT drop them silently.
3. A reader that meets an enum value it does not know keeps it as it was. Where the value matters to the reader's own logic it treats it as the neutral value (`unclear` for a disposition, `other` for a trigger); for any other enumeration it does not interpret the value.
4. A reader MUST refuse a document of a major version it does not support, and a `kind` it does not know.
5. A validator checks a document against the schema of the version the document declares. The schema forbids unknown properties and closes its enumerations on purpose, so that a misspelt property or value is found. When the document declares a newer minor version than the validator knows, the validator MUST NOT report unknown properties or unknown enumeration values as errors. It SHOULD warn that part of the document was not checked, and it MUST still report every other schema error and every error of section 10.
6. A property or an enum value added in a minor version MUST be ignorable: it MUST NOT change the meaning of what version 1.0 defines. Whatever a reader would misread if it ignored the addition needs a major version.

## 5. The document

```
{
  "$schema", "format", "formatVersion", "kind",
  "document",
  "municipality", "organisation", "responsibilities", "operators",
  "systems", "nodes", "rows",
  "provenance", "ext"
}
```

`format`, `formatVersion`, `kind` and `document` are required.

### 5.1 Kinds

| `kind` | Meaning | Typical content |
|---|---|---|
| `plan` | The plan of one organisation, adopted or proposed. | `organisation`, `systems`, `nodes`, `rows` |
| `reference` | The rules one authority has published, for example a municipality's retention plans. | `municipality`, `operators`, `rows`, `provenance` |
| `recommendation` | Advice that binds nobody, such as the national *Gallringsråd*. | `rows`, `nodes`, `provenance` |
| `taxonomy` | A classification tree with no rows. | `nodes` |

A `taxonomy` MUST NOT have rows. Rows of other kinds point into a taxonomy with `mappings` (section 7.2).

### 5.2 `document`

| Property | Type | Meaning |
|---|---|---|
| `id` | UUID, required | Identity of this document. |
| `key` | id | A readable, stable name, such as `skr` or `common-tree`. |
| `name` | Text, required | The name. |
| `description` | Text | What the document is. |
| `version` | string | A label for this version of the content, chosen by the author. |
| `status` | `draft`, `proposed`, `adopted`, `superseded`, `withdrawn`, `unknown` | Where the document is in its life. |
| `language` | language tag, required | Language of verbatim content. |
| `noteLanguage` | language tag | Language of notes. |
| `createdOn`, `updatedOn` | date | |
| `validFrom`, `validTo` | partial date or `null` | When the content applies; `validTo` is the last day, inclusive. `null` means not known. |
| `supersedes` | array of `{ id?, version?, ref? }` | What this document replaces. `ref` is a URI; a path on one machine is never part of a document. |
| `coverage` | `complete`, `partial`, `none` | Whether the rules cover every school form the authority runs. |
| `coverageNote` | string | The limits of the coverage. |
| `taxonomy` | `{ id, version? }` | The taxonomy document that `mappings` point into. |
| `generator` | `{ name, version? }` | The tool that wrote the document. |

### 5.3 `municipality`, `organisation`, `operators`, `responsibilities`

`municipality` is `{ code, name, county? }` where `code` is the four-digit kommun code. A `reference` has the municipality whose rules it holds. A `plan` has the municipality the organisation operates in.

`organisation` is for a plan: `name`, `legalForm`, `forms` (school forms), `units` (name, kommun, code, forms, grades, `skolenhetskod`, `sourceUri`), `pupilTotal` (`value`, `basis`, `source`, `note`), where `source` is the id of an entry in `provenance.sources`, `regime` and `regimeNote`.

`regime` is one of:

| Value | Meaning |
|---|---|
| `smaller-16f` | A smaller independent huvudman. Chapter 29 section 16 f of the Education Act applies: keep at least seven years, and dispose earlier only as far as the municipality's corresponding activity does. |
| `larger-osl` | A larger independent huvudman, under the Public Access to Information and Secrecy Act and the archives act like an authority. |
| `public` | A public huvudman, with its own plan. |

This is a reading of the statute for classification, not legal advice.

`operators` lists the bodies of a municipality that run school forms: `{ body, forms, note? }`. `responsibilities` is a list of `{ role, duty }`.

### 5.4 `systems`

The information systems records are kept in: `{ id, name, kind?, purpose?, confidentiality?, confirmedPublic?, uri?, note? }`. `confirmedPublic` is true when the use of the system is stated in a public source.

## 6. Nodes

A node is one place in the classification tree.

| Property | Type | Meaning |
|---|---|---|
| `id` | id, required | Identity within the document. |
| `uid`, `sourceId`, `uri` | | Global identity, identity in the source, and a stable IRI. |
| `code` | string or `null` | The code people see. |
| `parent` | id or `null` | The parent node. `null` is a root. |
| `nodeKind` | `group`, `leaf` | Whether the node groups others or is the smallest comparable thing. |
| `level` | string | What the node is in its source, such as `class`, `series` or `folder`. Open vocabulary. |
| `form` | school form | The school form the node belongs to. |
| `title` | Text, required | |
| `description` | Text | |
| `concept` | id or `null` | Links nodes that mean the same in different school forms, for example attendance in preschool and in leisure-centre. |
| `caseRegistration` | string | How matters under the node are registered. |
| `system` | id | The system records under the node are kept in, unless a row says otherwise. |
| `references` | array of `{ scheme, code, title? }` | Codes in other schemes, such as `{ "scheme": "klassa", "code": "3.5.1" }`. |
| `source` | source reference | Where the node was read. |
| `validFrom`, `validTo` | partial date | |

School forms are: `shared` (governance and support every form needs), `preschool`, `preschool-class`, `leisure-centre`, `compulsory-school`, `upper-secondary-school`, `adult-education`, `other-education`.

## 7. Rows

A row says what is kept and for how long.

| Property | Type | Meaning |
|---|---|---|
| `id` | id, required | |
| `uid`, `sourceId` | | |
| `rowKind` | `rule`, `process`, `record-type`, `case-type` | Default `rule`. A `rule` is a retention rule as published. A `process` states the retention of a whole node. A `record-type` and a `case-type` are types of record and of case in a plan. |
| `plan` | id or `null` | The plan in `provenance.plans` the row is attributed to. |
| `forms` | array of school forms, required | The forms the row applies to. An empty list means that the forms are not stated. |
| `name`, `description` | Text | |
| `node` | id | The node in this document's tree the row sits under. |
| `mappings` | array | How the row corresponds to nodes in the taxonomy (7.2). |
| `suggestedNode` | `{ id, title, form? }` | A node that would hold a row that fits none in the taxonomy yet. |
| `asWritten` | object | The source's wording (7.1). |
| `registration` | `registered`, `kept-ordered` | Entered in the diary, or kept in order without being registered. |
| `registrationText` | string | The source's own wording. |
| `personalData` | boolean | Whether records contain personal data. Absent means not stated. |
| `secrecy` | string | The name of a secrecy class. |
| `system` | id | Where the records are kept. |
| `references` | array | Codes in other schemes. |
| `retention` | array of retention, required, at least one | What happens and when (7.3). A row whose source states no retention has one entry with `disposition: unclear`. |
| `source` | source reference | Where in which source the row was read. |
| `confidence` | `high`, `medium`, `low` | How sure the reader is of the normalisation. |
| `current` | boolean | False when the row no longer applies to new documents. Default true. |
| `currentNote` | string | Why. |
| `ruleType` | see below | What kind of statement the row is. Default `plan-rule`. |
| `trackedChange` | `{ state, detail? }` | How a source with tracked changes marks the row. |
| `outOfScope` | boolean | The row is about something other than school activities. |

`ruleType` is one of `plan-rule` (an ordinary row), `one-off-decision` (a single gallring decision, for example at a system change), `gdpr-limit` (a storage period from a GDPR register, not a gallring decision), `platform-statement`, `general-principle`, `instruction`, `promise` (a public promise about removal, not retention), `placeholder` (a pointer to a decision that is not itself public), and `recommendation-quote`.

A source reference is `{ ref, page?, printedPage?, continuesOnPage?, section?, locator? }` where `ref` is the id of an entry in `provenance.sources`.

### 7.1 `asWritten`

`{ code, heading, text, storage, page, continuesOnPage, cells }`. The wording is kept verbatim, in the document language. `cells` holds further columns of the source table by column name. Retention in a row is the reader's normalisation of this wording. Where in doubt, a writer uses `unclear` and keeps the text.

### 7.2 `mappings`

`{ node, relation }`, where `node` is the id of a node in the taxonomy named by `document.taxonomy`.

| Relation | Meaning |
|---|---|
| `exact` | The row is the same thing as the node. |
| `covers` | The row is wider than the node; the value is inherited by it. |
| `part` | The row is only part of the node. |
| `similar` | Close but not the same. |

A row that covers several nodes maps to each of them. Several rows for one node are all kept.

### 7.3 Retention

A row has one or more retention entries. A source that gives separate periods, for example for granted and refused cases, becomes several entries with an `outcome`, not several rows. Several entries are alternatives, and each entry after the first MUST say when it applies with an `outcome`, a `condition` or a `qualifier`; several entries that say none of these make the row unreadable and it is treated as `unclear`. An entry with `qualifier: earliest` and one with `qualifier: latest` in the same row are read together as a range.

| Property | Type | Meaning |
|---|---|---|
| `disposition` | required | `retain`: keep for good. `dispose`: gallras. `handover`: pass to another party. `review`: decide later. `unclear`: the source cannot be read as one rule. |
| `dispositionText` | string | The source's own word. |
| `period` | `{ amount, unit }` | The period as written, with `unit` one of `day`, `month`, `year`. |
| `months` | integer or `null` | The period in whole months, rounded up. Days count 30 to a month. `null` when the source gives no period. |
| `trigger` | enum or `null` | What the period counts from. |
| `triggerText` | string | The source's own words. |
| `triggerInferred` | boolean | True when the trigger comes from a general statement of the plan, not from the row. |
| `qualifier` | `earliest`, `latest` | *At the earliest*, or *at the latest*. |
| `latestEvent` | string | A latest event in the source's words. |
| `condition` | string | A condition in the source's words. |
| `outcome` | `grant`, `refusal` | Which outcome of a case the entry applies to. |
| `ruleKey` | string | The key of the retention rule in the system the document came from. |
| `legalBasis` | string | The law, decision or recommendation it rests on. |
| `note` | string | |

`trigger` is one of:

| Value | Meaning |
|---|---|
| `from-creation` | From when the document was created. |
| `from-receipt` | From when the document was received. |
| `year-end` | After the end of the calendar year. |
| `financial-year-end` | After the end of the financial year. |
| `school-year-end` | After the end of the school year. |
| `case-closed` | After the case was closed. |
| `enrolment-end` | After the child or pupil left (placement ended, discharged, schooling ended). |
| `decision-date` | After the date of the decision. |
| `superseded` | Until the document is replaced by a new version. |
| `obsolete` | *Vid inaktualitet*: when the document is no longer needed. No period is given. |
| `other` | Another event; describe it in `triggerText` or `note`. |
| `unspecified` | The source gives a period but not what it counts from. |
| `none` | No trigger. Belongs to `retain`. |

Rules for retention:

1. Different triggers are never converted into each other. A reader compares periods only when the triggers are the same. A period whose trigger is `unspecified` MAY be compared with a period of any trigger, and a tool that does so SHOULD say that the comparison is uncertain.
2. `months` is given only when the source gives a period. `retain` has no period. Its trigger is `none`, or the event that a later review or handover is counted from.
3. *Gallras vid/efter* an event with no extra time is `dispose` with `months: null` and `trigger` set to the event.
4. A plain period of N years in a plan that states what such a period counts from carries that trigger with `triggerInferred: true`. Without such a statement, the trigger is `unspecified`.
5. *At the earliest N years* is `months` plus `qualifier: earliest`. *At the latest* is `qualifier: latest`.
6. A row with several periods or conditions that cannot be split into entries is `unclear`, and its text is kept.
7. If `period` and `months` are both given, `months` SHOULD be the period converted as above; a validator warns when it is not.

The national guidance counts N years as N whole calendar years after the year the document was created or the case closed. A document that follows this convention uses `trigger: year-end`; a plan that copies the guidance without saying so stays `unspecified`.

## 8. Provenance

`provenance` records where the content came from and what is known about it. Everything in it is optional. `sources`, `plans`, `missing` and `checks` describe the research behind a document that was read from published sources; an exchange of a plan between systems has no need of them. The required properties are `standard` for an origin; `on`, `from` and `to` for a conversion; `id` and `title` for a source; `id` and `name` for a plan; `id` and `kind` for a missing entry; and `on` and `outcome` for a check.

* **`origins`**: the documents or systems this one was converted from: `{ standard, version?, mediaType?, uri?, sha256?, content?, note? }`. `standard` names what the original was: a standard, a format or a reverse-domain name. `content` is the original, base64-encoded, when it is small enough to keep.
* **`conversions`**: each conversion the document has been through: `{ on, from, to, tool?, losses[] }`. A converter lists in `losses` what it could not carry.
* **`sources`**: the documents the content was read from: `{ id, uri, title, kind, publisher, edition, publishedDate, publishedDateBasis, lastFetchedAt, fetchedBasis, sha256, sha256Basis, note, uriNote }`. `kind` is `plan-pdf`, `plan-html`, `decision`, `protocol`, `page`, `recommendation-pdf` or `other`. `publishedDateBasis` is `meeting-date`, `document-metadata`, `page-stated` or `unknown`. `fetchedBasis` is `verified`, `file-timestamp`, `report-date`, `user-supplied-copy` or `null`. `sha256` is the lower-case hash of the file, or of its text extract when `sha256Basis` is `text-extract`. A path on one person's machine is never part of the document.
* **`plans`**: the plans and decisions rows are attributed to: `{ id, name, body, scope, forms, decidedDate, decisionRef, publishedDate, publishedDateBasis, lastKnownPlanDate, proposedValidFrom, status, structure, delegatesTo, inaktualitet, periodCounting, assessment, sourceIds }`.
  * `scope`: `namnd` (one committee), `kommunovergripande` (the whole municipality), `huvudman` (one organisation).
  * `status`: `text-public`, `draft-only`, `not-public`, `not-found`, `unreachable`, `not-researched`.
  * `structure`: `klassa`, `school-vir`, `own`, `unknown`.
  * `delegatesTo`: `{ planId, topics[], note? }`, where a plan hands topics to another plan.
  * `inaktualitet`: `{ defined, text }`, whether and how the plan defines *vid inaktualitet*.
  * `periodCounting`: `{ startsFrom, scope, statement, where, note }`, what a plain period of N years counts from, when the plan says so. `startsFrom` is `year-end`. `scope` is `all-rows` or `some-rows`.
  * `assessment`: `{ level, note }`, the reviewer's view of how usable the plan is; `level` is `high`, `medium` or `low`.
* **`missing`**: what is not usable yet and how to get it: `{ id, kind, forms, nodes, detail, nextStep, contact, request }`. `kind` is `plan-not-public`, `plan-not-found`, `site-unreachable`, `form-not-covered`, `delegated-plan-unpublished`, `plan-draft-only`, `text-unreadable`, `rules-vague`, `rows-placeholder`, `rows-not-extracted`, `newer-version-possible`, `source-needs-recheck` or `other`. `nextStep` is `recheck-homepage`, `contact-city`, `ask-archive`, `retry-later` or `none`. `contact` is `{ role, address }`: a role and a functional address, never a person.
* **`checks`**: `{ on, outcome, note }` with `outcome` one of `extracted`, `unchanged`, `changed`, `new`, `unreachable`.

## 9. Mapping to FortDocs (informative)

This is how a FortDocs classification scheme and the IPX format correspond. Export and import follow it.

| FortDocs | IPX | Note |
|---|---|---|
| Classification scheme: `Key`, `Name`, `Description`, `Version` | `document.key`, `name`, `description`, `version` | `Standard` has no counterpart and is kept in `ext`. |
| Process class: `Code`, `Name`, `Description`, `ParentId` | node: `code`, `title`, `description`, `parent` | The node `id` is a stable slug made once and kept. |
| Process class: `CaseRegistration`, `RegisteredInSystemId` | node `caseRegistration`, `system` | |
| Process class: retention rule | a row with `rowKind: process` and `node` set | |
| Case type | row with `rowKind: case-type`, `node` set | |
| Record type | row with `rowKind: record-type`, `node` set | `RegistrationMethod` becomes `registration`; `ContainsPersonalData` becomes `personalData`; the secrecy class becomes `secrecy`. |
| Retention rule: `Disposition`, `RetentionMonths`, `LegalBasis`, `Key` | retention: `disposition`, `months`, `legalBasis`, `ruleKey` | FortDocs has only `retain` and `dispose`. Importing `handover`, `review` or `unclear` is lossy and is reported. FortDocs has no trigger, qualifier or condition; those are kept in `ext` on import. |
| System | `systems` entry | |
| Deactivated (`DeactivatedAt`) | `current: false`, or `validTo` on a node | |

## 10. Validation

Three levels, run in this order:

1. **Schema.** The JSON Schema checks types, required properties, enum values, patterns and that no unknown property sits outside `ext`. Validators exist for most languages; any that supports JSON Schema draft 2020-12 will do.
2. **Rules a schema cannot check.** Each is an error:
   * Ids are unique within each of nodes, rows, systems, sources, plans and `missing`.
   * `node.parent`, `node.system`, `row.node` and `row.system` refer to something that exists.
   * `row.plan` refers to a plan, and `node.source.ref`, `row.source.ref`, `plan.sourceIds` and `organisation.pupilTotal.source` refer to sources, in `provenance`.
   * Every date is a real calendar date.
   * The parent chain of a node does not loop.
   * A `taxonomy` has no rows.
3. **Warnings.** These do not make a document invalid:
   * `months` does not match `period`.
   * A `retain` entry has a period, in `period` or in `months`.
   * A mapping points at a node that is not in the taxonomy. A validator can check this only when it is given the taxonomy.
   * A plan delegates to a plan that is not listed.
   * The document's minor version is newer than the validator knows.

A validator MUST report errors and warnings separately, and MUST run the second level only on a document that passed the first. That a `reference` has a `municipality`, that a `plan` has an `organisation`, and that a row of kind `process`, `record-type` or `case-type` has a `node`, are expectations of the kinds and not checked.

## 11. Extension and round trips

A document that has been converted from another format, or that is converted to one, should come back unchanged. These rules make that possible:

1. **Keep what has no home.** Whatever the source holds that IPX has no property for goes under `ext` on the object it belongs to, in the same shape as the source, under a reverse-domain key that names the source (for example `app.fortdocs.research`).
2. **Keep the original value.** Where a source value is mapped to an IPX value that several source values map to, the source value is kept as well, so the export can restore it.
3. **Keep the source's identity.** `sourceId` carries the source's own id; it is never regenerated.
4. **Keep the literal.** A date, a time zone, a unit, a language tag or a spelling is stored as the source wrote it, with the normalised value beside it.
5. **Name the original.** `provenance.origins` records which standard or format and which version the document came from, and a hash of it.
6. **Say what was lost.** A converter that cannot carry something lists it in `provenance.conversions[].losses`.
7. **Know what changed.** A preserved fragment SHOULD carry a hash of the mapped fields it came from (over the canonical form, section 3). On export a fragment whose fields are unchanged is written as it was, and one whose fields changed is rewritten while its foreign extras are kept.

Equality after a round trip means *canonically identical*: the same JSON value regardless of property order, or for XML the same canonical XML. Byte-identity is promised only when the document has not changed since it was converted and the original is kept in `provenance.origins[].content`.

Some targets cannot carry extras because their schema has no extension point. For those, the document is exported next to the target file as a pair.

### Experience with a real corpus

The authors converted their own research corpus, 26 documents in an earlier internal format (20 municipalities, 2 plans of one huvudman, 3 baselines and a common tree, 11,230 rows and 569 nodes in all), to IPX 1.0.0 and back. Every document validated against the schema and came back identical to the original. The corpus and the converter are not part of this repository.

## 12. Related standards

IPX does not replace any of these, and none covers a plan with verbatim wording, triggers, sources and a comparison tree. They exchange archives and structures.

| Standard | What it covers | Relation to IPX |
|---|---|---|
| Riksarkivet FGS Arkivredovisning (verksamhetsbaserad, RAFGS5) | An archive description in XML: EAD3 for the archive and EAC-CPF for the archive creator, with a classification structure, record groups and record types. Retention appears only as prose in `appraisal`. | The closest published format to a plan. Nodes and rows map well. Retention can be imported only as text, so it arrives as `unclear` with the wording kept. |
| Riksarkivet ERMS application of CITS (`ermsClassifications.xml`) | A closed XML tree of names. A unit has a name, a description and nested units, with no id, no code and no extension point. Riksarkivet marks its `disposal` element as not used in this export. | Carries structure only. An exporter needs the IPX document beside it to be lossless. |
| CITS ERMS 2.1.2, the main case export | A per-case transfer format. `aggregation` and `record` can carry a `disposal` element with a free-text period, and extra data through `additionalXMLData`. | Not a plan format. No converter is planned. |
| Noark 5 (Norway) | Classification system and archive structure in XML. `kassasjon` holds a decision, its basis, a retention time in whole years and a date; one per unit. No trigger, no language attribute, no extension point on a class. | An importer can be lossless by keeping the original XML under `ext`. An exporter is best effort and needs a side-by-side pair. |
| MoReq2010 | A disposal schedule per class, with an action, a trigger, an interval and an offset, and contextual elements for extra data. | The closest model of retention. An exporter is feasible in a single file. |
| RiC-O (International Council on Archives) | An ontology for describing archival resources | Out of scope as a converter. |
| KLASSA (SKR) | The classification scheme for Swedish local government | Carried as `references` with `scheme: klassa`. |
| Riksarkivet FGS Paketstruktur, Personal, Databas, Ärendehantering | Packages, personnel records, database dumps and cases | Not plan formats. Out of scope. |

None of the targets has a home for a trigger, a qualifier, a condition, an outcome, several retention entries on one row, or the structure of `asWritten`. A converter to one of them lists what it drops in `provenance.conversions[].losses` (section 11).

These descriptions come from the published XML schemas and specifications as read by the authors in November 2026. Two were read from copies: the MoReq schemas come from an archived copy, and the FGS tightening of EAD3 is known from the specification text, not from a schema. No converter is part of IPX 1.0.0.

## 13. Other serializations

IPX 1.0.0 defines JSON only. A second serialization, such as XML with an XSD, would carry the same data. The design keeps that possible:

* Every property name is a valid XML name, except `$schema`, which has no XML counterpart.
* Free-keyed objects are limited to `Text` (language tags), `ext` (reverse-domain keys) and `asWritten.cells` (column names). An XML form writes these as keyed entries.
* There are no mixed types except *value or `null`*, which XML expresses as an absent or empty element with an explicit marker. Inside `ext` any JSON value is allowed, so an XML form carries it as an opaque JSON string.
* Array order is meaningful and never implied by property order.

## 14. Changes

**1.0.0**: first version. It grows out of an earlier internal research format (`schemaVersion: 1`), which converts to it and back without loss. Changes from that format:

* The format has a name and a semantic version; the `kind` field is uniform.
* Retention keeps the source's period and unit, allows several entries per row, and gives `review` and `handover` a place beside `retain` and `dispose`.
* Rows have a stable identity (`id`, optional `uid` and `sourceId`) and separate their place in the plan (`node`) from their mapping to the taxonomy (`mappings`).
* Notes, sources and plans moved to `provenance`; research-job data (review notes, fix history, alignment with another plan) moved to `ext`.
* Controlled vocabularies are part of the specification instead of a separate file; every value has one meaning.
* Machine-specific file paths are no longer part of the document.
* Unknown properties are preserved, not ignored.
