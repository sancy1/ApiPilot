// filepath: dotnet/src/ApiPilot.AspNetCore/DependencyInjection/ApiPilotBuilder.cs
// layer: DependencyInjection | package: ApiPilot.AspNetCore | since: v0.2.0
// purpose: Fluent builder that delegates ApiPilot concern registration to the existing per-concern extensions
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (sealed builder)
//   Depends on : ApiPilotServiceCollectionExtensions (the AddApiPilot* per-concern extensions)
//   Used by    : application startup code in Program.cs
//   See also   : ApiPilotServiceCollectionExtensions.cs, docs/README.md (quick start)
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.Configuration;
using ApiPilot.AspNetCore.Middleware;
using ApiPilot.AspNetCore.RateLimiting;
using ApiPilot.AspNetCore.Serialization;
using ApiPilot.Core.Configuration;
using ApiPilot.Core.Metadata;
using ApiPilot.Core.Pagination;
using Microsoft.Extensions.DependencyInjection;

namespace ApiPilot.AspNetCore.DependencyInjection;

/// <summary>
/// A thin fluent orchestration layer over the per-concern ApiPilot
/// registration extensions. Each Configure method delegates to the
/// corresponding AddApiPilot extension and returns the same builder so
/// that calls chain.
/// </summary>
/// <remarks>
/// The builder is additive. It exists so that a full-library adoption can
/// be written as one fluent chain instead of a series of independent
/// AddApiPilot* calls. It does not introduce an aggregate options type,
/// it does not register any option or validator of its own, and it does
/// not add middleware to the pipeline.
///
/// Calling AddApiPilot on an IServiceCollection and then doing nothing
/// with the returned builder registers nothing. Every concern is
/// activated explicitly through a Configure* call. Service registration
/// and middleware activation are separate: the builder only touches the
/// service collection, and middleware is added separately through the
/// UseApiPilot* pipeline extensions.
///
/// The nullability of each Configure* parameter matches the underlying
/// extension. The extensions accept a nullable callback with a default
/// of null, so the builder does the same. Only the services collection
/// is guarded; the callback is not, because the extension does not
/// guard it either.
/// </remarks>
public sealed class ApiPilotBuilder
{
    /// <summary>
    /// Creates a builder around the given service collection. The
    /// constructor is internal because the entry point is the
    /// AddApiPilot extension method.
    /// </summary>
    /// <param name="services">The service collection. Must not be null.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> is null.
    /// </exception>
    internal ApiPilotBuilder(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        Services = services;
    }

    /// <summary>
    /// The underlying service collection. Exposed so that a caller can
    /// drop back to the IServiceCollection surface at any point in the
    /// chain.
    /// </summary>
    public IServiceCollection Services { get; }

    /// <summary>
    /// Registers the correlation options through the per-concern extension.
    /// The callback may be null, in which case the library defaults apply.
    /// </summary>
    /// <param name="configure">
    /// Optional callback to configure the correlation options. May be null.
    /// </param>
    /// <returns>The same builder, for method chaining.</returns>
    public ApiPilotBuilder ConfigureCorrelation(Action<CorrelationOptions>? configure = null)
    {
        Services.AddApiPilotCorrelation(configure);
        return this;
    }

    /// <summary>
    /// Registers the pagination options through the per-concern extension.
    /// The callback may be null, in which case the library defaults apply.
    /// </summary>
    /// <param name="configure">
    /// Optional callback to configure the pagination options. May be null.
    /// </param>
    /// <returns>The same builder, for method chaining.</returns>
    public ApiPilotBuilder ConfigurePagination(Action<PaginationOptions>? configure = null)
    {
        Services.AddApiPilotPagination(configure);
        return this;
    }

    /// <summary>
    /// Registers the JSON serialization options through the per-concern
    /// extension. The callback may be null, in which case the library
    /// defaults apply.
    /// </summary>
    /// <param name="configure">
    /// Optional callback to configure the JSON options. May be null.
    /// </param>
    /// <returns>The same builder, for method chaining.</returns>
    public ApiPilotBuilder ConfigureJson(Action<ApiPilotJsonOptions>? configure = null)
    {
        Services.AddApiPilotJson(configure);
        return this;
    }

    /// <summary>
    /// Registers the exception handling options through the per-concern
    /// extension. The callback may be null, in which case the library
    /// defaults apply.
    /// </summary>
    /// <param name="configure">
    /// Optional callback to configure the exception options. May be null.
    /// </param>
    /// <returns>The same builder, for method chaining.</returns>
    public ApiPilotBuilder ConfigureExceptions(Action<ApiExceptionOptions>? configure = null)
    {
        Services.AddApiPilotExceptions(configure);
        return this;
    }

    /// <summary>
    /// Registers the content negotiation options through the per-concern
    /// extension. The callback may be null, in which case the library
    /// defaults apply.
    /// </summary>
    /// <param name="configure">
    /// Optional callback to configure the content negotiation options.
    /// May be null.
    /// </param>
    /// <returns>The same builder, for method chaining.</returns>
    public ApiPilotBuilder ConfigureContentNegotiation(Action<ContentNegotiationOptions>? configure = null)
    {
        Services.AddApiPilotContentNegotiation(configure);
        return this;
    }

    /// <summary>
    /// Registers the validation options through the per-concern extension.
    /// The callback may be null, in which case the library defaults apply.
    /// </summary>
    /// <param name="configure">
    /// Optional callback to configure the validation options. May be null.
    /// </param>
    /// <returns>The same builder, for method chaining.</returns>
    public ApiPilotBuilder ConfigureValidation(Action<ApiPilotValidationOptions>? configure = null)
    {
        Services.AddApiPilotValidation(configure);
        return this;
    }

    /// <summary>
    /// Registers the rate-limit rejection options through the per-concern
    /// extension. The callback may be null, in which case the library
    /// defaults apply.
    /// </summary>
    /// <param name="configure">
    /// Optional callback to configure the rate-limit rejection options.
    /// May be null.
    /// </param>
    /// <returns>The same builder, for method chaining.</returns>
    public ApiPilotBuilder ConfigureRateLimitRejection(Action<ApiPilotRateLimitOptions>? configure = null)
    {
        Services.AddApiPilotRateLimitRejection(configure);
        return this;
    }
}

