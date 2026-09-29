// filepath: dotnet/src/ApiPilot.AspNetCore/DependencyInjection/ApiPilotServiceCollectionExtensions.cs
// layer: DependencyInjection | package: ApiPilot.AspNetCore | since: v0.2.0-alpha.0
// purpose: IServiceCollection extensions that configure ApiPilot validation options
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (static class with extension method)
//   Depends on : ApiPilotValidationOptions, IServiceCollection
//   Used by    : application startup code in Program.cs
//   See also   : Configuration/ApiPilotValidationOptions.cs, Validation/ApiPilotValidationFilter.cs
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.Configuration;
using ApiPilot.AspNetCore.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace ApiPilot.AspNetCore.DependencyInjection;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> that configure
/// ApiPilot validation options. Calling AddApiPilotValidation is optional:
/// the [ApiPilotValidate] attribute works without any call because the
/// framework supplies the options infrastructure in MVC hosts. Call this
/// method only to customize the options.
/// </summary>
public static class ApiPilotServiceCollectionExtensions
{
    /// <summary>
    /// Returns a fluent builder that orchestrates the per-concern
    /// ApiPilot registration extensions. Calling this method alone
    /// registers nothing; each concern is activated explicitly
    /// through a Configure* call on the returned builder.
    /// </summary>
    /// <param name="services">The service collection. Must not be null.</param>
    /// <returns>
    /// A builder whose Configure* methods delegate to the per-concern
    /// AddApiPilot* extensions.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> is null.
    /// </exception>
    public static ApiPilotBuilder AddApiPilot(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return new ApiPilotBuilder(services);
    }

    /// <summary>
    /// Registers the ApiPilot validation options. When <paramref name="configure"/>
    /// is non-null, it is applied to configure the options. When null, this
    /// method only ensures the options type is registered with the DI container
    /// so that any host can resolve it.
    /// </summary>
    /// <param name="services">The service collection. Must not be null.</param>
    /// <param name="configure">Optional callback to configure the options. May be null.</param>
    /// <returns>The same service collection, for method chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> is null.
    /// </exception>
    public static IServiceCollection AddApiPilotValidation(
        this IServiceCollection services,
        Action<ApiPilotValidationOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<ApiPilotValidationOptions>();

        if (configure is not null)
        {
            services.Configure(configure);
        }

        return services;
    }

    /// <summary>
    /// Wires the ApiPilot validation envelope into MVC for controllers marked
    /// with [ApiController]. Replaces the default ProblemDetails response with
    /// the ApiPilot validation wire shape. Call this after AddControllers.
    /// Calling this is optional: controllers without [ApiController] use the
    /// [ApiPilotValidate] filter instead and do not need this registration.
    /// </summary>
    /// <param name="services">The service collection. Must not be null.</param>
    /// <param name="configure">Optional callback to configure the options. May be null.</param>
    /// <returns>The same service collection, for method chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> is null.
    /// </exception>
    public static IServiceCollection AddApiPilotControllers(
        this IServiceCollection services,
        Action<ApiPilotValidationOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddApiPilotValidation(configure);

        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = ApiPilotInvalidModelStateResponseFactory.Create;
        });

        return services;
    }

    /// <summary>
    /// Registers the ApiPilot pagination options. Call this to customize
    /// the global PaginationOptions. Calling this is optional: the
    /// [WithApiPilotPagination] fluent extension and the
    /// [PaginationMetadata] attribute both work without any call,
    /// because ASP.NET Core supplies the options infrastructure.
    /// </summary>
    /// <param name="services">The service collection. Must not be null.</param>
    /// <param name="configure">Optional callback to configure the options. May be null.</param>
    /// <returns>The same service collection, for method chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> is null.
    /// </exception>
    public static IServiceCollection AddApiPilotPagination(
        this IServiceCollection services,
        Action<ApiPilot.Core.Pagination.PaginationOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var optionsBuilder = services.AddOptions<ApiPilot.Core.Pagination.PaginationOptions>();
        if (configure is not null)
        {
            optionsBuilder.Configure(configure);
        }
        optionsBuilder.ValidateOnStart();

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<
                IValidateOptions<ApiPilot.Core.Pagination.PaginationOptions>,
                PaginationOptionsValidator>());

        return services;
    }
}

