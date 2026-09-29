// filepath: dotnet/src/ApiPilot.AspNetCore/ExceptionHandling/DefaultApiExceptionMapper.cs
// layer: ExceptionHandling | package: ApiPilot.AspNetCore | since: v0.2.0-alpha.0
// purpose: Default IApiExceptionMapper implementation using ApiExceptionOptions
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : IApiExceptionMapper
//   Depends on : ApiPilot.Core.Errors, ApiExceptionOptions, KnownExceptionType
//   Used by    : ApiPilotExceptionMiddleware
//   See also   : IApiExceptionMapper.cs, KnownExceptionTypes.cs, ApiExceptionOptions.cs
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.Configuration;
using ApiPilot.Core.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace ApiPilot.AspNetCore.ExceptionHandling;

/// <summary>
/// The default <see cref="IApiExceptionMapper"/>. Walks the configured
/// mapping list in order, matches the first entry whose exception type is
/// an instance of the thrown exception, and produces an <see cref="ApiError"/>
/// using either the entry&#39;s safe message or the exception&#39;s own message.
/// Exceptions with no matching entry fall back to a generic internal error
/// unless the options explicitly reveal the exception type or message.
/// </summary>
public sealed class DefaultApiExceptionMapper : IApiExceptionMapper
{
    private const string GenericInternalErrorMessage = "An unexpected error occurred.";

    private readonly IOptions<ApiExceptionOptions> _options;

    /// <summary>
    /// Creates a mapper that uses the given options.
    /// </summary>
    /// <param name="options">The options. Must not be null.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/> is null.
    /// </exception>
    public DefaultApiExceptionMapper(IOptions<ApiExceptionOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
    }

    /// <inheritdoc />
    public ApiError Map(Exception exception, HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(httpContext);

        foreach (var mapping in _options.Value.Mappings)
        {
            if (mapping.ExceptionType.IsInstanceOfType(exception))
            {
                var message = mapping.IncludeExceptionMessage
                    ? exception.Message
                    : mapping.SafeMessage;
                return ApiError.Create(mapping.Code, message);
            }
        }

        return BuildFallback(exception);
    }

    private ApiError BuildFallback(Exception exception)
    {
        var message = _options.Value.RevealExceptionMessageInResponse
            ? exception.Message
            : GenericInternalErrorMessage;

        if (_options.Value.RevealExceptionTypeInResponse)
        {
            message = $"{message} ({exception.GetType().Name})";
        }

        return ApiError.Create(ApiErrorCode.InternalError, message);
    }
}

