// filepath: dotnet/src/ApiPilot.Security/DataProtection/ApiPilotDataProtectionExtensions.cs
// layer: DataProtection | package: ApiPilot.Security | since: v0.3.0
// purpose: Service collection extension that registers and applies the ApiPilot Data Protection configuration
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (static class with extension methods)
//   Depends on : ApiPilotDataProtectionOptions, SharedKeyConfiguration, InstanceSafetyValidator,
//                IDataProtectionBuilder, IOptions, IServiceCollection
//   Used by    : application setup code in Program.cs
//   See also   : ApiPilotDataProtectionOptions.cs, SharedKeyConfiguration.cs, InstanceSafetyValidator.cs
// -----------------------------------------------------------------------------

using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace ApiPilot.Security.DataProtection;

/// <summary>
/// Registers and applies the ApiPilot Data Protection configuration.
/// The extension augments the application's existing Data Protection
/// services; it never replaces them.
/// </summary>
/// <remarks>
/// The extension reads the effective ApiPilotDataProtectionOptions at
/// registration time and applies the KeyStorage delegate to the
/// builder before the host is built. This is the only point at which
/// the builder can be configured. The options instance is registered
/// as both a concrete singleton and behind IOptions so the startup
/// validator sees the same instance.
///
/// The extension preserves the application's existing Data Protection
/// configuration. It calls services.AddDataProtection() only when the
/// services are not already registered. It does not replace the
/// application's provider, application name, or key lifetime unless
/// ApiPilotDataProtectionOptions explicitly sets the corresponding property.
/// </remarks>
public static class ApiPilotDataProtectionExtensions
{
    /// <summary>
    /// Registers the ApiPilot Data Protection configuration.
    /// </summary>
    /// <param name="services">The service collection. Must not be null.</param>
    /// <param name="configure">
    /// Optional callback that configures the Data Protection options.
    /// May be null.
    /// </param>
    /// <returns>The same service collection, for method chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> is null.
    /// </exception>
    public static IServiceCollection AddApiPilotDataProtection(
        this IServiceCollection services,
        Action<ApiPilotDataProtectionOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Build the concrete options instance. The configure delegate
        // runs now, before the Data Protection builder is configured.
        var options = new ApiPilotDataProtectionOptions();
        configure?.Invoke(options);

        // Ensure Data Protection services exist without replacing an
        // application-supplied configuration. TryAdd semantics.
        var builder = services.AddDataProtection();

        // Apply the effective options to the builder. This invokes the
        // application's KeyStorage delegate and records that the
        // delegate ran.
        SharedKeyConfiguration.Apply(builder, options);

        // Register the concrete options instance as a singleton so the
        // validator and any other consumer see the same instance that
        // the builder was configured with.
        services.AddSingleton(options);

        // Register IOptions<ApiPilotDataProtectionOptions> to resolve to the
        // same instance. This is the OptionsWrapper bridge pattern: the
        // delegate has already run, so the options pipeline must not
        // construct a fresh default.
        services.AddSingleton<IOptions<ApiPilotDataProtectionOptions>>(
            new OptionsWrapper<ApiPilotDataProtectionOptions>(options));

        // Register the validator. It sees the same instance through
        // IOptions.
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<
                IValidateOptions<ApiPilotDataProtectionOptions>,
                InstanceSafetyValidator>());


        // Register the development-only warning. The warning is emitted
        // when the application resolves the options and the in-memory
        // key ring is in use. The emission is deferred to a callback so
        // the ILogger is available.
        services.TryAddSingleton<IDataProtectionStartupDiagnostics>(sp =>
            new DataProtectionStartupDiagnostics(
                sp.GetRequiredService<ApiPilotDataProtectionOptions>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<DataProtectionStartupDiagnostics>>(),
                sp.GetServices<IValidateOptions<ApiPilotDataProtectionOptions>>()));

        // Register the hosted service that emits the diagnostics at
        // host startup. TryAddEnumerable ensures one registration.
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<
                Microsoft.Extensions.Hosting.IHostedService,
                DataProtectionStartupHostedService>());

        return services;
    }
}

/// <summary>
/// A marker interface for the startup diagnostics component. The
/// diagnostics component is resolved at startup to emit the
/// development-only warning when applicable.
/// </summary>
public interface IDataProtectionStartupDiagnostics
{
    /// <summary>
    /// Emits the startup diagnostics. Called once at host startup.
    /// </summary>
    void Emit();
}

/// <summary>
/// The default startup diagnostics. Emits the development-only warning
/// when the application has not configured persistent key storage.
/// </summary>
internal sealed class DataProtectionStartupDiagnostics : IDataProtectionStartupDiagnostics
{
    private readonly ApiPilotDataProtectionOptions _options;
    private readonly Microsoft.Extensions.Logging.ILogger<DataProtectionStartupDiagnostics> _logger;

    private readonly IEnumerable<IValidateOptions<ApiPilotDataProtectionOptions>> _validators;

    /// <summary>Creates the diagnostics component.</summary>
    /// <param name="options">The effective options. Must not be null.</param>
    /// <param name="logger">The logger. Must not be null.</param>
    /// <param name="validators">
    /// The registered options validators, run at startup. Must not be null.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when an argument is null.
    /// </exception>
    public DataProtectionStartupDiagnostics(
        ApiPilotDataProtectionOptions options,
        Microsoft.Extensions.Logging.ILogger<DataProtectionStartupDiagnostics> logger,
        IEnumerable<IValidateOptions<ApiPilotDataProtectionOptions>> validators)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options;
        _logger = logger;
        _validators = validators;
    }

    /// <inheritdoc />
    public void Emit()
    {
        var failures = new List<string>();
        foreach (var validator in _validators)
        {
            var result = validator.Validate(null, _options);
            if (result.Failed && result.Failures is not null)
            {
                foreach (var failure in result.Failures)
                {
                    failures.Add(failure);
                }
            }
        }
        if (failures.Count > 0)
        {
            throw new OptionsValidationException(
                nameof(ApiPilotDataProtectionOptions),
                typeof(ApiPilotDataProtectionOptions),
                failures);
        }

        // The warning is appropriate only when the in-memory key ring
        // is in use: MultiInstance is false and no KeyStorage was
        // configured. When MultiInstance is true, the startup validator
        // already enforces the contract; the warning is redundant.
        if (_options.MultiInstance)
        {
            return;
        }

        if (_options.KeyStorageConfigured)
        {
            return;
        }

        DevelopmentOnlyWarning.WarnOnce(_logger);
    }
}

