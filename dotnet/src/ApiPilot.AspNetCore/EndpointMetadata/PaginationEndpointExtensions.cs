// filepath: dotnet/src/ApiPilot.AspNetCore/EndpointMetadata/PaginationEndpointExtensions.cs
// layer: EndpointMetadata | package: ApiPilot.AspNetCore | since: v0.2.0-alpha.0
// purpose: Fluent extension method that attaches the pagination filter and its overrides to an endpoint
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (static class with extension method)
//   Depends on : PaginationOverrides, PaginationEndpointFilter, PaginationOptions
//   Used by    : application endpoint setup code
//   See also   : PaginationEndpointFilter.cs, PaginationOverrides.cs
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.Configuration;
using ApiPilot.Core.Pagination;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ApiPilot.AspNetCore.EndpointMetadata;

/// <summary>
/// Fluent extension method that attaches the ApiPilot pagination
/// endpoint filter to a minimal API endpoint and applies per-endpoint
/// overrides on top of the global PaginationOptions.
/// </summary>
public static class PaginationEndpointExtensions
{
    /// <summary>
    /// Attaches the pagination filter to the endpoint and applies the
    /// supplied overrides on top of the global configuration.
    /// </summary>
    /// <typeparam name="TBuilder">The endpoint convention builder type.</typeparam>
    /// <param name="builder">The endpoint builder. Must not be null.</param>
    /// <param name="configure">
    /// Optional callback that sets the per-endpoint overrides. When null,
    /// the endpoint uses the global PaginationOptions unchanged.
    /// </param>
    /// <returns>The same builder, for method chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="builder"/> is null.
    /// </exception>
    public static TBuilder WithApiPilotPagination<TBuilder>(
        this TBuilder builder,
        Action<PaginationOverrides>? configure = null)
        where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        var overrides = new PaginationOverrides();
        configure?.Invoke(overrides);

        builder.WithMetadata(overrides);
        builder.AddEndpointFilterFactory((filterFactoryContext, next) =>
        {
            var options = filterFactoryContext.ApplicationServices
                .GetRequiredService<IOptions<PaginationOptions>>();
            var filter = new PaginationEndpointFilter(options);
            return invocationContext => filter.InvokeAsync(invocationContext, next);
        });

        return builder;
    }
}

