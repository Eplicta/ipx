namespace Eplicta.Ipx.Validation;

/// <summary>
/// Validates an IPX document in the order the specification sets out: first the JSON Schema, then the rules a schema cannot express. The second step runs only on a document that passed the first.
/// A document of a newer minor version is checked against the schema with the closed enumerations and the ban on unknown properties lifted, because a newer minor version may add both; the validator then warns that part of the document was not checked.
/// </summary>
public static class IpxValidator
{
    public static IpxValidationResult Validate(IpxDocument document, IpxValidationOptions options = null)
    {
        ArgumentNullException.ThrowIfNull(document);

        var schemaErrors = IpxSchema.Check(document.Node);
        if (schemaErrors.Count > 0)
        {
            if (!IpxSemanticRules.IsNewerMinor(document.Node)) return new IpxValidationResult(schemaErrors, []);

            var lenientErrors = IpxSchema.CheckLenient(document.Node);
            if (lenientErrors.Count > 0) return new IpxValidationResult(lenientErrors, []);
        }

        return IpxSemanticRules.Check(document.Node, options);
    }
}
