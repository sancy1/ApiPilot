// filepath: dotnet/samples/Samples.MultiInstance/Program.cs
// layer: Samples | package: n/a (standalone sample) | since: v0.5.0
// purpose: A minimal two-instance ApiPilot demonstration using a shared Data Protection key ring
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (top-level program)
//   Depends on : ApiPilot.Security.DataProtection, ApiPilot.Security.Csrf,
//                ApiPilot.AspNetCore.Serialization, ApiPilot.AspNetCore.Middleware
//   Used by    : the sample runner
//   See also   : README.md, docs/multi-instance.md
// -----------------------------------------------------------------------------
//
// WHAT THIS SAMPLE SHOWS
//   Two ApiPilot instances configured with the SAME persisted Data
//   Protection key ring and the SAME application name interoperate: a
//   CSRF token issued by instance A validates on instance B. With
//   separate key rings, instance B rejects instance A token with the
//   stable CSRF_TOKEN_INVALID wire code.

using ApiPilot.AspNetCore.Middleware;
using ApiPilot.AspNetCore.Serialization;
using ApiPilot.Security.Csrf;
using ApiPilot.Security.DataProtection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

// The shared application name is required for cross-instance payload
// interoperability, together with the shared key ring.
const string ApplicationName = "ApiPilot.Samples.MultiInstance";

// A single shared key-ring directory on disk.
var keyRingDirectory = Path.Combine(Path.GetTempPath(), "apipilot-sample-keys-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(keyRingDirectory);
Console.WriteLine("Key ring: " + keyRingDirectory);

try
{
    await using var hostA = await StartInstanceAsync("http://127.0.0.1:5081", keyRingDirectory, ApplicationName);
    await using var hostB = await StartInstanceAsync("http://127.0.0.1:5082", keyRingDirectory, ApplicationName);

    using var client = new HttpClient();

    // Instance A issues a bootstrap token.
    var tokenJson = await client.GetStringAsync("http://127.0.0.1:5081/api/csrf");
    using var tokenDoc = JsonDocument.Parse(tokenJson);
    var token = tokenDoc.RootElement.GetProperty("token").GetString()!;
    Console.WriteLine("Instance A issued a token; sending it to instance B...");

    // Instance B validates the token A issued (shared key ring + app name).
    var request = new HttpRequestMessage(HttpMethod.Post, "http://127.0.0.1:5082/submit");
    request.Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
    request.Headers.Add("X-CSRF-TOKEN", token);
    var response = await client.SendAsync(request);
    Console.WriteLine("Instance B response: " + (int)response.StatusCode);
    Console.WriteLine("Expected: 200 (the shared key ring validated the token).");
}
finally
{
    try { Directory.Delete(keyRingDirectory, recursive: true); } catch { }
}

static async Task<WebApplication> StartInstanceAsync(string url, string keyRingDirectory, string applicationName)
{
    var builder = WebApplication.CreateBuilder();
    builder.WebHost.UseUrls(url);

    builder.Services.AddApiPilotJson();
    builder.Services.AddApiPilotCorrelation();
    builder.Services.AddApiPilotDataProtection(o =>
    {
        o.ApplicationName = applicationName;
        o.MultiInstance = true;
        o.KeyStorage = dp => dp.PersistKeysToFileSystem(new DirectoryInfo(keyRingDirectory));
    });
    builder.Services.AddApiPilotCsrf(o =>
    {
        o.PreAuthBindingSource = _ => "sample-binding";
    });

    var app = builder.Build();
    app.UseApiPilotCorrelation();
    app.UseRouting();
    app.UseApiPilotCsrfProtection();
    app.MapApiPilotCsrf();
    app.MapPost("/submit", () => "ok");

    await app.StartAsync();
    return app;
}

