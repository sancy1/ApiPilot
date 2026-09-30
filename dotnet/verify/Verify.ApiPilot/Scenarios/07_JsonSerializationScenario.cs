// filepath: dotnet/verify/Verify.ApiPilot/Scenarios/07_JsonSerializationScenario.cs
// layer:    Verification
// package:  Verify.ApiPilot
// purpose:  Scenario 07. Proves that a real consumer of the published
//           ApiPilot.AspNetCore 1.0.3 package can register the JSON
//           serialization conventions and observe the documented defaults
//           and overrides on the wire. Also proves that
//           JsonSerializerConfigurator.Configure is idempotent with respect
//           to converter registration.
// relates:  Uses ApiPilot.AspNetCore.Serialization.JsonSerializationExtensions,
//           EnumSerializationMode,
//           DateSerializationMode,
//           JsonSerializerConfigurator,
//           ApiPilotJsonOptions,
//           ApiPilot.Core.Metadata.ResponseMetadata,
//           ApiPilot.Core.Responses.ApiResponseBuilder,
//           ApiPilot.AspNetCore.Results.ApiPilotResultsExtensions.
// authority: the shipped lib/net10.0/ApiPilot.AspNetCore.xml and the
//           packaged README. Not the source tree.

using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using ApiPilot.AspNetCore.Results;
using ApiPilot.AspNetCore.Configuration;
using ApiPilot.AspNetCore.Serialization;
using ApiPilot.Core.Metadata;
using ApiPilot.Core.Responses;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Verify.ApiPilot.Infrastructure;

namespace Verify.ApiPilot.Scenarios;

/// <summary>
/// Scenario 07. JSON serialization conventions end to end over real HTTP.
/// Eight sub-checks: four defaults, three overrides, one idempotence.
/// </summary>
public static class JsonSerializationScenario
{
    /// <summary>The scenario name.</summary>
    public const string Name = "07_JsonSerialization";

    /// <summary>A fixed DateTimeOffset used across the date sub-checks.</summary>
    private static readonly DateTimeOffset FixedInstant =
        new DateTimeOffset(2026, 09, 29, 12, 00, 00, TimeSpan.Zero);

    /// <summary>The sample enum used across the enum sub-checks.</summary>
    private enum SampleState { Confirmed, Pending }

    /// <summary>The sample DTO used across the property-naming sub-checks.</summary>
    private sealed record SampleDto(string OrderId, SampleState State, DateTimeOffset When, string? Note);

    /// <summary>Runs the scenario.</summary>
    public static async Task<ScenarioResult> RunAsync()
    {
        try
        {
            var failures = new List<string>();

            await CheckDefaultCamelCase(failures);
            await CheckDefaultStringEnum(failures);
            await CheckDefaultIso8601Date(failures);
            await CheckDefaultNullWritten(failures);
            await CheckOverrideEnumNumber(failures);
            await CheckOverrideUnixSeconds(failures);
            await CheckOverridePascalCase(failures);
            CheckConfiguratorIdempotence(failures);

            if (failures.Count > 0)
            {
                return ScenarioResult.Failed(Name, string.Join(" | ", failures));
            }

            return ScenarioResult.Passed(
                Name,
                "AddApiPilotJson is public and callable; the documented defaults (camelCase, string enums, ISO 8601 dates, explicit nulls) are on the wire; the documented overrides (EnumMode, DateMode, PropertyNamingPolicy) are consumed; JsonSerializerConfigurator.Configure is idempotent on converter registration.");
        }
        catch (Exception ex)
        {
            return ScenarioResult.Failed(Name, "Unhandled exception during scenario.", ex);
        }
    }

    // -------------------------------------------------------------------
    // Sub-check 1: default camelCase property naming
    // -------------------------------------------------------------------
    private static async Task CheckDefaultCamelCase(List<string> failures)
    {
        await using var host = await StartHostAsync(null);
        using var client = host.CreateClient();
        using var response = await client.GetAsync(new Uri("/", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();
        if (response.StatusCode != HttpStatusCode.OK) { failures.Add("CamelCase: expected 200, got " + (int)response.StatusCode + "."); return; }
        if (!body.Contains("\"orderId\"", StringComparison.Ordinal)) { failures.Add("CamelCase: body missing camelCase key orderId."); return; }
        if (body.Contains("\"OrderId\"", StringComparison.Ordinal)) { failures.Add("CamelCase: body unexpectedly contains PascalCase OrderId."); }
    }

    // -------------------------------------------------------------------
    // Sub-check 2: default string enum
    // -------------------------------------------------------------------
    private static async Task CheckDefaultStringEnum(List<string> failures)
    {
        await using var host = await StartHostAsync(null);
        using var client = host.CreateClient();
        using var response = await client.GetAsync(new Uri("/", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();
        if (response.StatusCode != HttpStatusCode.OK) { failures.Add("StringEnum: expected 200, got " + (int)response.StatusCode + "."); return; }
        if (!body.Contains("\"Confirmed\"", StringComparison.Ordinal)) { failures.Add("StringEnum: body missing string enum value Confirmed. Body=" + body); return; }
        if (body.Contains("\"state\":0", StringComparison.Ordinal)) { failures.Add("StringEnum: body contains numeric enum value 0, expected string form."); }
    }

    // -------------------------------------------------------------------
    // Sub-check 3: default ISO 8601 date
    // -------------------------------------------------------------------
    private static async Task CheckDefaultIso8601Date(List<string> failures)
    {
        await using var host = await StartHostAsync(null);
        using var client = host.CreateClient();
        using var response = await client.GetAsync(new Uri("/", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();
        if (response.StatusCode != HttpStatusCode.OK) { failures.Add("Iso8601: expected 200, got " + (int)response.StatusCode + "."); return; }
        if (!body.Contains("2026-09-29T12:00:00", StringComparison.Ordinal))
            failures.Add("Iso8601: body does not contain an ISO 8601 date. Body=" + body);
    }

    // -------------------------------------------------------------------
    // Sub-check 4: default null written explicitly
    // -------------------------------------------------------------------
    private static async Task CheckDefaultNullWritten(List<string> failures)
    {
        await using var host = await StartHostAsync(null);
        using var client = host.CreateClient();
        using var response = await client.GetAsync(new Uri("/", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();
        if (response.StatusCode != HttpStatusCode.OK) { failures.Add("NullWritten: expected 200, got " + (int)response.StatusCode + "."); return; }
        if (!body.Contains("\"note\":null", StringComparison.Ordinal))
            failures.Add("NullWritten: body does not contain a \"note\":null key. Body=" + body);
    }

    // -------------------------------------------------------------------
    // Sub-check 5: override EnumMode = Number
    // -------------------------------------------------------------------
    private static async Task CheckOverrideEnumNumber(List<string> failures)
    {
        await using var host = await StartHostAsync(o => o.EnumMode = EnumSerializationMode.Number);
        using var client = host.CreateClient();
        using var response = await client.GetAsync(new Uri("/", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();
        if (response.StatusCode != HttpStatusCode.OK) { failures.Add("EnumNumber: expected 200, got " + (int)response.StatusCode + "."); return; }
        if (!body.Contains("\"state\":0", StringComparison.Ordinal))
            failures.Add("EnumNumber: body does not contain numeric enum state. Body=" + body);
        if (body.Contains("\"Confirmed\"", StringComparison.Ordinal))
            failures.Add("EnumNumber: body unexpectedly contains the string enum value Confirmed.");
    }

    // -------------------------------------------------------------------
    // Sub-check 6: override DateMode = UnixTimeSeconds
    // -------------------------------------------------------------------
    private static async Task CheckOverrideUnixSeconds(List<string> failures)
    {
        await using var host = await StartHostAsync(o => o.DateMode = DateSerializationMode.UnixTimeSeconds);
        using var client = host.CreateClient();
        using var response = await client.GetAsync(new Uri("/", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();
        if (response.StatusCode != HttpStatusCode.OK) { failures.Add("UnixSeconds: expected 200, got " + (int)response.StatusCode + "."); return; }

        var expected = FixedInstant.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (!body.Contains("\"when\":" + expected, StringComparison.Ordinal))
            failures.Add("UnixSeconds: body does not contain \"when\":" + expected + ". Body=" + body);
        if (body.Contains("2026-09-29T12:00:00", StringComparison.Ordinal))
            failures.Add("UnixSeconds: body unexpectedly contains an ISO 8601 date.");
    }

    // -------------------------------------------------------------------
    // Sub-check 7: override PropertyNamingPolicy = null produces PascalCase
    // -------------------------------------------------------------------
    private static async Task CheckOverridePascalCase(List<string> failures)
    {
        await using var host = await StartHostAsync(o => o.PropertyNamingPolicy = null);
        using var client = host.CreateClient();
        using var response = await client.GetAsync(new Uri("/", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();
        if (response.StatusCode != HttpStatusCode.OK) { failures.Add("PascalCase: expected 200, got " + (int)response.StatusCode + "."); return; }
        if (!body.Contains("\"OrderId\"", StringComparison.Ordinal))
            failures.Add("PascalCase: body missing PascalCase key OrderId. Body=" + body);
        if (body.Contains("\"orderId\"", StringComparison.Ordinal))
            failures.Add("PascalCase: body unexpectedly contains camelCase orderId.");
    }

    // -------------------------------------------------------------------
    // Sub-check 8: JsonSerializerConfigurator.Configure is idempotent
    // -------------------------------------------------------------------
    private static void CheckConfiguratorIdempotence(List<string> failures)
    {
        try
        {
            var target = new JsonSerializerOptions();
            var options = new ApiPilotJsonOptions
            {
                DateMode = DateSerializationMode.UnixTimeSeconds,
            };

            JsonSerializerConfigurator.Configure(target, options);
            var firstCount = target.Converters.Count;

            JsonSerializerConfigurator.Configure(target, options);
            var secondCount = target.Converters.Count;

            if (secondCount != firstCount)
            {
                failures.Add("ConfiguratorIdempotence: converter count changed on second Configure call (" + firstCount + " -> " + secondCount + ").");
            }
        }
        catch (Exception ex)
        {
            failures.Add("ConfiguratorIdempotence: unhandled exception: " + ex.GetType().Name + ": " + ex.Message);
        }
    }

    // -------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------
    private static async Task<InProcessHost> StartHostAsync(Action<ApiPilotJsonOptions>? configure)
    {
        return await InProcessHost.StartAsync(
            configureServices: services =>
            {
                services.AddApiPilotJson(configure);
            },
            configurePipeline: app =>
            {
                app.MapGet("/", (HttpContext context) =>
                {
                    var meta = ResponseMetadata.Empty.WithRequestId(context.TraceIdentifier);
                    var payload = new SampleDto(
                        OrderId: "ORD-1",
                        State: SampleState.Confirmed,
                        When: FixedInstant,
                        Note: null);
                    return ApiResponseBuilder.Ok(payload, meta, "ok").ToResult();
                });
            });
    }
}