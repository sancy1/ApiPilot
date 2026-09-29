// filepath: dotnet/src/ApiPilot.AspNetCore/ExceptionHandling/KnownExceptionTypes.cs
// layer: ExceptionHandling | package: ApiPilot.AspNetCore | since: v0.2.0-alpha.0
// purpose: Built-in mapping of common exception types to ApiErrorCode values
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (static class with mapping type)
//   Depends on : ApiPilot.Core.Errors.ApiErrorCode
//   Used by    : DefaultApiExceptionMapper, ApiExceptionOptions
//   See also   : IApiExceptionMapper.cs, ApiExceptionOptions.cs, SPEC.md
// -----------------------------------------------------------------------------

using ApiPilot.Core.Errors;

namespace ApiPilot.AspNetCore.ExceptionHandling;

/// <summary>
/// Describes a mapping from a CLR exception type to an <see cref="ApiErrorCode"/>
/// and a safe default message. When <see cref="IncludeExceptionMessage"/> is
/// true, the exception&#39;s own message is used instead of the safe default.
/// That flag should only be true when the exception message is known to be
/// safe for client consumption.
/// </summary>
/// <param name="ExceptionType">The CLR type to match.</param>
/// <param name="Code">The error code to associate with this exception type.</param>
/// <param name="SafeMessage">A safe default message used on the wire.</param>
/// <param name="IncludeExceptionMessage">
/// Whether to use the exception message instead of the safe default.
/// </param>
public sealed record KnownExceptionType(
    Type ExceptionType,
    ApiErrorCode Code,
    string SafeMessage,
    bool IncludeExceptionMessage);

/// <summary>
/// The set of exception types that ApiPilot maps by default. Applications can
/// extend this list through <c>ApiExceptionOptions.AddMapping</c>. The order
/// of the list matters: more specific types must come before less specific
/// ones, because matching walks the list top to bottom.
/// </summary>
public static class KnownExceptionTypes
{
    /// <summary>
    /// The default mapping table. ArgumentNullException is listed before
    /// ArgumentException because the former derives from the latter.
    /// </summary>
    public static IReadOnlyList<KnownExceptionType> Default { get; } = new[]
    {
        new KnownExceptionType(
            typeof(ArgumentNullException),
            ApiErrorCode.ValidationError,
            "One or more required values are missing.",
            IncludeExceptionMessage: false),
        new KnownExceptionType(
            typeof(ArgumentException),
            ApiErrorCode.ValidationError,
            "One or more values are invalid.",
            IncludeExceptionMessage: false),
        new KnownExceptionType(
            typeof(KeyNotFoundException),
            ApiErrorCode.ResourceNotFound,
            "The requested resource was not found.",
            IncludeExceptionMessage: false),
        new KnownExceptionType(
            typeof(UnauthorizedAccessException),
            ApiErrorCode.Forbidden,
            "You are not permitted to perform this operation.",
            IncludeExceptionMessage: false),
        new KnownExceptionType(
            typeof(InvalidOperationException),
            ApiErrorCode.Conflict,
            "The operation conflicts with the current state of the resource.",
            IncludeExceptionMessage: false),
    };
}

