// filepath: dotnet/verify/Verify.ApiPilot/Infrastructure/InProcessHost.cs
// layer:    Verification
// package:  Verify.ApiPilot
// purpose:  Builds, starts, and disposes a real ASP.NET Core host on a free
//           loopback port. The scenario code passes service registration and
//           pipeline configuration through callbacks. No third-party package
//           is used: WebApplication and Kestrel come from the ASP.NET Core
//           shared framework, which is a platform reference, not a package.
// relates:  Used by every AspNetCore scenario. The endpoint route is
//           configured by the scenario through the pipeline callback. The
//           actual bound port is read from the server after startup.

using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Verify.ApiPilot.Infrastructure;

/// <summary>
/// A started ASP.NET Core host on a free loopback port. Disposing the host
/// stops the server and releases the port. Each instance owns its own
/// service collection and pipeline, so scenarios can start several hosts in
/// sequence without interfering with each other.
/// </summary>
public sealed class InProcessHost : IAsyncDisposable
{
    private readonly WebApplication _app;
    private readonly string _baseAddress;

    private InProcessHost(WebApplication app, string baseAddress)
    {
        _app = app;
        _baseAddress = baseAddress;
    }

    /// <summary>
    /// The base address, for example "http://127.0.0.1:54321". No trailing slash.
    /// </summary>
    public string BaseAddress => _baseAddress;

    /// <summary>
    /// Builds and starts a host.
    /// </summary>
    /// <param name="configureServices">Optional callback invoked before Build().</param>
    /// <param name="configurePipeline">Callback invoked after Build() to add middleware and endpoints. Must not be null.</param>
    public static async Task<InProcessHost> StartAsync(
        Action<IServiceCollection>? configureServices,
        Action<WebApplication> configurePipeline)
    {
        ArgumentNullException.ThrowIfNull(configurePipeline);

        var builder = WebApplication.CreateBuilder();

        // Silence the framework's console logging so the verifier output stays clean.
        builder.Logging.ClearProviders();

        // Bind to an ephemeral loopback port. Port 0 asks the OS for a free one.
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        configureServices?.Invoke(builder.Services);

        var app = builder.Build();

        configurePipeline(app);

        await app.StartAsync().ConfigureAwait(false);

        var server = app.Services.GetRequiredService<IServer>();
        var addressesFeature = server.Features.Get<IServerAddressesFeature>()
            ?? throw new InvalidOperationException("The server did not expose IServerAddressesFeature. Cannot determine the bound address.");
        var address = addressesFeature.Addresses.FirstOrDefault()
            ?? throw new InvalidOperationException("The server started but reported no bound address.");

        // Normalize: strip any trailing slash.
        var baseAddress = address.TrimEnd('/');

        return new InProcessHost(app, baseAddress);
    }

    /// <summary>
    /// The host service provider, for scenarios that resolve services after startup.
    /// </summary>
    public IServiceProvider Services => _app.Services;

    /// <summary>
    /// Creates an HttpClient configured for this host. The caller owns the client.
    /// </summary>
    public HttpClient CreateClient()
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri(_baseAddress + "/"),
            Timeout = TimeSpan.FromSeconds(10),
        };
        return client;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        try
        {
            await _app.StopAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        }
        finally
        {
            await _app.DisposeAsync().ConfigureAwait(false);
        }
    }
}