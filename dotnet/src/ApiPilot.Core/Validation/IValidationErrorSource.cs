// filepath: dotnet/src/ApiPilot.Core/Validation/IValidationErrorSource.cs
// layer: Validation | package: ApiPilot.Core | since: v0.1.0-alpha.0
// purpose: Framework-neutral contract for supplying validation field errors
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (interface)
//   Depends on : ApiErrorField
//   Used by    : ASP.NET Core model validation adapter (Phase 1.4), application validators
//   See also   : ApiErrorField.cs, ValidationErrorCollection.cs, ValidationResponseFactory.cs
// -----------------------------------------------------------------------------

using ApiPilot.Core.Errors;

namespace ApiPilot.Core.Validation;

/// <summary>
/// A framework-neutral source of validation field errors. Implementations
/// translate the results of an application validator (ASP.NET Core model
/// state, a third-party validation framework, custom logic) into the ApiPilot error contract
/// without depending on any specific validation framework.
/// </summary>
/// <remarks>
/// The source returns a flat list of field errors. Aggregation and
/// deduplication into a ValidationErrorCollection happens at the boundary,
/// not inside the source. The source must never echo raw input values back
/// in its messages; only field names and safe messages belong in the
/// returned data.
/// </remarks>
public interface IValidationErrorSource
{
    /// <summary>
    /// Produces the validation field errors from this source. The returned
    /// list may be empty. It must not contain null entries.
    /// </summary>
    /// <returns>The validation field errors, or an empty list when none.</returns>
    IReadOnlyList<ApiErrorField> GetErrors();
}

