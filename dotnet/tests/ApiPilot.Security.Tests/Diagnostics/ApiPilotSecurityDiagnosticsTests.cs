// filepath: dotnet/tests/ApiPilot.Security.Tests/Diagnostics/ApiPilotSecurityDiagnosticsTests.cs
// layer: Tests | package: ApiPilot.Security.Tests | since: v0.3.0
// purpose: Contract tests for ApiPilotSecurityDiagnostics and SecurityConfigurationValidator
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute,
//                ApiPilot.Security.Diagnostics, ApiPilot.Security.DataProtection,
//                Microsoft.Extensions.DependencyInjection
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : ApiPilotSecurityDiagnostics.cs, SecurityConfigurationValidator.cs
// -----------------------------------------------------------------------------

using ApiPilot.Security.Configuration;
using ApiPilot.Security.Cookies;
using ApiPilot.Security.Csrf;
using ApiPilot.Security.DataProtection;
using ApiPilot.Security.Diagnostics;
using ApiPilot.Security.Origin;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ApiPilot.Security.Tests.Diagnostics;

/// <summary>
/// Contract tests for the diagnostics surface and the registration
/// checker.
/// </summary>
[TestClass]
public sealed class ApiPilotSecurityDiagnosticsTests
{
    /// <summary>The in-memory key ring produces the InMemoryKeyRing warning.</summary>
    [Test]
    public void Diagnostics_InMemoryKeyRing_ProducesWarning()
    {
        var options = new ApiPilotDataProtectionOptions();
        var diagnostics = new ApiPilotSecurityDiagnostics(options);
        var list = diagnostics.GetDiagnostics();
        TestAssert.Equal(1, list.Count);
        TestAssert.Equal(DiagnosticCodes.InMemoryKeyRing, list[0].Code);
        TestAssert.Equal(SecurityDiagnosticLevel.Warning, list[0].Level);
    }

    /// <summary>MultiInstance=true suppresses the warning.</summary>
    [Test]
    public void Diagnostics_MultiInstance_NoWarning()
    {
        var options = new ApiPilotDataProtectionOptions { MultiInstance = true, KeyStorageConfigured = true };
        var diagnostics = new ApiPilotSecurityDiagnostics(options);
        var list = diagnostics.GetDiagnostics();
        TestAssert.Equal(0, list.Count);
    }

    /// <summary>KeyStorageConfigured=true suppresses the warning.</summary>
    [Test]
    public void Diagnostics_KeyStorageConfigured_NoWarning()
    {
        var options = new ApiPilotDataProtectionOptions { KeyStorageConfigured = true };
        var diagnostics = new ApiPilotSecurityDiagnostics(options);
        var list = diagnostics.GetDiagnostics();
        TestAssert.Equal(0, list.Count);
    }

    /// <summary>Emit calls the sink once per diagnostic.</summary>
    [Test]
    public void Diagnostics_Emit_InvokesSink()
    {
        var options = new ApiPilotDataProtectionOptions();
        var diagnostics = new ApiPilotSecurityDiagnostics(options);
        var count = 0;
        diagnostics.Emit(_ => count++);
        TestAssert.Equal(1, count);
    }

    /// <summary>A fully-configured container has no missing validators.</summary>
    [Test]
    public void RegistrationCheck_FullyConfigured_NoFatalDiagnostics()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IValidateOptions<CsrfOptions>, CsrfOptionsValidator>();
        services.AddSingleton<IValidateOptions<CookieProfileOptions>, CookieProfileOptionsValidator>();
        services.AddSingleton<IValidateOptions<OriginPolicyOptions>, OriginPolicyOptionsValidator>();
        services.AddSingleton<IValidateOptions<FetchMetadataOptions>, FetchMetadataOptionsValidator>();
        services.AddSingleton<IValidateOptions<ApiPilotDataProtectionOptions>, InstanceSafetyValidator>();
        var provider = services.BuildServiceProvider();

        var checker = new SecurityConfigurationValidator(provider);
        var list = checker.GetDiagnostics();
        TestAssert.Equal(0, list.Count);
    }

    /// <summary>A missing validator produces a fatal diagnostic.</summary>
    [Test]
    public void RegistrationCheck_MissingValidator_ProducesFatalDiagnostic()
    {
        var services = new ServiceCollection();
        // Deliberately do not register any validators.
        var provider = services.BuildServiceProvider();

        var checker = new SecurityConfigurationValidator(provider);
        var list = checker.GetDiagnostics();
        TestAssert.Equal(5, list.Count);
        foreach (var d in list)
        {
            TestAssert.Equal(SecurityDiagnosticLevel.Fatal, d.Level);
            TestAssert.Equal(DiagnosticCodes.ValidatorMissing, d.Code);
        }
    }
}

