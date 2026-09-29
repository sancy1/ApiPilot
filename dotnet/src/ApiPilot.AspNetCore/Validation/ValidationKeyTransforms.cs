// filepath: dotnet/src/ApiPilot.AspNetCore/Validation/ValidationKeyTransforms.cs
// layer: Validation | package: ApiPilot.AspNetCore | since: v0.2.0-alpha.0
// purpose: Single point of truth for resolving the effective key transform from options
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (static class)
//   Depends on : ApiPilotValidationOptions, FieldKeyNormalizer
//   Used by    : ApiPilotValidationFilter, ApiPilotInvalidModelStateResponseFactory,
//                ApiPilotValidationEndpointFilter (via DI resolution path)
//   See also   : Configuration/ApiPilotValidationOptions.cs, FieldKeyNormalizer.cs
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.Configuration;

namespace ApiPilot.AspNetCore.Validation;

/// <summary>
/// Resolves the effective field key transform from ApiPilotValidationOptions.
/// This is the single source of truth for the precedence rule:
/// the configured KeyTransform when present, otherwise the default
/// FieldKeyNormalizer.Normalize. Every ApiPilot validation entry point
/// resolves the transform through this helper so that the two paths
/// (action filter and [ApiController] response factory) cannot drift.
/// </summary>
internal static class ValidationKeyTransforms
{
    /// <summary>
    /// Returns the effective key transform for the given options.
    /// </summary>
    /// <param name="options">The validation options. Must not be null.</param>
    /// <returns>The key transform to apply to field keys.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/> is null.
    /// </exception>
    public static Func<string, string> Resolve(ApiPilotValidationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return options.KeyTransform ?? FieldKeyNormalizer.Normalize;
    }
}

