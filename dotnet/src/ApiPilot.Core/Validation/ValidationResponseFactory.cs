// filepath: dotnet/src/ApiPilot.Core/Validation/ValidationResponseFactory.cs
// layer: Validation | package: ApiPilot.Core | since: v0.1.0-alpha.0
// purpose: Builds standard validation ErrorResponse envelopes from field errors
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (static class)
//   Depends on : ApiError, ApiErrorCode, ApiErrorField, ErrorResponse, ResponseMetadata,
//                ValidationErrors, IValidationErrorSource
//   Used by    : ASP.NET Core model validation adapter (Phase 1.4), application endpoints
//   See also   : ValidationErrors.cs, IValidationErrorSource.cs, SPEC.md
// -----------------------------------------------------------------------------

using ApiPilot.Core.Errors;
using ApiPilot.Core.Metadata;

namespace ApiPilot.Core.Validation;

/// <summary>
/// Builds standard validation error responses. The factory aggregates the
/// supplied field errors into a deterministic ValidationErrors,
/// wraps them in an ApiError with code VALIDATION_ERROR, and produces an
/// ErrorResponse with the supplied metadata.
/// </summary>
/// <remarks>
/// The factory never echoes raw input values. Messages are supplied by the
/// caller and are the caller&#39;s responsibility to keep safe. The factory
/// rejects empty field sets: a validation error response must carry at
/// least one field error to be meaningful.
/// </remarks>
public static class ValidationResponseFactory
{
    /// <summary>The default message used when the caller does not supply one.</summary>
    public const string DefaultMessage = "One or more values are invalid.";

    /// <summary>
    /// Builds a validation error response from a collection of field errors.
    /// </summary>
    /// <param name="fields">
    /// The field errors. Must not be null or empty. Null entries are rejected.
    /// </param>
    /// <param name="meta">The response metadata. Must not be null.</param>
    /// <param name="message">
    /// An optional human-readable summary. When null or whitespace,
    /// <see cref="DefaultMessage"/> is used.
    /// </param>
    /// <returns>A standard ErrorResponse with code VALIDATION_ERROR.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="fields"/> or <paramref name="meta"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="fields"/> is empty or contains a null entry.
    /// </exception>
    public static ErrorResponse FromFields(
        IEnumerable<ApiErrorField> fields,
        ResponseMetadata meta,
        string? message = null)
    {
        ArgumentNullException.ThrowIfNull(fields);
        ArgumentNullException.ThrowIfNull(meta);

        var collection = ValidationErrors.From(fields);
        if (collection.IsEmpty)
        {
            throw new ArgumentException(
                "A validation error response requires at least one field error.",
                nameof(fields));
        }

        var effectiveMessage = string.IsNullOrWhiteSpace(message)
            ? DefaultMessage
            : message.Trim();

        var error = ApiError.Create(
            ApiErrorCode.ValidationError,
            effectiveMessage,
            collection.Fields);

        return new ErrorResponse(error, meta);
    }

    /// <summary>
    /// Builds a validation error response from a validation error source.
    /// </summary>
    /// <param name="source">
    /// The source that supplies the field errors. Must not be null. The
    /// source&#39;s GetErrors result must be non-empty.
    /// </param>
    /// <param name="meta">The response metadata. Must not be null.</param>
    /// <param name="message">
    /// An optional human-readable summary. When null or whitespace,
    /// <see cref="DefaultMessage"/> is used.
    /// </param>
    /// <returns>A standard ErrorResponse with code VALIDATION_ERROR.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source"/> or <paramref name="meta"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the source produces no field errors, or when it contains
    /// a null entry.
    /// </exception>
    public static ErrorResponse FromSource(
        IValidationErrorSource source,
        ResponseMetadata meta,
        string? message = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        var errors = source.GetErrors();
        ArgumentNullException.ThrowIfNull(errors);
        return FromFields(errors, meta, message);
    }
}

