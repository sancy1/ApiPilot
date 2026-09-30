// filepath: dotnet/src/ApiPilot.AspNetCore/Configuration/ApiExceptionOptions.cs
// layer: Configuration | package: ApiPilot.AspNetCore | since: v0.2.0-alpha.0
// purpose: Mutable configuration for the ApiPilot exception mapping middleware
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (sealed class)
//   Depends on : KnownExceptionType, KnownExceptionTypes, Microsoft.Extensions.Logging.LogLevel
//   Used by    : DefaultApiExceptionMapper, ApiPilotExceptionMiddleware, application setup
//   See also   : KnownExceptionTypes.cs, ApiPilotExceptionExtensions.cs
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.ExceptionHandling;
using Microsoft.Extensions.Logging;

namespace ApiPilot.AspNetCore.Configuration;

/// <summary>
/// Mutable configuration for the ApiPilot exception mapping middleware.
/// Controls which exception types map to which errors and what information
/// is logged and revealed. Defaults are safe: no exception type or message
/// is revealed to clients for unknown exceptions, and known mappings use
/// their documented safe messages.
/// </summary>
public sealed class ApiExceptionOptions
{
    /// <summary>
    /// The ordered list of exception-to-code mappings. Defaults to
    /// <see cref="KnownExceptionTypes.Default"/>. Applications can add,
    /// remove, or reorder entries. Order matters: the mapper matches the
    /// first entry whose ExceptionType is an instance of the thrown
    /// exception.
    /// </summary>
    public IList<KnownExceptionType> Mappings { get; } =
        new List<KnownExceptionType>(KnownExceptionTypes.Default);

    /// <summary>
    /// Whether to include the exception type name in log output for known
    /// mappings. Defaults to true. Type names are safe for server-side logs
    /// and useful for diagnosis.
    /// </summary>
    public bool IncludeExceptionTypeInLogs { get; set; } = true;

    /// <summary>
    /// Whether to include the exception message in log output. Defaults to
    /// true. Set to false for applications that process sensitive input where
    /// exception messages might contain that input.
    /// </summary>
    public bool IncludeExceptionMessageInLogs { get; set; } = true;

    /// <summary>
    /// The log level used when the middleware logs an exception that has a
    /// known mapping (for example, ArgumentException or KeyNotFoundException).
    /// Defaults to Warning. These exceptions are expected outcomes.
    /// </summary>
    public LogLevel KnownExceptionLogLevel { get; set; } = LogLevel.Warning;

    /// <summary>
    /// The log level used when the middleware logs an exception that has no
    /// known mapping. Defaults to Error. These exceptions are unexpected
    /// failures that warrant attention.
    /// </summary>
    public LogLevel UnknownExceptionLogLevel { get; set; } = LogLevel.Error;

    /// <summary>
    /// Whether to reveal the exception type name in the response body for
    /// unknown exceptions. Defaults to false. When true, the response body
    /// includes the exception type name in the error message. Intended only
    /// for internal APIs behind authentication. Never enable for public APIs.
    /// </summary>
    public bool RevealExceptionTypeInResponse { get; set; }

    /// <summary>
    /// Whether to reveal the exception message in the response body for
    /// unknown exceptions. Defaults to false. When true, the response body
    /// uses the exception message instead of the generic internal error
    /// message. Intended only for internal APIs behind authentication.
    /// </summary>
    public bool RevealExceptionMessageInResponse { get; set; }

    /// <summary>
    /// Optional overrides for the HTTP status code produced for a given
    /// error code. Keys are wire-format error codes (for example
    /// "VALIDATION_ERROR"); values are the HTTP status codes to use.
    /// When a code is not present in this dictionary, the built-in
    /// mapping is used; if the built-in mapping does not recognize the
    /// code either, the response falls back to 500.
    /// </summary>
    /// <remarks>
    /// This is the Option D override point for error-code-to-status
    /// mapping. Applications that define custom error codes, or that
    /// prefer a different status for a built-in code, populate this
    /// dictionary. Defaults to an empty dictionary, which means the
    /// built-in mappings are used unchanged.
    /// The declared type is <c>IReadOnlyDictionary&lt;string, int&gt;</c> with a
    /// public setter; assign a whole dictionary, do not mutate in place.
    /// </remarks>
    public IReadOnlyDictionary<string, int> ErrorCodeToStatusMap { get; set; } =
        new Dictionary<string, int>(StringComparer.Ordinal);
}

