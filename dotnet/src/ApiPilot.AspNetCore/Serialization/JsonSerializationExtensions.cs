// filepath: dotnet/src/ApiPilot.AspNetCore/Serialization/JsonSerializationExtensions.cs
// layer: Serialization | package: ApiPilot.AspNetCore | since: v0.2.0-alpha.0
// purpose: IMvcBuilder and IServiceCollection extensions that apply ApiPilot JSON conventions
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (static class with extension methods)
//   Depends on : ApiPilotJsonOptions, JsonSerializerConfigurator, IMvcBuilder,
//                MVC JsonOptions, Http.Json.JsonOptions, IServiceCollection
//   Used by    : application setup code in Program.cs
//   See also   : ApiPilotJsonOptions.cs, JsonSerializerConfigurator.cs
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace ApiPilot.AspNetCore.Serialization;

/// <summary>
/// Extension methods that configure ApiPilot JSON conventions on the
/// ASP.NET Core JSON serializers. Two overloads are provided: one for
/// applications that use MVC controllers and one for applications that
/// use minimal APIs only. Both register the ApiPilot JSON options
/// through the standard options pipeline so the configuration reaches
/// every consumer, and both configure the minimal API JSON serializer
/// because that is where HTTP responses are serialized for both models.
/// </summary>
public static class JsonSerializationExtensions
{
    /// <summary>
    /// Applies ApiPilot JSON conventions through the MVC builder. This
    /// overload registers the options through the service collection and
    /// additionally configures the MVC JSON serializer. When
    /// <paramref name="configure"/> is provided, it runs through the
    /// options pipeline so the configured instance is the one every
    /// consumer resolves.
    /// </summary>
    /// <param name="builder">The MVC builder. Must not be null.</param>
    /// <param name="configure">Optional callback to adjust defaults. May be null.</param>
    /// <returns>The same MVC builder, for method chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="builder"/> is null.
    /// </exception>
    public static IMvcBuilder AddApiPilotJson(
        this IMvcBuilder builder,
        Action<ApiPilotJsonOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Register the options through the service collection. This is
        // the single source of truth; the MVC serializer is wired below
        // to read from the pipeline, not from a throwaway instance.
        builder.Services.AddApiPilotJson(configure);

        // Wire the MVC JSON serializer to read the options from the pipeline.
        builder.Services.AddOptions<Microsoft.AspNetCore.Mvc.JsonOptions>()
            .Configure<IOptions<ApiPilotJsonOptions>>((mvcJsonOptions, apiPilotOptions) =>
            {
                JsonSerializerConfigurator.Configure(
                    mvcJsonOptions.JsonSerializerOptions,
                    apiPilotOptions.Value);
            });

        return builder;
    }

    /// <summary>
    /// Applies ApiPilot JSON conventions directly to the service collection.
    /// This overload is appropriate for applications that use only minimal
    /// APIs, which do not have an MVC builder. It registers the options
    /// through the standard options pipeline and configures the minimal API
    /// JSON serializer to read from the pipeline. When
    /// <paramref name="configure"/> is provided, it runs through the
    /// pipeline so the configured instance is the one every consumer
    /// resolves.
    /// </summary>
    /// <param name="services">The service collection. Must not be null.</param>
    /// <param name="configure">Optional callback to adjust defaults. May be null.</param>
    /// <returns>The same service collection, for method chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> is null.
    /// </exception>
    public static IServiceCollection AddApiPilotJson(
        this IServiceCollection services,
        Action<ApiPilotJsonOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var optionsBuilder = services.AddOptions<ApiPilotJsonOptions>();
        if (configure is not null)
        {
            optionsBuilder.Configure(configure);
        }
        optionsBuilder.ValidateOnStart();

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<ApiPilotJsonOptions>, ApiPilotJsonOptionsValidator>());

        // Configure the minimal API JSON serializer to read the options
        // from the pipeline. The Configure<TOptions, TDep> overload injects
        // IOptions<ApiPilotJsonOptions> into the delegate.
        services.AddOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>()
            .Configure<IOptions<ApiPilotJsonOptions>>((httpJsonOptions, apiPilotOptions) =>
            {
                JsonSerializerConfigurator.Configure(
                    httpJsonOptions.SerializerOptions,
                    apiPilotOptions.Value);
            });

        return services;
    }
}

