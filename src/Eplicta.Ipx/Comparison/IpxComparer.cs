namespace Eplicta.Ipx.Comparison;

/// <summary>
/// Compares the retention in several documents node by node, through a shared taxonomy. Each row of a document says which taxonomy nodes it corresponds to (<see cref="IpxRow.Mappings"/>); the comparer reads the documents only through those mappings.
/// The first document is the base: every other document is measured against it. Triggers are never converted into each other, so periods that count from different events are reported as not comparable.
/// </summary>
public static class IpxComparer
{
    private const int RetainRank = 100000;
    private const string DefaultRuleType = "plan-rule";

    private static readonly HashSet<string> NotRetention = new(StringComparer.Ordinal) { "instruction", "promise", "placeholder", "general-principle", "recommendation-quote" };
    private static readonly HashSet<string> NotStanding = new(StringComparer.Ordinal) { "one-off-decision", "gdpr-limit", "platform-statement" };

    /// <summary>
    /// Compares the documents at every leaf node of the taxonomy. Only rows that are current and that state a retention are used; a node without a row of its own takes the rule of its parent when the parent is not a root.
    /// Where several rows sit at the closest relation to a node, the longest-keeping one stands for the cell and the cell text shows the range.
    /// </summary>
    public static IpxComparison Compare(IpxDocument taxonomy, IEnumerable<IpxDocument> documents, IpxCompareOptions options = null)
    {
        ArgumentNullException.ThrowIfNull(taxonomy);
        ArgumentNullException.ThrowIfNull(documents);

        var compared = documents.ToList();
        if (compared.Count == 0) throw new ArgumentException("At least one document is needed: the base.", nameof(documents));

        options ??= new IpxCompareOptions();

        var nodes = taxonomy.Nodes.ToList();
        var byId = new Dictionary<string, IpxNode>(StringComparer.Ordinal);
        foreach (var node in nodes)
        {
            if (node.Id is not null) byId.TryAdd(node.Id, node);
        }

        var indexes = compared.Select(Index).ToList();
        var lines = new List<IpxComparisonLine>();

        foreach (var node in nodes)
        {
            if (node.NodeKind != "leaf" || node.Id is null) continue;
            if (options.Form is not null && node.Form != options.Form) continue;

            var cells = new List<IpxComparedCell>(compared.Count);
            for (var i = 0; i < compared.Count; i++)
            {
                cells.Add(CellOf(compared[i], indexes[i], node, byId));
            }

            for (var i = 1; i < cells.Count; i++)
            {
                cells[i].Delta = DeltaOf(cells[0], cells[i]);
            }

            var line = new IpxComparisonLine(node, DepthOf(node, byId), cells);
            if (options.OnlyDifferences && !(line.HasAnyRule && line.IsDifferent)) continue;

            lines.Add(line);
        }

        return new IpxComparison(taxonomy, compared, lines);
    }

    internal static IpxDelta DeltaOf(IpxComparedCell baseCell, IpxComparedCell other)
    {
        if (!other.HasRule) return IpxDelta.NoRule;
        if (!baseCell.HasRule) return IpxDelta.NoBase;

        var a = baseCell.Retention;
        var b = other.Retention;
        var da = a?.Disposition ?? IpxDisposition.Unclear;
        var db = b?.Disposition ?? IpxDisposition.Unclear;

        if (IsUnclear(da) || IsUnclear(db)) return IpxDelta.Unclear;
        if (da == IpxDisposition.Handover || db == IpxDisposition.Handover) return da == db ? IpxDelta.Same : IpxDelta.Differs;
        if (da == IpxDisposition.Retain && db == IpxDisposition.Retain) return IpxDelta.Same;
        if (da == IpxDisposition.Retain) return IpxDelta.Shorter;
        if (db == IpxDisposition.Retain) return IpxDelta.Longer;

        var ma = a.Months;
        var mb = b.Months;
        var ta = a.Trigger ?? IpxTrigger.Unspecified;
        var tb = b.Trigger ?? IpxTrigger.Unspecified;

        if (ma is null && mb is null) return ta == tb ? IpxDelta.Same : IpxDelta.NotComparable;
        if (ma is null || mb is null) return IpxDelta.NotComparable;
        if (ta != tb && ta != IpxTrigger.Unspecified && tb != IpxTrigger.Unspecified) return IpxDelta.NotComparable;
        if (ma == mb) return IpxDelta.Same;

        return mb < ma ? IpxDelta.Shorter : IpxDelta.Longer;
    }

    private static bool IsUnclear(IpxDisposition disposition) => disposition is IpxDisposition.Unclear or IpxDisposition.Review;

    private static Dictionary<string, List<Hit>> Index(IpxDocument document)
    {
        var index = new Dictionary<string, List<Hit>>(StringComparer.Ordinal);
        foreach (var row in document.Rows)
        {
            foreach (var mapping in row.Mappings)
            {
                var nodeId = mapping.TaxonomyNode;
                if (nodeId is null) continue;

                if (!index.TryGetValue(nodeId, out var hits))
                {
                    hits = [];
                    index[nodeId] = hits;
                }

                hits.Add(new Hit(row, mapping.Relation ?? IpxRelation.Exact));
            }
        }

        return index;
    }

    private static IpxComparedCell CellOf(IpxDocument document, Dictionary<string, List<Hit>> index, IpxNode node, Dictionary<string, IpxNode> byId)
    {
        var nodeId = node.Id;
        var inherited = false;

        if (!index.ContainsKey(nodeId))
        {
            var parent = node.Parent is not null && byId.TryGetValue(node.Parent, out var found) ? found : null;
            if (parent?.Parent is not null && index.ContainsKey(parent.Id))
            {
                nodeId = parent.Id;
                inherited = true;
            }
            else
            {
                return Empty(document);
            }
        }

        var hits = index[nodeId].Where(x => x.Row.Current && !NotRetention.Contains(x.Row.RuleType ?? DefaultRuleType)).ToList();
        if (hits.Count == 0) return Empty(document);

        var tier = hits.Min(x => x.Relation);
        hits = hits.Where(x => x.Relation == tier).ToList();

        var best = hits.OrderByDescending(x => RankOf(Representative(x.Row))).First();
        var retention = Representative(best.Row);
        var text = IpxRetentionText.Describe(retention);

        if (hits.Count > 1)
        {
            var ranked = hits.Select(x => Representative(x.Row)).Where(x => RankOf(x) >= 0).OrderBy(RankOf).ToList();
            if (ranked.Count > 1)
            {
                var low = IpxRetentionText.Describe(ranked[0]);
                var high = IpxRetentionText.Describe(ranked[^1]);
                if (low != high) text = $"{low}…{high}";
            }
        }

        var standing = NotStanding.Contains(best.Row.RuleType ?? DefaultRuleType);
        return new IpxComparedCell(document, hits.Select(x => x.Row).ToList(), best.Relation, retention, text, inherited, standing);
    }

    private static IpxComparedCell Empty(IpxDocument document) => new(document, [], null, null, "-", false, false);

    private static IpxRetention Representative(IpxRow row) => row.Retention.OrderByDescending(RankOf).FirstOrDefault();

    private static int RankOf(IpxRetention retention)
    {
        if (retention is null) return -1;

        return retention.Disposition switch
        {
            IpxDisposition.Retain => RetainRank,
            IpxDisposition.Dispose => retention.Months ?? 0,
            _ => -1
        };
    }

    private static int DepthOf(IpxNode node, Dictionary<string, IpxNode> byId)
    {
        var depth = 0;
        var current = node;
        while (current.Parent is not null && byId.TryGetValue(current.Parent, out var parent) && depth <= byId.Count)
        {
            depth++;
            current = parent;
        }

        return depth;
    }

    private sealed class Hit
    {
        public Hit(IpxRow row, IpxRelation relation)
        {
            Row = row;
            Relation = relation;
        }

        public IpxRow Row { get; }

        public IpxRelation Relation { get; }
    }
}
