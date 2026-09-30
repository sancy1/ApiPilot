// filepath: dotnet/tests/ApiPilot.AspNetCore.Tests/Integration/MinimalApiValidationKeyTransformTests.cs
// layer: Tests | package: ApiPilot.AspNetCore.Tests | since: v1.0.4
// purpose: Regression tests for the KeyTransform identity override (F-59)
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class + MVC controller under test)
//   Depends on : InProcessHost, TestAssert, TestClassAttribute, TestAttribute,
//                ApiPilot.AspNetCore.DependencyInjection,
//                ApiPilot.AspNetCore.Serialization,
//                ApiPilot.AspNetCore.Validation,
//                Microsoft.AspNetCore.Builder, Microsoft.AspNetCore.Mvc,
//                Microsoft.Extensions.DependencyInjection, System.Net.Http
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : ApiPilotValidationEndpointFilter.cs, ValidationKeyTransforms.cs
// -----------------------------------------------------------------------------
//
// F-59 regression boundary.
//
// The minimal-API convenience overload resolves the effective key transform
// as: options.KeyTransform ?? FieldKeyNormalizer.Normalize. It does not call
// ValidationKeyTransforms.Resolve, which is the documented single source of
// truth for the resolution rule. The expressions are semantically identical
// today; the missing Resolve call is a consistency and drift hazard, and the
// test records the documented contract behavior on the wire.
//
// Three tests:
//   1. Identity transform on the convenience overload preserves the raw key.
//   2. Null transform on the convenience overload falls back to camelCase.
//   3. Identity transform on the MVC filter path preserves the raw key,
//      establishing a cross-pipeline parity reference at the HTTP level.
//
// All assertions read the serialized response body: error.fields keys on the
// wire. Not in-memory objects.
//
// The endpoints return a plain string, not Results.Ok, so the unqualified
// identifier does not bind to the test project Results namespace. This is
// the A-116 namespace-shadow avoidance.
// -----------------------------------------------------------------------------

using System.Net.Http;
using System.Text;
using System.Text.Json;
using ApiPilot.AspNetCore.DependencyInjection;
using ApiPilot.AspNetCore.Serialization;
using ApiPilot.AspNetCore.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace ApiPilot.AspNetCore.Tests.Integration;

/// <summary>
/// Regression tests for the KeyTransform identity override (F-59).
/// </summary>
[TestClass]
public sealed class MinimalApiValidationKeyTransformTests
{
    private static async Task<InProcessHost> StartMinimalApiHostAsync(
        bool configureIdentity)
    {
        return await InProcessHost.StartAsync(
            configureServices: builder =>
            {
                builder.Services.AddApiPilotJson();
                if (configureIdentity)
                {
                    builder.Services.AddApiPilotValidation(o => o.KeyTransform = key => key);
                }
                else
                {
                    builder.Services.AddApiPilotValidation();
                }
            },
            configureApp: app =>
            {
                app.UseRouting();
                app.MapPost("/validate-raw", () => "ok")
                   .WithApiPilotValidation(_ =>
                       new Dictionary<string, IReadOnlyList<string>>
                       {
                           ["Email"] = new[] { "Email is required." }
                       });
            });
    }

    private static async Task<InProcessHost> StartMvcHostAsync()
    {
        return await InProcessHost.StartAsync(
            configureServices: builder =>
            {
                builder.Services.AddApiPilotJson();
                builder.Services.AddApiPilotValidation(o => o.KeyTransform = key => key);
                builder.Services.AddControllers()
                    .AddApplicationPart(typeof(MinimalApiValidationKeyTransformTests).Assembly);
            },
            configureApp: app =>
            {
                app.UseRouting();
                app.MapControllers();
            });
    }

    private static HttpContent EmptyJson()
    {
        return new StringContent("{}", Encoding.UTF8, "application/json");
    }

    /// <summary>
    /// Identity transform on the minimal-API convenience overload must
    /// preserve the raw key "Email" on the wire.
    /// </summary>
    [Test]
    public async Task IdentityTransform_ConvenienceOverload_PreservesRawKey()
    {
        await using var host = await StartMinimalApiHostAsync(configureIdentity: true);
        var response = await host.Client.PostAsync("/validate-raw", EmptyJson());
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var fields = doc.RootElement.GetProperty("error").GetProperty("fields");
        TestAssert.True(fields.TryGetProperty("Email", out _));
        TestAssert.False(fields.TryGetProperty("email", out _));
    }

    /// <summary>
    /// Null transform on the minimal-API convenience overload must fall back
    /// to the camelCase normalizer.
    /// </summary>
    [Test]
    public async Task NullTransform_ConvenienceOverload_NormalizesToCamelCase()
    {
        await using var host = await StartMinimalApiHostAsync(configureIdentity: false);
        var response = await host.Client.PostAsync("/validate-raw", EmptyJson());
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var fields = doc.RootElement.GetProperty("error").GetProperty("fields");
        TestAssert.True(fields.TryGetProperty("email", out _));
        TestAssert.False(fields.TryGetProperty("Email", out _));
    }

    /// <summary>
    /// Identity transform on the MVC [ApiPilotValidate] path must preserve the
    /// raw binder key on the wire. Cross-pipeline parity reference.
    /// </summary>
    [Test]
    public async Task IdentityTransform_MvcFilterPath_PreservesRawKey()
    {
        await using var host = await StartMvcHostAsync();
        var response = await host.Client.PostAsync("/api/keytransform-sample", EmptyJson());
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var fields = doc.RootElement.GetProperty("error").GetProperty("fields");
        TestAssert.True(fields.TryGetProperty("Email", out _));
        TestAssert.False(fields.TryGetProperty("email", out _));
    }
}

/// <summary>Request DTO whose binder key is "Email".</summary>
public sealed class KeyTransformSampleRequest
{
    /// <summary>A required value so ModelState produces the "Email" key.</summary>
    [System.ComponentModel.DataAnnotations.Required]
    public string? Email { get; set; }
}

/// <summary>Controller that exercises the MVC [ApiPilotValidate] path.</summary>
[Route("api/keytransform-sample")]
public sealed class KeyTransformSampleController : ControllerBase
{
    /// <summary>Returns 200 on a valid body, or the validation envelope.</summary>
    [HttpPost]
    [ApiPilotValidate]
    public IActionResult Post([FromBody] KeyTransformSampleRequest request)
    {
        return Ok(new { received = request.Email });
    }
}

