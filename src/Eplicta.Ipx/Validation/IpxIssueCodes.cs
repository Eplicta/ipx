namespace Eplicta.Ipx.Validation;

/// <summary>The codes an <see cref="IpxIssue"/> can have. They are part of the conformance corpus and do not change within a major version.</summary>
public static class IpxIssueCodes
{
    /// <summary>Error: a structural error found by the JSON Schema.</summary>
    public const string Schema = "schema";

    /// <summary>Error: two items in one list have the same id.</summary>
    public const string DuplicateId = "duplicate-id";

    /// <summary>Error: an id refers to something that does not exist.</summary>
    public const string MissingReference = "missing-reference";

    /// <summary>Error: the parent chain of a node loops.</summary>
    public const string ParentLoop = "parent-loop";

    /// <summary>Error: a taxonomy document has rows.</summary>
    public const string TaxonomyHasRows = "taxonomy-has-rows";

    /// <summary>Error: a date is not a real calendar date, such as 2026-02-30 or 2026-13.</summary>
    public const string InvalidDate = "invalid-date";

    /// <summary>Warning: the months of a retention entry are not the period converted to months.</summary>
    public const string MonthsMismatch = "months-mismatch";

    /// <summary>Warning: a retain entry has a period.</summary>
    public const string RetainWithPeriod = "retain-with-period";

    /// <summary>Warning: a mapping points at a node that is not in the taxonomy that was given.</summary>
    public const string MappingNotInTaxonomy = "mapping-not-in-taxonomy";

    /// <summary>Warning: a plan hands topics to a plan that is not listed.</summary>
    public const string DelegatesToUnknownPlan = "delegates-to-unknown-plan";

    /// <summary>Warning: the document is of a newer minor version than the validator knows, so properties it added are not checked.</summary>
    public const string NewerMinorVersion = "newer-minor-version";
}
