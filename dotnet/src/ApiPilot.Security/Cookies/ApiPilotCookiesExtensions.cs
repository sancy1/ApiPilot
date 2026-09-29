// filepath: dotnet/src/ApiPilot.Security/Cookies/ApiPilotCookiesExtensions.cs
// layer: Cookies | package: ApiPilot.Security | since: v0.3.0
// purpose: Service collection extensions that register the cookie profile options and validator
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (static class with extension methods)
//   Depends on : CookieProfileOptions, CookieProfileOptionsValidator, IServiceCollection
//   Used by    : application setup code in Program.cs
//   See also   : CookieProfileOptions.cs, Configuration/CookieProfileOptionsValidator.cs
// -----------------------------------------------------------------------------

using ApiPilot.Security.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace ApiPilot.Security.Cookies;

/// <summary>
/// Service collection extensions that register the cookie profile
/// options and the startup validator.
/// </summary>
/// <remarks>
/// The CookieProfileOptions are registered through the standard
/// options pipeline so that ValidateOnStart runs the validator at
/// host startup. An invalid profile fails the host.
///
/// The library never sets a cookie. The options are a declaration of
/// the profiles the application intends to use; the application
/// applies them to its own cookie configuration.
/// </remarks>
public static class ApiPilotCookiesExtensions
{
    /// <summary>
    /// Registers the cookie profile options and the startup validator.
    /// </summary>
    /// <param name="services">The service collection. Must not be null.</param>
    /// <param name="configure">
    /// Optional callback that adjusts the default profiles. May be null.
    /// </param>
    /// <returns>The same service collection, for method chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> is null.
    /// </exception>
    public static IServiceCollection AddApiPilotCookies(
        this IServiceCollection services,
        Action<CookieProfileOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var optionsBuilder = services.AddOptions<CookieProfileOptions>();
        if (configure is not null)
        {
            optionsBuilder.Configure(configure);
        }
        optionsBuilder.ValidateOnStart();

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<
                IValidateOptions<CookieProfileOptions>,
                CookieProfileOptionsValidator>());

        return services;
    }
}

