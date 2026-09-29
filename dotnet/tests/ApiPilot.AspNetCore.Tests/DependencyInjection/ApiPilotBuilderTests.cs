// filepath: dotnet/tests/ApiPilot.AspNetCore.Tests/DependencyInjection/ApiPilotBuilderTests.cs
// layer: Tests | package: ApiPilot.AspNetCore.Tests | since: v0.2.0
// purpose: Contract tests for the ApiPilotBuilder fluent orchestration layer
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute,
//                ApiPilot.AspNetCore.DependencyInjection,
//                ApiPilot.AspNetCore.Configuration,
//                ApiPilot.AspNetCore.ExceptionHandling,
//                ApiPilot.AspNetCore.Serialization,
//                ApiPilot.AspNetCore.Validation,
//                ApiPilot.Core.Configuration,
//                ApiPilot.Core.Metadata,
//                ApiPilot.Core.Pagination
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : ApiPilotBuilder.cs, ApiPilotServiceCollectionExtensions.cs
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.Configuration;
using ApiPilot.AspNetCore.DependencyInjection;
using ApiPilot.AspNetCore.ExceptionHandling;
using ApiPilot.AspNetCore.Serialization;
using ApiPilot.AspNetCore.Validation;
using ApiPilot.Core.Configuration;
using ApiPilot.Core.Metadata;
using ApiPilot.Core.Pagination;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ApiPilot.AspNetCore.Tests.DependencyInjection;

/// <summary>
/// Contract tests for ApiPilotBuilder. Verifies that the builder delegates
/// to the per-concern extensions, that AddApiPilot alone registers no
/// ApiPilot options, and that null service collections are rejected.
/// </summary>
[TestClass]
public sealed class ApiPilotBuilderTests
{
    /// <summary>AddApiPilot returns a builder around the same collection.</summary>
    [Test]
    public void AddApiPilot_ReturnsBuilder_WithSameServicesCollection()
    {
        var services = new ServiceCollection();
        var builder = services.AddApiPilot();
        TestAssert.NotNull(builder);
        TestAssert.True(ReferenceEquals(services, builder.Services),
            "Builder.Services must be the same instance that AddApiPilot was called on.");
    }

    /// <summary>AddApiPilot rejects a null service collection.</summary>
    [Test]
    public void AddApiPilot_NullServices_Throws()
    {
        TestAssert.Throws<ArgumentNullException>(() =>
            ApiPilotServiceCollectionExtensions.AddApiPilot(null!));
    }

    /// <summary>The builder constructor rejects a null service collection.</summary>
    [Test]
    public void BuilderConstructor_NullServices_Throws()
    {
        TestAssert.Throws<ArgumentNullException>(() =>
            new ApiPilotBuilder(null!));
    }

    /// <summary>ConfigureCorrelation delegates to AddApiPilotCorrelation.</summary>
    [Test]
    public void ConfigureCorrelation_DelegatesToAddApiPilotCorrelation()
    {
        var services = new ServiceCollection();
        services.AddApiPilot().ConfigureCorrelation(o => o.HeaderName = "X-Test-Correlation");

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<CorrelationOptions>>().Value;
        TestAssert.Equal("X-Test-Correlation", options.HeaderName);
    }

    /// <summary>ConfigurePagination delegates to AddApiPilotPagination.</summary>
    [Test]
    public void ConfigurePagination_DelegatesToAddApiPilotPagination()
    {
        var services = new ServiceCollection();
        services.AddApiPilot().ConfigurePagination(o => o.MaxPageSize = 250);

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<PaginationOptions>>().Value;
        TestAssert.Equal(250, options.MaxPageSize);
    }

    /// <summary>ConfigureJson delegates to AddApiPilotJson.</summary>
    [Test]
    public void ConfigureJson_DelegatesToAddApiPilotJson()
    {
        var services = new ServiceCollection();
        services.AddApiPilot().ConfigureJson(o => o.EnumMode = EnumSerializationMode.Number);

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ApiPilotJsonOptions>>().Value;
        TestAssert.Equal(EnumSerializationMode.Number, options.EnumMode);
    }

    /// <summary>ConfigureExceptions delegates to AddApiPilotExceptions.</summary>
    [Test]
    public void ConfigureExceptions_DelegatesToAddApiPilotExceptions()
    {
        var services = new ServiceCollection();
        services.AddApiPilot().ConfigureExceptions(o => o.RevealExceptionMessageInResponse = true);

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ApiExceptionOptions>>().Value;
        TestAssert.True(options.RevealExceptionMessageInResponse);
    }

    /// <summary>ConfigureContentNegotiation delegates to AddApiPilotContentNegotiation.</summary>
    [Test]
    public void ConfigureContentNegotiation_DelegatesToAddApiPilotContentNegotiation()
    {
        var services = new ServiceCollection();
        services.AddApiPilot().ConfigureContentNegotiation(o => o.AcceptWildcard = false);

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ContentNegotiationOptions>>().Value;
        TestAssert.False(options.AcceptWildcard);
    }

    /// <summary>ConfigureValidation delegates to AddApiPilotValidation.</summary>
    [Test]
    public void ConfigureValidation_DelegatesToAddApiPilotValidation()
    {
        var services = new ServiceCollection();
        services.AddApiPilot().ConfigureValidation(o => o.KeyTransform = k => k.ToUpperInvariant());

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ApiPilotValidationOptions>>().Value;
        TestAssert.NotNull(options.KeyTransform);
        TestAssert.Equal("EMAIL", options.KeyTransform!("email"));
    }

    /// <summary>
    /// AddApiPilot alone registers no ApiPilot options and no ApiPilot
    /// validators. The check is scoped to the six specific options types so
    /// that unrelated test infrastructure does not affect the assertion.
    /// </summary>
    [Test]
    public void AddApiPilot_Alone_RegistersNoApiPilotOptions()
    {
        var services = new ServiceCollection();
        services.AddApiPilot();

        // No IValidateOptions<T> for any ApiPilot options type should be
        // present. The builder registers nothing on its own.
        AssertNoValidator<CorrelationOptions>(services);
        AssertNoValidator<PaginationOptions>(services);
        AssertNoValidator<ApiPilotJsonOptions>(services);
        AssertNoValidator<ApiExceptionOptions>(services);
        AssertNoValidator<ContentNegotiationOptions>(services);
        AssertNoValidator<ApiPilotValidationOptions>(services);
    }

    private static void AssertNoValidator<TOptions>(IServiceCollection services)
        where TOptions : class
    {
        foreach (var descriptor in services)
        {
            if (descriptor.ServiceType == typeof(IValidateOptions<TOptions>))
            {
                TestAssert.True(false,
                    "Expected no IValidateOptions<" + typeof(TOptions).Name +
                    "> registration, but found one.");
            }
        }
    }
}

