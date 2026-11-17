using Eplicta.Ipx.Comparison;

namespace Eplicta.Ipx.Tests;

public class ComparerTests
{
    private static IpxDocument Taxonomy()
    {
        var builder = new IpxBuilder(IpxKind.Taxonomy).SetName("en", "Tree").SetLanguage("en");
        builder.AddNode("root", n => n.NodeKind = "group");
        builder.AddNode("top", n =>
        {
            n.NodeKind = "group";
            n.Parent = "root";
        });
        builder.AddNode("leaf-a", n => Leaf(n, "top", IpxForm.CompulsorySchool));
        builder.AddNode("leaf-b", n => Leaf(n, "top", IpxForm.CompulsorySchool));
        builder.AddNode("deep", n =>
        {
            n.NodeKind = "group";
            n.Parent = "top";
        });
        builder.AddNode("leaf-deep", n => Leaf(n, "deep", IpxForm.Preschool));
        builder.AddNode("leaf-top", n => Leaf(n, "root", IpxForm.Preschool));
        return builder.Build();
    }

    private static void Leaf(IpxNode node, string parent, IpxForm form)
    {
        node.NodeKind = "leaf";
        node.Parent = parent;
        node.Form = form;
    }

    private static IpxRow Row(string id, string node, IpxRetention retention, IpxRelation relation = IpxRelation.Exact, string ruleType = null, bool current = true)
    {
        var row = new IpxRow { Id = id, RuleType = ruleType, Current = current };
        row.Forms.Add(IpxForm.Shared);
        row.Retention.Add(retention);
        row.Mappings.Add(new IpxMapping { TaxonomyNode = node, Relation = relation });
        return row;
    }

    private static IpxDocument Plan(string name, params IpxRow[] rows)
    {
        var builder = new IpxBuilder(IpxKind.Plan).SetName("en", name).SetLanguage("en");
        foreach (var row in rows)
        {
            builder.AddRow(row);
        }

        return builder.Build();
    }

    private static IpxRetention Years(double years, IpxTrigger trigger = IpxTrigger.CaseClosed) => IpxRetention.Dispose(years, IpxPeriodUnit.Year, trigger);

    private static IpxComparedCell CellAt(IpxComparison comparison, string nodeId, int document) => comparison.Lines.Single(x => x.Node.Id == nodeId).Cells[document];

    private static IpxDelta? DeltaOf(IpxRetention baseRetention, IpxRetention otherRetention)
    {
        var comparison = IpxComparer.Compare(Taxonomy(), [Plan("base", Row("b", "leaf-a", baseRetention)), Plan("other", Row("o", "leaf-a", otherRetention))]);
        return CellAt(comparison, "leaf-a", 1).Delta;
    }

    [Fact]
    public void Only_leaves_are_compared_and_each_line_has_a_cell_per_document()
    {
        var comparison = IpxComparer.Compare(Taxonomy(), [Plan("base"), Plan("other")]);

        Assert.Equal(["leaf-a", "leaf-b", "leaf-deep", "leaf-top"], comparison.Lines.Select(x => x.Node.Id));
        Assert.All(comparison.Lines, x => Assert.Equal(2, x.Cells.Count));
        Assert.Equal(2, comparison.Documents.Count);
    }

    [Fact]
    public void The_depth_is_the_number_of_ancestors()
    {
        var comparison = IpxComparer.Compare(Taxonomy(), [Plan("base")]);

        Assert.Equal(2, comparison.Lines.Single(x => x.Node.Id == "leaf-a").Depth);
        Assert.Equal(3, comparison.Lines.Single(x => x.Node.Id == "leaf-deep").Depth);
        Assert.Equal(1, comparison.Lines.Single(x => x.Node.Id == "leaf-top").Depth);
    }

    [Fact]
    public void The_base_has_no_delta()
    {
        var comparison = IpxComparer.Compare(Taxonomy(), [Plan("base", Row("b", "leaf-a", Years(6))), Plan("other", Row("o", "leaf-a", Years(6)))]);

        Assert.Null(CellAt(comparison, "leaf-a", 0).Delta);
        Assert.Equal(IpxDelta.Same, CellAt(comparison, "leaf-a", 1).Delta);
    }

    public static IEnumerable<object[]> Deltas()
    {
        yield return Case("the same period from the same trigger", Years(6), Years(6), IpxDelta.Same);
        yield return Case("a shorter period", Years(6), Years(2), IpxDelta.Shorter);
        yield return Case("a longer period", Years(2), Years(6), IpxDelta.Longer);
        yield return Case("periods from different triggers", Years(6), Years(6, IpxTrigger.YearEnd), IpxDelta.NotComparable);
        yield return Case("a period from an unspecified trigger against a named one", Years(6), Years(6, IpxTrigger.Unspecified), IpxDelta.Same);
        yield return Case("a retain against a disposal", IpxRetention.Retain(), Years(6), IpxDelta.Shorter);
        yield return Case("a disposal against a retain", Years(6), IpxRetention.Retain(), IpxDelta.Longer);
        yield return Case("two retains", IpxRetention.Retain(), IpxRetention.Retain(), IpxDelta.Same);
        yield return Case("an unclear base", new IpxRetention { Disposition = IpxDisposition.Unclear }, Years(6), IpxDelta.Unclear);
        yield return Case("an unclear other", Years(6), new IpxRetention { Disposition = IpxDisposition.Unclear }, IpxDelta.Unclear);
        yield return Case("a review entry", Years(6), new IpxRetention { Disposition = IpxDisposition.Review }, IpxDelta.Unclear);
        yield return Case("two handovers", IpxRetention.Handover(10, IpxPeriodUnit.Year, IpxTrigger.YearEnd), IpxRetention.Handover(5, IpxPeriodUnit.Year, IpxTrigger.YearEnd), IpxDelta.Same);
        yield return Case("a handover against a disposal", IpxRetention.Handover(10, IpxPeriodUnit.Year, IpxTrigger.YearEnd), Years(6), IpxDelta.Differs);
        yield return Case("no period on either, same trigger", IpxRetention.Dispose(IpxTrigger.Obsolete), IpxRetention.Dispose(IpxTrigger.Obsolete), IpxDelta.Same);
        yield return Case("no period on either, different triggers", IpxRetention.Dispose(IpxTrigger.Obsolete), IpxRetention.Dispose(IpxTrigger.Superseded), IpxDelta.NotComparable);
        yield return Case("a period on one only", Years(6), IpxRetention.Dispose(IpxTrigger.Obsolete), IpxDelta.NotComparable);
    }

    private static object[] Case(string name, IpxRetention baseRetention, IpxRetention otherRetention, IpxDelta expected) => [name, baseRetention, otherRetention, expected];

    [Theory]
    [MemberData(nameof(Deltas))]
    public void A_cell_is_compared_with_the_base(string name, IpxRetention baseRetention, IpxRetention otherRetention, IpxDelta expected)
    {
        Assert.True(expected == DeltaOf(baseRetention, otherRetention), name);
    }

    [Fact]
    public void A_document_without_a_rule_at_a_node_has_no_rule_and_a_base_without_one_has_no_base()
    {
        var comparison = IpxComparer.Compare(Taxonomy(), [Plan("base", Row("b", "leaf-a", Years(6))), Plan("other", Row("o", "leaf-b", Years(6)))]);

        Assert.Equal(IpxDelta.NoRule, CellAt(comparison, "leaf-a", 1).Delta);
        Assert.Equal(IpxDelta.NoBase, CellAt(comparison, "leaf-b", 1).Delta);
        Assert.False(CellAt(comparison, "leaf-a", 1).HasRule);
        Assert.Equal("-", CellAt(comparison, "leaf-a", 1).Text);
    }

    [Fact]
    public void The_closest_relation_wins_and_the_cell_says_which()
    {
        var plan = Plan("base", Row("wide", "leaf-a", Years(10), IpxRelation.Covers), Row("exact", "leaf-a", Years(2)), Row("near", "leaf-a", Years(30), IpxRelation.Similar));

        var cell = CellAt(IpxComparer.Compare(Taxonomy(), [plan]), "leaf-a", 0);

        Assert.Equal(IpxRelation.Exact, cell.Relation);
        Assert.Equal(["exact"], cell.Rows.Select(x => x.Id));
        Assert.Equal("2y/case-closed", cell.Text);
    }

    [Fact]
    public void A_mapping_without_a_relation_is_exact()
    {
        var row = new IpxRow { Id = "r" };
        row.Forms.Add(IpxForm.Shared);
        row.Retention.Add(Years(3));
        row.Mappings.Add(new IpxMapping { TaxonomyNode = "leaf-a" });

        var cell = CellAt(IpxComparer.Compare(Taxonomy(), [Plan("base", row)]), "leaf-a", 0);

        Assert.Equal(IpxRelation.Exact, cell.Relation);
    }

    [Fact]
    public void Several_rows_at_the_same_relation_show_a_range_and_the_longest_keeping_one_stands_for_the_cell()
    {
        var plan = Plan("base", Row("short", "leaf-a", Years(2)), Row("long", "leaf-a", Years(6)));

        var cell = CellAt(IpxComparer.Compare(Taxonomy(), [plan]), "leaf-a", 0);

        Assert.True(cell.Multiple);
        Assert.Equal("2y/case-closed…6y/case-closed", cell.Text);
        Assert.Equal(72, cell.Retention.Months);
    }

    [Fact]
    public void Several_rows_that_say_the_same_show_no_range()
    {
        var plan = Plan("base", Row("one", "leaf-a", Years(6)), Row("two", "leaf-a", Years(6)));

        var cell = CellAt(IpxComparer.Compare(Taxonomy(), [plan]), "leaf-a", 0);

        Assert.True(cell.Multiple);
        Assert.Equal("6y/case-closed", cell.Text);
    }

    [Fact]
    public void A_row_that_has_several_retention_entries_stands_for_the_longest_keeping_one()
    {
        var row = Row("r", "leaf-a", Years(2));
        row.Retention.Add(Years(6));

        var cell = CellAt(IpxComparer.Compare(Taxonomy(), [Plan("base", row)]), "leaf-a", 0);

        Assert.Equal(72, cell.Retention.Months);
    }

    [Theory]
    [InlineData("instruction")]
    [InlineData("promise")]
    [InlineData("placeholder")]
    [InlineData("general-principle")]
    [InlineData("recommendation-quote")]
    public void A_row_that_is_not_a_retention_rule_does_not_count(string ruleType)
    {
        var plan = Plan("base", Row("r", "leaf-a", Years(6), ruleType: ruleType));

        Assert.False(CellAt(IpxComparer.Compare(Taxonomy(), [plan]), "leaf-a", 0).HasRule);
    }

    [Fact]
    public void A_row_that_is_no_longer_current_does_not_count()
    {
        var plan = Plan("base", Row("r", "leaf-a", Years(6), current: false));

        Assert.False(CellAt(IpxComparer.Compare(Taxonomy(), [plan]), "leaf-a", 0).HasRule);
    }

    [Theory]
    [InlineData("one-off-decision", true)]
    [InlineData("gdpr-limit", true)]
    [InlineData("platform-statement", true)]
    [InlineData("plan-rule", false)]
    [InlineData(null, false)]
    public void A_row_that_is_not_a_standing_rule_is_marked(string ruleType, bool expected)
    {
        var plan = Plan("base", Row("r", "leaf-a", Years(6), ruleType: ruleType));

        Assert.Equal(expected, CellAt(IpxComparer.Compare(Taxonomy(), [plan]), "leaf-a", 0).NotAStandingRule);
    }

    [Fact]
    public void A_node_without_a_row_takes_the_rule_of_its_parent_when_the_parent_is_not_a_root()
    {
        var plan = Plan("base", Row("r", "deep", Years(6)));

        var cell = CellAt(IpxComparer.Compare(Taxonomy(), [plan]), "leaf-deep", 0);

        Assert.True(cell.Inherited);
        Assert.True(cell.HasRule);
        Assert.Equal("6y/case-closed", cell.Text);
    }

    [Fact]
    public void A_node_does_not_take_the_rule_of_a_root()
    {
        var plan = Plan("base", Row("r", "root", Years(6)));

        Assert.False(CellAt(IpxComparer.Compare(Taxonomy(), [plan]), "leaf-top", 0).HasRule);
    }

    [Fact]
    public void A_node_with_a_row_of_its_own_does_not_inherit()
    {
        var plan = Plan("base", Row("up", "deep", Years(6)), Row("own", "leaf-deep", Years(2)));

        var cell = CellAt(IpxComparer.Compare(Taxonomy(), [plan]), "leaf-deep", 0);

        Assert.False(cell.Inherited);
        Assert.Equal("2y/case-closed", cell.Text);
    }

    [Fact]
    public void Only_differences_leaves_out_lines_that_are_alike_or_empty()
    {
        var baseline = Plan("base", Row("b1", "leaf-a", Years(6)), Row("b2", "leaf-b", Years(6)));
        var other = Plan("other", Row("o1", "leaf-a", Years(6)), Row("o2", "leaf-b", Years(2)));

        var comparison = IpxComparer.Compare(Taxonomy(), [baseline, other], new IpxCompareOptions { OnlyDifferences = true });

        Assert.Equal(["leaf-b"], comparison.Lines.Select(x => x.Node.Id));
    }

    [Fact]
    public void A_form_limits_the_lines()
    {
        var comparison = IpxComparer.Compare(Taxonomy(), [Plan("base")], new IpxCompareOptions { Form = IpxForm.Preschool });

        Assert.Equal(["leaf-deep", "leaf-top"], comparison.Lines.Select(x => x.Node.Id));
    }

    [Fact]
    public void A_line_says_whether_it_has_a_rule_and_whether_it_differs()
    {
        var comparison = IpxComparer.Compare(Taxonomy(), [Plan("base", Row("b", "leaf-a", Years(6))), Plan("other", Row("o", "leaf-a", Years(2)))]);

        var line = comparison.Lines.Single(x => x.Node.Id == "leaf-a");
        var empty = comparison.Lines.Single(x => x.Node.Id == "leaf-b");

        Assert.True(line.HasAnyRule);
        Assert.True(line.IsDifferent);
        Assert.False(empty.HasAnyRule);
    }

    [Fact]
    public void Comparing_needs_a_base()
    {
        Assert.Throws<ArgumentException>(() => IpxComparer.Compare(Taxonomy(), []));
        Assert.Throws<ArgumentNullException>(() => IpxComparer.Compare(null, [Plan("base")]));
    }

    [Theory]
    [InlineData(IpxDisposition.Retain, null, null, "retain")]
    [InlineData(IpxDisposition.Handover, null, null, "handover")]
    [InlineData(IpxDisposition.Unclear, null, null, "?")]
    [InlineData(IpxDisposition.Review, null, null, "?")]
    [InlineData(IpxDisposition.Dispose, null, null, "dispose")]
    [InlineData(IpxDisposition.Dispose, null, IpxTrigger.Obsolete, "obsolete")]
    [InlineData(IpxDisposition.Dispose, null, IpxTrigger.Superseded, "at/superseded")]
    [InlineData(IpxDisposition.Dispose, 0, IpxTrigger.EnrolmentEnd, "at/enrolment-end")]
    [InlineData(IpxDisposition.Dispose, 72, IpxTrigger.CaseClosed, "6y/case-closed")]
    [InlineData(IpxDisposition.Dispose, 30, null, "30m")]
    [InlineData(IpxDisposition.Dispose, 24, IpxTrigger.Unspecified, "2y")]
    [InlineData(IpxDisposition.Dispose, 24, IpxTrigger.None, "2y")]
    public void A_retention_entry_is_described_in_a_few_characters(IpxDisposition disposition, int? months, IpxTrigger? trigger, string expected)
    {
        var entry = new IpxRetention { Disposition = disposition, Months = months, Trigger = trigger };

        Assert.Equal(expected, IpxRetentionText.Describe(entry));
    }

    [Fact]
    public void No_entry_is_described_as_a_dash()
    {
        Assert.Equal("-", IpxRetentionText.Describe(null));
    }
}
