// filepath: dotnet/src/ApiPilot.Security/DataProtection/ApiPilotDataProtectionOptions.cs
// layer: DataProtection | package: ApiPilot.Security | since: v0.3.0
// purpose: Multi-instance and key-storage configuration for the ApiPilot Data Protection layer
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (sealed class)
//   Depends on : Microsoft.AspNetCore.DataProtection
//   Used by    : ApiPilotDataProtectionExtensions, InstanceSafetyValidator, DevelopmentOnlyWarning
//   See also   : ApiPilotDataProtectionExtensions.cs, InstanceSafetyValidator.cs, SPEC.md (Data Protection)
// -----------------------------------------------------------------------------

using Microsoft.AspNetCore.DataProtection;

namespace ApiPilot.Security.DataProtection;

/// <summary>
/// Configuration for the ApiPilot Data Protection layer. Separate from
/// CsrfTokenOptions: this type governs how the Data Protection keys
/// are stored and shared; CsrfTokenOptions governs the CSRF token
/// lifetime and payload.
/// </summary>
/// <remarks>
/// The library does not own the Data Protection configuration. It
/// augments the application's existing configuration through the
/// KeyStorage delegate. The MultiInstance flag declares the
/// application's intent; the startup validator checks that the
/// declaration is accompanied by a KeyStorage configuration. The
/// library cannot verify that the delegate configured a persistent
/// store; the contract requires the application to do so.
///
/// KeyLifetime is not the CSRF token lifetime. Rotating a Data
/// Protection key does not invalidate an existing CSRF token unless
/// the Data Protection configuration itself revokes the old key.
/// See CsrfTokenOptions.TokenLifetime for the token lifetime.
/// </remarks>
public sealed class ApiPilotDataProtectionOptions
{
    /// <summary>
    /// The Data Protection application name. Defaults to null, which
    /// means the framework default (the entry assembly name) is used.
    /// Two instances of an application must use the same application
    /// name to validate each other's protected payloads.
    /// </summary>
    public string? ApplicationName { get; set; }

    /// <summary>
    /// The Data Protection key lifetime. Defaults to null, which means
    /// the framework default (90 days) is used. This is NOT the CSRF
    /// token lifetime. See CsrfTokenOptions.TokenLifetime for that.
    /// </summary>
    public TimeSpan? KeyLifetime { get; set; }

    /// <summary>
    /// Whether the application runs multiple instances behind a load
    /// balancer. Defaults to false. When true, the application must
    /// configure a shared or persistent key ring through the KeyStorage
    /// delegate; otherwise startup fails with a configuration error.
    /// </summary>
    public bool MultiInstance { get; set; }

    /// <summary>
    /// The delegate that configures the application's Data Protection
    /// builder for shared or persistent key storage. Defaults to null.
    /// The delegate receives the IDataProtectionBuilder; the application
    /// calls PersistKeysToFileSystem, PersistKeysToDbContext, or any
    /// other persistence method it needs.
    /// </summary>
    public Action<IDataProtectionBuilder>? KeyStorage { get; set; }

    /// <summary>
    /// Set by the ApiPilot extension when the KeyStorage delegate is
    /// invoked. This records that the delegate ran; it does not record
    /// whether the delegate configured a persistent store. The startup
    /// validator uses this flag to enforce the documented contract:
    /// when MultiInstance is true, the application must have supplied a
    /// KeyStorage delegate through the approved ApiPilot registration
    /// path.
    /// </summary>
    internal bool KeyStorageConfigured { get; set; }
}

