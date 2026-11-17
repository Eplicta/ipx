using System.Text.Json.Nodes;

namespace Eplicta.Ipx;

/// <summary>The wording of the source, kept verbatim in the language of the document.</summary>
public sealed class IpxAsWritten : IpxObject
{
    private const string CellsName = "cells";

    public IpxAsWritten() : base(new JsonObject())
    {
    }

    public IpxAsWritten(JsonObject node) : base(node)
    {
    }

    public string Code
    {
        get => GetString("code");
        set => SetString("code", value);
    }

    public string Heading
    {
        get => GetString("heading");
        set => SetString("heading", value);
    }

    public string Text
    {
        get => GetString("text");
        set => SetString("text", value);
    }

    public string Storage
    {
        get => GetString("storage");
        set => SetString("storage", value);
    }

    public int? Page
    {
        get => GetInt("page");
        set => SetInt("page", value);
    }

    public int? ContinuesOnPage
    {
        get => GetInt("continuesOnPage");
        set => SetInt("continuesOnPage", value);
    }

    /// <summary>The names of the further columns of the source table that are kept.</summary>
    public IEnumerable<string> CellNames => Node[CellsName] is JsonObject cells ? cells.Select(x => x.Key).ToArray() : [];

    /// <summary>The text of one further column of the source table, or null when there is none.</summary>
    public string GetCell(string column) => Node[CellsName] is JsonObject cells && cells[column] is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;

    public void SetCell(string column, string value)
    {
        if (value is null)
        {
            if (Node[CellsName] is JsonObject existing) existing.Remove(column);
            return;
        }

        if (Node[CellsName] is not JsonObject cells)
        {
            cells = new JsonObject();
            Node[CellsName] = cells;
        }

        cells[column] = value;
    }
}
