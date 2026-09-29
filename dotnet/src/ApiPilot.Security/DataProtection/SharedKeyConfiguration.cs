// filepath: dotnet/src/ApiPilot.Security/DataProtection/SharedKeyConfiguration.cs
// layer: DataProtection | package: ApiPilot.Security | since: v0.3.0
// purpose: Apply the effective ApiPilot Data Protection options to an existing IDataProtectionBuilder
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (static class)
//   Depends on : ApiPilotDataProtectionOptions, IDataProtectionBuilder
//   Used by    : ApiPilotDataProtectionExtensions
//   See also   : ApiPilotDataProtectionOptions.cs, ApiPilotDataProtectionExtensions.cs
// -----------------------------------------------------------------------------

using Microsoft.AspNetCore.DataProtection;

namespace ApiPilot.Security.DataProtection;

/// <summary>
/// Applies the effective ApiPilot Data Protection options to an
/// existing IDataProtectionBuilder. The helper does not add the
/// Data Protection services itself; the application is the authority
/// for that. The helper augments the application's configuration with
/// the ApiPilot options when the corresponding property is set.
/// </summary>
/// <remarks>
/// The helper preserves the application's application name and key
/// lifetime unless ApiPilotDataProtectionOptions explicitly overrides them.
/// The KeyStorage delegate is invoked when set; the helper then
/// records on the options instance that the delegate ran. The helper
/// does not inspect the resulting configuration and cannot verify
/// that the delegate configured a persistent store.
/// </remarks>
public static class SharedKeyConfiguration
{
    /// <summary>
    /// Applies the effective options to the builder.
    /// </summary>
    /// <param name="builder">
    /// The Data Protection builder supplied by the application. Must not
    /// be null.
    /// </param>
    /// <param name="options">
    /// The effective options. Must not be null.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="builder"/> or
    /// <paramref name="options"/> is null.
    /// </exception>
    public static void Apply(
        IDataProtectionBuilder builder,
        ApiPilotDataProtectionOptions options)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(options);

        if (!string.IsNullOrEmpty(options.ApplicationName))
        {
            builder.SetApplicationName(options.ApplicationName);
        }

        if (options.KeyLifetime is { } lifetime)
        {
            builder.SetDefaultKeyLifetime(lifetime);
        }

        if (options.KeyStorage is { } configureStorage)
        {
            configureStorage(builder);
            options.KeyStorageConfigured = true;
        }
    }
}

