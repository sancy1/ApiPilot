// filepath: dotnet/src/ApiPilot.AspNetCore/Validation/ApiPilotValidationEndpointFilter.cs
// layer: Validation | package: ApiPilot.AspNetCore | since: v0.2.0-alpha.0
// purpose: Minimal API endpoint filter and registration extensions for ApiPilot validation
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : Microsoft.AspNetCore.Http.IEndpointFilter
//   Depends on : ApiPilotValidationOptions, ErrorResponseResult, ValidationResponseFactory,
//                FieldKeyNormalizer, IOptions, EndpointFilterFactoryContext, CorrelationOptions
//   Used by    : attached to endpoints via RouteHandlerBuilder.WithApiPilotValidation
//   See also   : ApiPilotValidationFilter.cs, Configuration/ApiPilotValidationOptions.cs
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.Configuration;
using ApiPilot.AspNetCore.Results;
using ApiPilot.Core.Errors;
using ApiPilot.Core.Metadata;
using ApiPilot.Core.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ApiPilot.AspNetCore.Validation;

/// <summary>
/// A minimal API endpoint filter that runs an application-supplied validation
/// callback before the endpoint handler. If the callback returns one or more
/// field errors, the filter short-circuits the pipeline and writes a standard
/// VALIDATION_ERROR envelope. If the callback returns an empty list, the
/// handler runs normally.
/// </summary>
/// <remarks>
/// Minimal APIs do not populate ModelState, so the filter relies on the
/// application to supply validation. The callback receives the HttpContext
/// and returns the field errors, or an empty list when the request is valid.
/// </remarks>
public sealed class ApiPilotValidationEndpointFilter : IEndpointFilter
{
    private readonly Func<HttpContext, IReadOnlyList<ApiErrorField>> _validate;

    /// <summary>Creates a filter with the given validation callback.</summary>
    /// <param name="validate">
    /// The validation callback. Must not be null. It must return an empty
    /// list when the request is valid, or one or more field errors otherwise.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="validate"/> is null.
    /// </exception>
    public ApiPilotValidationEndpointFilter(
        Func<HttpContext, IReadOnlyList<ApiErrorField>> validate)
    {
        ArgumentNullException.ThrowIfNull(validate);
        _validate = validate;
    }

    /// <inheritdoc />
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var fields = _validate(context.HttpContext);

        if (fields is not null && fields.Count > 0)
        {
            var correlationOptions = context.HttpContext.RequestServices
                .GetService<IOptions<CorrelationOptions>>()?.Value;
            var echoInBody = correlationOptions?.EchoInResponseBody ?? true;
            var requestId = echoInBody ? context.HttpContext.TraceIdentifier : string.Empty;
            var meta = new ResponseMetadata { RequestId = requestId };
            var response = ValidationResponseFactory.FromFields(fields, meta);
            return new ErrorResponseResult(response);
        }

        return await next(context);
    }
}

/// <summary>
/// Extension methods that attach the ApiPilot validation filter to a
/// minimal API route handler. Two forms are provided: a raw callback that
/// returns ApiErrorField values directly, and a convenience callback that
/// returns raw key/message pairs and is normalized using the configured
/// KeyTransform from ApiPilotValidationOptions.
/// </summary>
public static class ApiPilotValidationEndpointExtensions
{
    /// <summary>
    /// Attaches a validation filter using a callback that returns ApiErrorField
    /// values. The callback receives the HttpContext and returns field errors,
    /// or an empty list when the request is valid. Key transformation, if any,
    /// is the responsibility of the callback.
    /// </summary>
    public static TBuilder WithApiPilotValidation<TBuilder>(
        this TBuilder builder,
        Func<HttpContext, IReadOnlyList<ApiErrorField>> validate)
        where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(validate);
        return builder.AddEndpointFilter(new ApiPilotValidationEndpointFilter(validate));
    }

    /// <summary>
    /// Attaches a validation filter using a callback that returns raw key to
    /// messages pairs. The keys are normalized using the KeyTransform
    /// configured in ApiPilotValidationOptions, falling back to
    /// FieldKeyNormalizer.Normalize when no transform is configured.
    /// </summary>
    public static TBuilder WithApiPilotValidation<TBuilder>(
        this TBuilder builder,
        Func<HttpContext, IReadOnlyDictionary<string, IReadOnlyList<string>>> validate)
        where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(validate);

        return builder.AddEndpointFilterFactory((filterFactoryContext, next) =>
        {
            var options = filterFactoryContext.ApplicationServices
                .GetRequiredService<IOptions<ApiPilotValidationOptions>>()
                .Value;
            var keyTransform = options.KeyTransform ?? FieldKeyNormalizer.Normalize;

            Func<HttpContext, IReadOnlyList<ApiErrorField>> wrapped = httpContext =>
            {
                var raw = validate(httpContext);
                if (raw is null || raw.Count == 0)
                {
                    return Array.Empty<ApiErrorField>();
                }

                var fields = new List<ApiErrorField>(raw.Count);
                foreach (var pair in raw)
                {
                    var key = keyTransform(pair.Key);
                    fields.Add(ApiErrorField.Create(key, pair.Value));
                }
                return fields;
            };

            var filter = new ApiPilotValidationEndpointFilter(wrapped);
            return invocationContext => filter.InvokeAsync(invocationContext, next);
        });
    }
}

