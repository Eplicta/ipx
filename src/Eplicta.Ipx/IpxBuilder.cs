using System.Globalization;
using System.Text.Json.Nodes;
using Eplicta.Ipx.Validation;

namespace Eplicta.Ipx;

/// <summary>
/// Builds an IPX document from code. Each call sets or adds one thing and returns the builder, so a document reads as one chain, and <see cref="Build"/> hands over the finished document.
/// The properties of the result are written in the order of the schema, so the same input always gives the same text.
/// </summary>
public sealed class IpxBuilder
{
    private const string DateFormat = "yyyy-MM-dd";

    private readonly IpxDocument _document;
    private readonly IpxDocumentInfo _info;
    private readonly HashSet<string> _systemIds = new(StringComparer.Ordinal);
    private readonly HashSet<string> _nodeIds = new(StringComparer.Ordinal);
    private readonly HashSet<string> _rowIds = new(StringComparer.Ordinal);

    public IpxBuilder(IpxKind kind)
    {
        _document = IpxDocument.Create(kind, null, null, null);
        _info = _document.Document;
    }

    /// <summary>The UUID of the document. When none is set, one is made the first time the document is built.</summary>
    public IpxBuilder SetId(string id)
    {
        _info.Id = id;
        return this;
    }

    /// <summary>A readable, stable name for the document, such as <c>skr</c> or <c>fortdocs-default</c>.</summary>
    public IpxBuilder SetKey(string key)
    {
        _info.Key = key;
        return this;
    }

    /// <summary>Replaces the name of the document in every language.</summary>
    public IpxBuilder SetName(IpxText name)
    {
        _info.Name = Copy(name);
        return this;
    }

    /// <summary>Sets the name of the document in one language and leaves the others as they are.</summary>
    public IpxBuilder SetName(string language, string name)
    {
        ArgumentNullException.ThrowIfNull(language);

        var text = _info.Name ?? new IpxText();
        text[language] = name;
        _info.Name = text;
        return this;
    }

    /// <summary>Replaces the description of the document in every language.</summary>
    public IpxBuilder SetDescription(IpxText description)
    {
        _info.Description = Copy(description);
        return this;
    }

    /// <summary>Sets the description of the document in one language and leaves the others as they are.</summary>
    public IpxBuilder SetDescription(string language, string description)
    {
        ArgumentNullException.ThrowIfNull(language);

        var text = _info.Description ?? new IpxText();
        text[language] = description;
        _info.Description = text;
        return this;
    }

    /// <summary>A label for this version of the content, chosen by the author.</summary>
    public IpxBuilder SetVersion(string version)
    {
        _info.Version = version;
        return this;
    }

    /// <summary>One of draft, proposed, adopted, superseded, withdrawn or unknown.</summary>
    public IpxBuilder SetStatus(string status)
    {
        _info.Status = status;
        return this;
    }

    /// <summary>The language of verbatim content such as the text of a row as written. A language tag such as <c>sv</c>.</summary>
    public IpxBuilder SetLanguage(string language)
    {
        _info.Language = language;
        return this;
    }

    /// <summary>The language of notes and explanations, when it is not the language of the content.</summary>
    public IpxBuilder SetNoteLanguage(string language)
    {
        _info.NoteLanguage = language;
        return this;
    }

    public IpxBuilder SetCreatedOn(DateOnly date)
    {
        _info.CreatedOn = Format(date);
        return this;
    }

    public IpxBuilder SetUpdatedOn(DateOnly date)
    {
        _info.UpdatedOn = Format(date);
        return this;
    }

    public IpxBuilder SetValidFrom(DateOnly date)
    {
        _info.ValidFrom = Format(date);
        return this;
    }

    /// <summary>When the document takes effect. A partial date such as <c>2027</c> or <c>2027-01</c> is allowed.</summary>
    public IpxBuilder SetValidFrom(string date)
    {
        _info.ValidFrom = date;
        return this;
    }

    public IpxBuilder SetValidTo(DateOnly date)
    {
        _info.ValidTo = Format(date);
        return this;
    }

    /// <summary>When the document stops applying. A partial date such as <c>2027</c> or <c>2027-01</c> is allowed.</summary>
    public IpxBuilder SetValidTo(string date)
    {
        _info.ValidTo = date;
        return this;
    }

    /// <summary>The software that wrote the document.</summary>
    public IpxBuilder SetGenerator(string name, string version = null)
    {
        ArgumentNullException.ThrowIfNull(name);

        var generator = new JsonObject { ["name"] = name };
        if (version is not null) generator["version"] = version;
        _info.Node["generator"] = generator;
        return this;
    }

    public IpxBuilder SetMunicipality(string code, string name, string county = null)
    {
        _document.Municipality = new IpxMunicipality { Code = code, Name = name, County = county };
        return this;
    }

    /// <summary>The organisation the plan is for. Use the action to set what the parameters do not cover, such as the school forms and the regime.</summary>
    public IpxBuilder SetOrganisation(string name, Action<IpxOrganisation> configure = null)
    {
        var organisation = new IpxOrganisation { Name = name };
        configure?.Invoke(organisation);
        _document.Organisation = organisation;
        return this;
    }

    /// <summary>A role and what it is responsible for. Roles only, never personal names.</summary>
    public IpxBuilder AddResponsibility(string role, string duty)
    {
        _document.Responsibilities.Add(new IpxResponsibility { Role = role, Duty = duty });
        return this;
    }

    /// <summary>A body in the municipality that runs the given school forms.</summary>
    public IpxBuilder AddOperator(string body, IEnumerable<IpxForm> forms, string note = null)
    {
        ArgumentNullException.ThrowIfNull(forms);

        var item = new IpxOperator { Body = body, Note = note };
        foreach (var form in forms)
        {
            item.Forms.Add(form);
        }

        _document.Operators.Add(item);
        return this;
    }

    public IpxBuilder AddSystem(string id, string name, Action<IpxSystem> configure = null)
    {
        var system = new IpxSystem { Id = id, Name = name };
        configure?.Invoke(system);
        return AddSystem(system);
    }

    /// <summary>Adds a copy of the system. Changing the original afterwards does not change the document.</summary>
    public IpxBuilder AddSystem(IpxSystem system)
    {
        ArgumentNullException.ThrowIfNull(system);
        RequireNew(_systemIds, system.Id, "system");

        _document.Systems.Add(new IpxSystem(Copy(system.Node)));
        return this;
    }

    /// <summary>Adds a node of the tree. Use the action for the code, parent, title and the rest.</summary>
    public IpxBuilder AddNode(string id, Action<IpxNode> configure = null)
    {
        var node = new IpxNode { Id = id };
        configure?.Invoke(node);
        return AddNode(node);
    }

    /// <summary>Adds a copy of the node. Changing the original afterwards does not change the document.</summary>
    public IpxBuilder AddNode(IpxNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        RequireNew(_nodeIds, node.Id, "node");

        _document.Nodes.Add(new IpxNode(Copy(node.Node)));
        return this;
    }

    /// <summary>Adds a row. Use the action for the name, the node it sits under, the retention and the rest.</summary>
    public IpxBuilder AddRow(string id, IpxRowKind rowKind, Action<IpxRow> configure = null)
    {
        var row = new IpxRow { Id = id, RowKind = rowKind };
        configure?.Invoke(row);
        return AddRow(row);
    }

    /// <summary>Adds a copy of the row. Changing the original afterwards does not change the document. A row that names no school forms gets an empty list, which means that the forms are not stated.</summary>
    public IpxBuilder AddRow(IpxRow row)
    {
        ArgumentNullException.ThrowIfNull(row);
        RequireNew(_rowIds, row.Id, "row");

        var copy = Copy(row.Node);
        if (!copy.ContainsKey("forms")) copy["forms"] = new JsonArray();

        _document.Rows.Add(new IpxRow(copy));
        return this;
    }

    /// <summary>Records a document or system the content was converted from.</summary>
    public IpxBuilder AddOrigin(Action<IpxOrigin> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var origin = new IpxOrigin();
        configure(origin);
        _document.EnsureProvenance().Origins.Add(origin);
        return this;
    }

    /// <summary>Records a conversion the content has been through and what it lost.</summary>
    public IpxBuilder AddConversion(Action<IpxConversion> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var conversion = new IpxConversion();
        configure(conversion);
        _document.EnsureProvenance().Conversions.Add(conversion);
        return this;
    }

    /// <summary>Adds data of your own under <c>ext</c>. The key is a reverse-domain name such as <c>app.fortdocs.research</c>.</summary>
    public IpxBuilder SetExtension(string key, JsonNode value)
    {
        ArgumentNullException.ThrowIfNull(key);

        _document.EnsureExt().Set(key, value?.DeepClone());
        return this;
    }

    /// <summary>The way to reach what the builder has no method for. The action sees the document as it is built so far.</summary>
    public IpxBuilder Configure(Action<IpxDocument> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        configure(_document);
        return this;
    }

    /// <summary>
    /// Returns the document. It is a copy, so the builder can be changed and built again.
    /// Throws <see cref="InvalidOperationException"/> when a property every document needs is missing: the name or the language. It does not check the content; <see cref="BuildValidated"/> does.
    /// </summary>
    public IpxDocument Build()
    {
        var missing = new List<string>();
        if (_info.Name is null || !_info.Name.Languages.Any()) missing.Add("a name");
        if (string.IsNullOrWhiteSpace(_info.Language)) missing.Add("a language");

        if (missing.Count > 0) throw new InvalidOperationException($"The document needs {string.Join(" and ", missing)}.");

        _info.Id ??= Guid.NewGuid().ToString();

        var node = Copy(_document.Node);
        IpxPropertyOrder.Apply(node);
        return new IpxDocument(node);
    }

    /// <summary>Returns the document after checking it against the schema and the other rules. Throws <see cref="IpxValidationException"/> when there are errors; warnings do not stop it.</summary>
    public IpxDocument BuildValidated(IpxValidationOptions options = null)
    {
        var document = Build();
        var result = IpxValidator.Validate(document, options);
        if (!result.IsValid) throw new IpxValidationException(result);

        return document;
    }

    private static void RequireNew(HashSet<string> ids, string id, string what)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentException($"A {what} needs an id.");
        if (!ids.Add(id)) throw new ArgumentException($"There is already a {what} with the id '{id}'.");
    }

    private static string Format(DateOnly date) => date.ToString(DateFormat, CultureInfo.InvariantCulture);

    private static IpxText Copy(IpxText text) => text is null ? null : new IpxText(Copy(text.Node));

    private static JsonObject Copy(JsonObject node) => (JsonObject)node.DeepClone();
}
