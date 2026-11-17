namespace Eplicta.Ipx.Validation;

/// <summary>Validates an IPX document in the order the specification sets out: first the JSON Schema, then the rules a schema cannot express. The second step runs only on a document that passed the first.</summary>
public static class IpxValidator
{
    public static IpxValidationResult Validate(IpxDocument document, IpxValidationOptions options = null)
    {
        ArgumentNullException.ThrowIfNull(document);

        var schemaErrors = IpxSchema.Check(document.Node);
        if (schemaErrors.Count > 0) return new IpxValidationResult(schemaErrors, []);

        return IpxSemanticRules.Check(document.Node, options);
    }
}
