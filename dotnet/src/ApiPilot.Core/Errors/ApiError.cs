// filepath: dotnet/src/ApiPilot.Core/Errors/ApiError.cs
// layer: Errors | package: ApiPilot.Core | since: v0.1.0-alpha.0
// purpose: Immutable error object carrying a code, safe message, and optional field errors
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (sealed record)
//   Depends on : ApiErrorCode, ApiErrorField
//   Used by    : ErrorResponse, IApiErrorMapper, HTTP adapter (Phase 1)
//   See also   : SPEC.md (error envelope), ApiErrorField.cs, ApiErrorCode.cs
// -----------------------------------------------------------------------------

using System.Text.Json.Serialization;

namespace ApiPilot.Core.Errors;

/// <summary>
/// An immutable error object carrying a machine-readable code, a safe
/// human-readable message, and an optional dictionary of field-level
/// errors for validation failures.
/// </summary>
/// <remarks>
/// The message is a safe summary intended for the client. It must never
/// contain stack traces, cryptographic details, internal implementation
/// notes, or sensitive data. The type cannot enforce this mechanically;
/// the discipline is documented and reviewed.
/// </remarks>
public sealed record ApiError
{
    /// <summary>The machine-readable error code.</summary>
    public ApiErrorCode Code { get; }

    /// <summary>
    /// A short, safe, human-readable summary of the error. Always non-null
    /// and non-empty.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Optional field-level errors, present only for validation failures.
    /// Keyed by field name; each value is the array of messages for that
    /// field. Null when the error has no fields. This shape matches the
    /// wire contract in SPEC.md.
    /// </summary>
    /// <remarks>
    /// The ApiError.Fields converter preserves field-key casing verbatim during
    /// JSON serialization and deserialization.
    /// </remarks>
    [JsonConverter(typeof(ApiErrorFieldsConverter))]
    public IReadOnlyDictionary<string, IReadOnlyList<string>>? Fields { get; }

    private ApiError(
        ApiErrorCode code,
        string message,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? fields)
    {
        Code = code;
        Message = message;
        Fields = fields;
    }

    /// <summary>
    /// Creates an error without field-level details.
    /// </summary>
    /// <param name="code">The error code. Must not be null.</param>
    /// <param name="message">The safe message. Must be non-null and non-empty.</param>
    /// <returns>A new error.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="code"/> or <paramref name="message"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the message is empty or whitespace.
    /// </exception>
    public static ApiError Create(ApiErrorCode code, string message)
    {
        return Create(code, message, null);
    }

    /// <summary>
    /// Creates an error, optionally carrying field-level details. The input
    /// field values are copied into a read-only dictionary keyed by field
    /// name; the value for each key is the array of messages. Duplicate
    /// field names are rejected.
    /// </summary>
    /// <param name="code">The error code. Must not be null.</param>
    /// <param name="message">The safe message. Must be non-null and non-empty.</param>
    /// <param name="fields">
    /// Field-level errors. May be null. If non-null, each entry must be
    /// non-null and field names must be unique.
    /// </param>
    /// <returns>A new error.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="code"/> or <paramref name="message"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the message is empty, when a field entry is null, or
    /// when two field entries share the same name.
    /// </exception>
    public static ApiError Create(
        ApiErrorCode code,
        string message,
        IEnumerable<ApiErrorField>? fields)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(message);

        var trimmed = message.Trim();
        if (trimmed.Length == 0)
        {
            throw new ArgumentException("Error message must not be empty or whitespace.", nameof(message));
        }

        IReadOnlyDictionary<string, IReadOnlyList<string>>? fieldDict = null;
        if (fields is not null)
        {
            var dict = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
            foreach (var f in fields)
            {
                if (f is null)
                {
                    throw new ArgumentException("Field entries must not be null.", nameof(fields));
                }
                if (!dict.TryAdd(f.Field, f.Messages))
                {
                    throw new ArgumentException(
                        $"Duplicate field name in error: {f.Field}.",
                        nameof(fields));
                }
            }
            if (dict.Count > 0)
            {
                fieldDict = dict;
            }
        }

        return new ApiError(code, trimmed, fieldDict);
    }
}

