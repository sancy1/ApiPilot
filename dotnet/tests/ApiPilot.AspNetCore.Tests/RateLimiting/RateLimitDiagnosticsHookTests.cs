// filepath: dotnet/tests/ApiPilot.AspNetCore.Tests/RateLimiting/RateLimitDiagnosticsHookTests.cs
// layer: Tests | package: ApiPilot.AspNetCore.Tests | since: v0.5.0
// purpose: Contract tests for the RateLimitDiagnosticsHook rejection handler
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class + test lease double)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute,
//                ApiPilot.AspNetCore.RateLimiting, ApiPilot.AspNetCore.Tests.Middleware,
//                Microsoft.AspNetCore.RateLimiting, System.Threading.RateLimiting
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : RateLimitDiagnosticsHook.cs, docs/rate-limiting.md
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.Configuration;
using ApiPilot.AspNetCore.RateLimiting;
using ApiPilot.AspNetCore.Serialization;
using ApiPilot.AspNetCore.Tests.Middleware;
using ApiPilot.Core.Metadata;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Diagnostics.Metrics;
using System.Text.Json;
using System.Threading.RateLimiting;

namespace ApiPilot.AspNetCore.Tests.RateLimiting;

/// <summary>
/// Contract tests for RateLimitDiagnosticsHook. Verifies the correlation
/// envelope rules, the rate_limited wire shape, the configured status and
/// message, the metric, the Retry-After behavior, the response-already-
/// started guard, and the cancellation path.
/// </summary>
[TestClass]
public sealed class RateLimitDiagnosticsHookTests
{
    private static (OnRejectedContext context, HttpContext http, MemoryStream body) Build(
        Action<ApiPilotRateLimitOptions>? configureOptions = null,
        ICorrelationIdAccessor? accessor = null,
        Action<CorrelationOptions>? configureCorrelation = null,
        IDictionary<string, object?>? leaseMetadata = null)
    {
        var rlOptions = new ApiPilotRateLimitOptions();
        configureOptions?.Invoke(rlOptions);

        var services = new ServiceCollection();
        services.AddApiPilotJson();
        services.AddSingleton<IOptions<ApiPilotRateLimitOptions>>(Options.Create(rlOptions));
        if (accessor is not null)
        {
            services.AddSingleton<ICorrelationIdAccessor>(accessor);
        }
        if (configureCorrelation is not null)
        {
            var correlationOptions = new CorrelationOptions();
            configureCorrelation(correlationOptions);
            services.AddSingleton<IOptions<CorrelationOptions>>(Options.Create(correlationOptions));
        }
        var provider = services.BuildServiceProvider();

        var http = new DefaultHttpContext { RequestServices = provider };
        var body = new MemoryStream();
        http.Response.Body = body;

        var lease = new TestRateLimitLease(leaseMetadata);
        var context = new OnRejectedContext { HttpContext = http, Lease = lease };
        return (context, http, body);
    }

    /// <summary>A non-null accessor value is written to the envelope request id.</summary>
    [Test]
    public async Task HandleAsync_AccessorPresent_UsesAccessorId()
    {
        var accessor = new StubCorrelationAccessor("accessor-id-1");
        var (ctx, http, body) = Build(accessor: accessor);
        http.TraceIdentifier = "trace-id-1";

        await RateLimitDiagnosticsHook.HandleAsync(ctx, CancellationToken.None);

        body.Position = 0;
        var doc = await JsonDocument.ParseAsync(body);
        var requestId = doc.RootElement.GetProperty("meta").GetProperty("requestId").GetString();
        TestAssert.Equal("accessor-id-1", requestId);
    }

    /// <summary>No accessor falls back to the trace identifier.</summary>
    [Test]
    public async Task HandleAsync_AccessorAbsent_FallsBackToTraceIdentifier()
    {
        var (ctx, http, body) = Build();
        http.TraceIdentifier = "trace-fallback";

        await RateLimitDiagnosticsHook.HandleAsync(ctx, CancellationToken.None);

        body.Position = 0;
        var doc = await JsonDocument.ParseAsync(body);
        var requestId = doc.RootElement.GetProperty("meta").GetProperty("requestId").GetString();
        TestAssert.Equal("trace-fallback", requestId);
    }

    /// <summary>EchoInResponseBody false writes an empty request id.</summary>
    [Test]
    public async Task HandleAsync_EchoOff_RequestIdIsEmpty()
    {
        var (ctx, http, body) = Build(
            configureCorrelation: o => o.EchoInResponseBody = false);
        http.TraceIdentifier = "trace-should-not-appear";

        await RateLimitDiagnosticsHook.HandleAsync(ctx, CancellationToken.None);

        body.Position = 0;
        var doc = await JsonDocument.ParseAsync(body);
        var requestId = doc.RootElement.GetProperty("meta").GetProperty("requestId").GetString();
        TestAssert.Equal(string.Empty, requestId);
    }

    /// <summary>The rejection writes the configured status code.</summary>
    [Test]
    public async Task HandleAsync_WritesConfiguredStatus()
    {
        var (ctx, http, _) = Build();
        await RateLimitDiagnosticsHook.HandleAsync(ctx, CancellationToken.None);
        TestAssert.Equal(429, http.Response.StatusCode);
    }

    /// <summary>The rejection writes the RATE_LIMITED wire code.</summary>
    [Test]
    public async Task HandleAsync_WritesRateLimitedCode()
    {
        var (ctx, _, body) = Build();
        await RateLimitDiagnosticsHook.HandleAsync(ctx, CancellationToken.None);

        body.Position = 0;
        var doc = await JsonDocument.ParseAsync(body);
        var code = doc.RootElement.GetProperty("error").GetProperty("code").GetString();
        TestAssert.Equal("RATE_LIMITED", code);
    }

    /// <summary>The configured message appears in the body.</summary>
    [Test]
    public async Task HandleAsync_WritesConfiguredMessage()
    {
        var (ctx, _, body) = Build(configureOptions: o => o.Message = "custom rate message");
        await RateLimitDiagnosticsHook.HandleAsync(ctx, CancellationToken.None);

        body.Position = 0;
        var doc = await JsonDocument.ParseAsync(body);
        var message = doc.RootElement.GetProperty("error").GetProperty("message").GetString();
        TestAssert.Equal("custom rate message", message);
    }

    /// <summary>A configured status override is written.</summary>
    [Test]
    public async Task HandleAsync_StatusOverride_IsWritten()
    {
        var (ctx, http, _) = Build(configureOptions: o => o.StatusCode = 503);
        await RateLimitDiagnosticsHook.HandleAsync(ctx, CancellationToken.None);
        TestAssert.Equal(503, http.Response.StatusCode);
    }

    /// <summary>The registered exception options instance is not mutated by the hook.</summary>
    [Test]
    public async Task HandleAsync_RegisteredExceptionOptions_AreNotMutated()
    {
        var registered = new ApiExceptionOptions();
        registered.ErrorCodeToStatusMap = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["VALIDATION_ERROR"] = 422,
        };

        var services = new ServiceCollection();
        services.AddApiPilotJson();
        services.AddSingleton<IOptions<ApiPilotRateLimitOptions>>(Options.Create(new ApiPilotRateLimitOptions { StatusCode = 503 }));
        services.AddSingleton<IOptions<ApiExceptionOptions>>(Options.Create(registered));
        var provider = services.BuildServiceProvider();

        var http = new DefaultHttpContext { RequestServices = provider };
        var body = new MemoryStream();
        http.Response.Body = body;
        var context = new OnRejectedContext { HttpContext = http, Lease = new TestRateLimitLease() };

        await RateLimitDiagnosticsHook.HandleAsync(context, CancellationToken.None);

        TestAssert.Equal(503, http.Response.StatusCode);
        TestAssert.Equal(1, registered.ErrorCodeToStatusMap.Count);
        TestAssert.False(registered.ErrorCodeToStatusMap.ContainsKey("RATE_LIMITED"));
        TestAssert.Equal(422, registered.ErrorCodeToStatusMap["VALIDATION_ERROR"]);
    }

    /// <summary>An unrelated error-code mapping is preserved through the clone.</summary>
    [Test]
    public async Task HandleAsync_UnrelatedMappings_ArePreserved()
    {
        var registered = new ApiExceptionOptions();
        registered.ErrorCodeToStatusMap = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["CONFLICT"] = 409,
            ["RESOURCE_NOT_FOUND"] = 404,
        };

        var services = new ServiceCollection();
        services.AddApiPilotJson();
        services.AddSingleton<IOptions<ApiPilotRateLimitOptions>>(Options.Create(new ApiPilotRateLimitOptions()));
        services.AddSingleton<IOptions<ApiExceptionOptions>>(Options.Create(registered));
        var provider = services.BuildServiceProvider();

        var http = new DefaultHttpContext { RequestServices = provider };
        var body = new MemoryStream();
        http.Response.Body = body;
        var context = new OnRejectedContext { HttpContext = http, Lease = new TestRateLimitLease() };

        await RateLimitDiagnosticsHook.HandleAsync(context, CancellationToken.None);

        TestAssert.Equal(429, http.Response.StatusCode);
        TestAssert.Equal(2, registered.ErrorCodeToStatusMap.Count);
        TestAssert.Equal(409, registered.ErrorCodeToStatusMap["CONFLICT"]);
        TestAssert.Equal(404, registered.ErrorCodeToStatusMap["RESOURCE_NOT_FOUND"]);
    }

    /// <summary>Concurrent requests with different options do not leak status mappings.</summary>
    [Test]
    public async Task HandleAsync_ConcurrentRequests_DifferentOptions_DoNotLeak()
    {
        async Task<int> Invoke(int statusCode)
        {
            var services = new ServiceCollection();
            services.AddApiPilotJson();
            services.AddSingleton<IOptions<ApiPilotRateLimitOptions>>(
                Options.Create(new ApiPilotRateLimitOptions { StatusCode = statusCode }));
            var provider = services.BuildServiceProvider();

            var http = new DefaultHttpContext { RequestServices = provider };
            var body = new MemoryStream();
            http.Response.Body = body;
            var context = new OnRejectedContext { HttpContext = http, Lease = new TestRateLimitLease() };

            await RateLimitDiagnosticsHook.HandleAsync(context, CancellationToken.None);
            return http.Response.StatusCode;
        }

        var task503 = Invoke(503);
        var task429 = Invoke(429);
        await Task.WhenAll(task503, task429);

        TestAssert.Equal(503, await task503);
        TestAssert.Equal(429, await task429);
    }
    /// <summary>The rejection emits the apipilot.ratelimit.rejected metric.</summary>
    [Test]
    public async Task HandleAsync_EmitsRejectedMetric()
    {
        var measurements = new List<(string Name, long Value)>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name == "ApiPilot") { l.EnableMeasurementEvents(instrument); }
        };
        listener.SetMeasurementEventCallback<long>((instrument, value, tags, state) =>
        {
            measurements.Add((instrument.Name, value));
        });
        listener.Start();

        var (ctx, _, _) = Build();
        await RateLimitDiagnosticsHook.HandleAsync(ctx, CancellationToken.None);

        TestAssert.Equal(1, measurements.Count);
        TestAssert.Equal("apipilot.ratelimit.rejected", measurements[0].Name);
        TestAssert.Equal(1L, measurements[0].Value);
    }

    /// <summary>The metric carries the lease reason as the reason tag.</summary>
    [Test]
    public async Task HandleAsync_MetricCarriesReasonTag()
    {
        string? capturedReason = null;
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name == "ApiPilot") { l.EnableMeasurementEvents(instrument); }
        };
        listener.SetMeasurementEventCallback<long>((instrument, value, tags, state) =>
        {
            foreach (var tag in tags)
            {
                if (tag.Key == "reason" && tag.Value is string s) { capturedReason = s; }
            }
        });
        listener.Start();

        var metadata = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            [MetadataName.ReasonPhrase.Name] = "limit_exceeded",
        };
        var (ctx, _, _) = Build(leaseMetadata: metadata);
        await RateLimitDiagnosticsHook.HandleAsync(ctx, CancellationToken.None);

        TestAssert.Equal("limit_exceeded", capturedReason);
    }

    /// <summary>Absent retry metadata emits no Retry-After header.</summary>
    [Test]
    public async Task HandleAsync_RetryAfter_AbsentMetadata_NoHeader()
    {
        var (ctx, http, _) = Build();
        await RateLimitDiagnosticsHook.HandleAsync(ctx, CancellationToken.None);
        TestAssert.False(http.Response.Headers.ContainsKey("Retry-After"));
    }

    /// <summary>A positive retry interval is written as integer seconds.</summary>
    [Test]
    public async Task HandleAsync_RetryAfter_PositiveSeconds_WritesIntegerHeader()
    {
        var metadata = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            [MetadataName.RetryAfter.Name] = TimeSpan.FromSeconds(5),
        };
        var (ctx, http, _) = Build(leaseMetadata: metadata);
        await RateLimitDiagnosticsHook.HandleAsync(ctx, CancellationToken.None);
        TestAssert.Equal("5", http.Response.Headers["Retry-After"].ToString());
    }

    /// <summary>A zero retry interval emits no header.</summary>
    [Test]
    public async Task HandleAsync_RetryAfter_Zero_NoHeader()
    {
        var metadata = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            [MetadataName.RetryAfter.Name] = TimeSpan.Zero,
        };
        var (ctx, http, _) = Build(leaseMetadata: metadata);
        await RateLimitDiagnosticsHook.HandleAsync(ctx, CancellationToken.None);
        TestAssert.False(http.Response.Headers.ContainsKey("Retry-After"));
    }

    /// <summary>A negative retry interval emits no header.</summary>
    [Test]
    public async Task HandleAsync_RetryAfter_Negative_NoHeader()
    {
        var metadata = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            [MetadataName.RetryAfter.Name] = TimeSpan.FromSeconds(-1),
        };
        var (ctx, http, _) = Build(leaseMetadata: metadata);
        await RateLimitDiagnosticsHook.HandleAsync(ctx, CancellationToken.None);
        TestAssert.False(http.Response.Headers.ContainsKey("Retry-After"));
    }

    /// <summary>A fractional retry interval is rounded up to whole seconds.</summary>
    [Test]
    public async Task HandleAsync_RetryAfter_Fractional_Ceils()
    {
        var metadata = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            [MetadataName.RetryAfter.Name] = TimeSpan.FromSeconds(1.5),
        };
        var (ctx, http, _) = Build(leaseMetadata: metadata);
        await RateLimitDiagnosticsHook.HandleAsync(ctx, CancellationToken.None);
        TestAssert.Equal("2", http.Response.Headers["Retry-After"].ToString());
    }

    /// <summary>EmitRetryAfter false suppresses the header.</summary>
    [Test]
    public async Task HandleAsync_RetryAfter_Disabled_NoHeader()
    {
        var metadata = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            [MetadataName.RetryAfter.Name] = TimeSpan.FromSeconds(5),
        };
        var (ctx, http, _) = Build(
            configureOptions: o => o.EmitRetryAfter = false,
            leaseMetadata: metadata);
        await RateLimitDiagnosticsHook.HandleAsync(ctx, CancellationToken.None);
        TestAssert.False(http.Response.Headers.ContainsKey("Retry-After"));
    }

    /// <summary>A started response is not written.</summary>
    [Test]
    public async Task HandleAsync_ResponseAlreadyStarted_DoesNotWrite()
    {
        var (ctx, http, body) = Build();
        http.Features.Set<Microsoft.AspNetCore.Http.Features.IHttpResponseFeature>(new StartedResponseFeature());

        await RateLimitDiagnosticsHook.HandleAsync(ctx, CancellationToken.None);

        TestAssert.Equal(0L, body.Length);
    }

    /// <summary>A canceled request is not written.</summary>
    [Test]
    public async Task HandleAsync_RequestAborted_DoesNotWrite()
    {
        var (ctx, http, body) = Build();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        http.RequestAborted = cts.Token;

        try
        {
            await RateLimitDiagnosticsHook.HandleAsync(ctx, cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Acceptable: cancellation propagated.
        }

        TestAssert.Equal(0L, body.Length);
    }

    /// <summary>The rejection emits a structured log entry.</summary>
    [Test]
    public async Task HandleAsync_EmitsStructuredLog()
    {
        var logger = new FakeLogger<RateLimitDiagnosticsHookTests>();
        var services = new ServiceCollection();
        services.AddApiPilotJson();
        services.AddSingleton<IOptions<ApiPilotRateLimitOptions>>(Options.Create(new ApiPilotRateLimitOptions()));
        services.AddSingleton<Microsoft.Extensions.Logging.ILoggerFactory>(new StubLoggerFactory(logger));
        var provider = services.BuildServiceProvider();

        var http = new DefaultHttpContext { RequestServices = provider };
        var body = new MemoryStream();
        http.Response.Body = body;
        var context = new OnRejectedContext { HttpContext = http, Lease = new TestRateLimitLease() };

        await RateLimitDiagnosticsHook.HandleAsync(context, CancellationToken.None);

        TestAssert.True(logger.Entries.Count > 0);
        var found = false;
        foreach (var e in logger.Entries)
        {
            if (e.Message.Contains("RATE_LIMITED", StringComparison.Ordinal)) { found = true; }
        }
        TestAssert.True(found);
    }

}

/// <summary>A rate-limit lease backed by an in-memory metadata dictionary.</summary>
internal sealed class TestRateLimitLease : RateLimitLease
{
    private readonly Dictionary<string, object?> _metadata;

    /// <summary>Creates a lease with the supplied metadata entries. May be null.</summary>
    /// <param name="metadata">The metadata name/value pairs. May be null.</param>
    public TestRateLimitLease(IDictionary<string, object?>? metadata = null)
    {
        _metadata = metadata is null
            ? new Dictionary<string, object?>(StringComparer.Ordinal)
            : new Dictionary<string, object?>(metadata, StringComparer.Ordinal);
    }

    /// <inheritdoc />
    public override bool IsAcquired => true;

    /// <inheritdoc />
    public override IEnumerable<string> MetadataNames => _metadata.Keys;

    /// <inheritdoc />
    public override bool TryGetMetadata(string metadataName, out object? metadata)
    {
        return _metadata.TryGetValue(metadataName, out metadata);
    }
}

/// <summary>A correlation accessor returning a fixed id.</summary>
internal sealed class StubCorrelationAccessor : ICorrelationIdAccessor
{
    /// <summary>Creates the accessor with a fixed id.</summary>
    /// <param name="requestId">The id to return. Must not be null.</param>
    public StubCorrelationAccessor(string requestId)
    {
        RequestId = requestId;
    }

    /// <inheritdoc />
    public string? RequestId { get; }
}


/// <summary>A logger factory that returns a single FakeLogger for any category.</summary>
internal sealed class StubLoggerFactory : Microsoft.Extensions.Logging.ILoggerFactory
{
    private readonly Microsoft.Extensions.Logging.ILogger _logger;

    /// <summary>Creates the factory around a fixed logger.</summary>
    /// <param name="logger">The logger to return. Must not be null.</param>
    public StubLoggerFactory(Microsoft.Extensions.Logging.ILogger logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName) => _logger;

    /// <inheritdoc />
    public void AddProvider(Microsoft.Extensions.Logging.ILoggerProvider provider) { }

    /// <inheritdoc />
    public void Dispose() { }
}
