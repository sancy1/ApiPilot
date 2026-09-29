// filepath: dotnet/tests/ApiPilot.AspNetCore.Tests/InProcessHost.cs
// layer: TestInfrastructure | package: ApiPilot.AspNetCore.Tests | since: v0.2.0-alpha.0
// purpose: Starts an in-process ASP.NET Core host on a random loopback port for integration tests
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : IAsyncDisposable
//   Depends on : WebApplicationBuilder, WebApplication, IServerAddressesFeature, HttpClient
//   Used by    : every integration test that needs a real HTTP endpoint
//   See also   : Integration/SmokeTests.cs, Integration/JsonSerializationIntegrationTests.cs
// -----------------------------------------------------------------------------

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;

namespace ApiPilot.AspNetCore.Tests;

/// <summary>
/// Starts a real ASP.NET Core application in-process, bound to a random
/// loopback port. Exposes an HttpClient configured against that port and
/// a BaseAddress for constructing request URIs. Dispose stops the host and
/// disposes the HttpClient.
/// </summary>
/// <remarks>
/// Port 0 is used so the OS picks a free ephemeral port. The actual port is
/// read back from the server addresses feature after the host starts. This
/// allows multiple InProcessHost instances to run without port conflicts.
/// </remarks>
internal sealed class InProcessHost : IAsyncDisposable
{
    private readonly WebApplication _app;
    private readonly HttpClient _client;

    /// <summary>The base address of the running host, including the assigned port.</summary>
    public Uri BaseAddress { get; }

    /// <summary>
    /// The HTTP client used to issue requests against the running host.
    /// Configured with the host base address. Do not dispose directly;
    /// dispose the host instead.
    /// </summary>
    public HttpClient Client => _client;

    private InProcessHost(WebApplication app, HttpClient client, Uri baseAddress)
    {
        _app = app;
        _client = client;
        BaseAddress = baseAddress;
    }

    /// <summary>
    /// Starts an in-process ASP.NET Core application. Service registration
    /// happens through <paramref name="configureServices"/> on the
    /// WebApplicationBuilder before the application is built. Middleware
    /// and endpoint mapping happen through <paramref name="configureApp"/>
    /// on the WebApplication after build, before the server starts.
    /// </summary>
    /// <param name="configureServices">
    /// Optional callback for service registration. Receives the
    /// WebApplicationBuilder. May be null.
    /// </param>
    /// <param name="configureApp">
    /// Optional callback for middleware and endpoint mapping. Receives the
    /// built WebApplication. May be null.
    /// </param>
    /// <returns>A started InProcessHost.</returns>
    public static async Task<InProcessHost> StartAsync(
        Action<WebApplicationBuilder>? configureServices = null,
        Action<WebApplication>? configureApp = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        configureServices?.Invoke(builder);

        var app = builder.Build();
        configureApp?.Invoke(app);

        await app.StartAsync();

        var server = app.Services.GetRequiredService<IServer>();
        var addressesFeature = server.Features.Get<IServerAddressesFeature>();
        if (addressesFeature is null || addressesFeature.Addresses.Count == 0)
        {
            await app.StopAsync();
            await app.DisposeAsync();
            throw new InvalidOperationException("Server did not report any bound addresses.");
        }

        var address = addressesFeature.Addresses.First();
        var baseAddress = new Uri(address, UriKind.Absolute);
        var client = new HttpClient { BaseAddress = baseAddress };

        return new InProcessHost(app, client, baseAddress);
    }

    /// <summary>Stops the host and disposes the HttpClient.</summary>
    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
    }
}

