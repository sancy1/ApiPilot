// filepath: dotnet/tests/ApiPilot.AspNetCore.Tests/Integration/ValidationEnvelopeParityTests.cs
// layer: Tests | package: ApiPilot.AspNetCore.Tests | since: v0.2.0-alpha.0
// purpose: Asserts that the filter path and the [ApiController] path produce identical envelopes
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : InProcessHost, TestAssert, ApiPilot.AspNetCore.DependencyInjection,
//                ApiPilot.AspNetCore.Serialization
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : ApiPilotValidateAttributeIntegrationTests.cs,
//                ApiPilotControllersIntegrationTests.cs
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.DependencyInjection;
using ApiPilot.AspNetCore.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace ApiPilot.AspNetCore.Tests.Integration;

[TestClass]
public sealed class ValidationEnvelopeParityTests
{
    private static async Task<InProcessHost> StartFilterPathHostAsync()
    {
        return await InProcessHost.StartAsync(
            configureServices: builder =>
            {
                builder.Services.AddApiPilotJson();
                builder.Services.AddControllers()
                    .AddApplicationPart(typeof(SimpleController).Assembly);
            },
            configureApp: app =>
            {
                app.UseRouting();
                app.MapControllers();
            });
    }

    private static async Task<InProcessHost> StartApiControllerPathHostAsync()
    {
        return await InProcessHost.StartAsync(
            configureServices: builder =>
            {
                builder.Services.AddApiPilotJson();
                builder.Services.AddControllers()
                    .AddApplicationPart(typeof(ApiControllerSampleController).Assembly);
                builder.Services.AddApiPilotControllers();
            },
            configureApp: app =>
            {
                app.UseRouting();
                app.MapControllers();
            });
    }

    private static HttpContent InvalidBody()
    {
        return new StringContent("{}", Encoding.UTF8, "application/json");
    }

    private static async Task<JsonElement> PostInvalidAsync(InProcessHost host, string url)
    {
        var response = await host.Client.PostAsync(url, InvalidBody());
        var json = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json).RootElement.Clone();
    }

    [Test]
    public async Task InvalidInput_FilterPathAndApiControllerPath_ProduceSameCode()
    {
        await using var filterHost = await StartFilterPathHostAsync();
        await using var acHost = await StartApiControllerPathHostAsync();

        var filterRoot = await PostInvalidAsync(filterHost, "/api/simple");
        var acRoot = await PostInvalidAsync(acHost, "/api/apipilot-sample");

        var filterCode = filterRoot.GetProperty("error").GetProperty("code").GetString();
        var acCode = acRoot.GetProperty("error").GetProperty("code").GetString();
        TestAssert.Equal("VALIDATION_ERROR", filterCode);
        TestAssert.Equal(filterCode, acCode);
    }

    [Test]
    public async Task InvalidInput_FilterPathAndApiControllerPath_ProduceSameFields()
    {
        await using var filterHost = await StartFilterPathHostAsync();
        await using var acHost = await StartApiControllerPathHostAsync();

        var filterRoot = await PostInvalidAsync(filterHost, "/api/simple");
        var acRoot = await PostInvalidAsync(acHost, "/api/apipilot-sample");

        var filterFields = filterRoot.GetProperty("error").GetProperty("fields");
        var acFields = acRoot.GetProperty("error").GetProperty("fields");

        TestAssert.True(filterFields.TryGetProperty("email", out _));
        TestAssert.True(acFields.TryGetProperty("email", out _));
        TestAssert.Equal(filterFields.EnumerateObject().Count(),
            acFields.EnumerateObject().Count());
    }

    [Test]
    public async Task InvalidInput_FilterPathAndApiControllerPath_ProduceSameFieldMessages()
    {
        await using var filterHost = await StartFilterPathHostAsync();
        await using var acHost = await StartApiControllerPathHostAsync();

        var filterRoot = await PostInvalidAsync(filterHost, "/api/simple");
        var acRoot = await PostInvalidAsync(acHost, "/api/apipilot-sample");

        var filterMessages = filterRoot.GetProperty("error").GetProperty("fields").GetProperty("email");
        var acMessages = acRoot.GetProperty("error").GetProperty("fields").GetProperty("email");

        TestAssert.Equal(filterMessages.GetArrayLength(), acMessages.GetArrayLength());
        TestAssert.Equal(filterMessages[0].GetString(), acMessages[0].GetString());
    }
}

