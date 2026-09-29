// filepath: dotnet/samples/Samples.Api/Program.cs
// layer: Samples | package: n/a (standalone sample) | since: v0.6.0
// purpose: A standalone reference API exercising the ApiPilot envelope, CSRF, correlation, and rate-limit paths
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (top-level program)
//   Depends on : ApiPilot.AspNetCore.Middleware, ApiPilot.AspNetCore.Serialization,
//                ApiPilot.AspNetCore.RateLimiting, ApiPilot.AspNetCore.EndpointMetadata,
//                ApiPilot.AspNetCore.Results, ApiPilot.Core.Errors, ApiPilot.Core.Metadata,
//                ApiPilot.Core.Responses, ApiPilot.Security.Csrf
//   Used by    : the sample runner
//   See also   : README.md, docs/openapi.md, docs/rate-limiting.md
// -----------------------------------------------------------------------------
//
// WHAT THIS SAMPLE SHOWS
//   A single ApiPilot host that exercises the representative response
//   paths: success, error, validation, pagination, CSRF bootstrap and
//   protection, correlation, and a real platform rate-limit rejection.
//   It is the target for the Phase 5 contract tests and browser tests.
//
// HOST URL
//   The URL is not hard-coded. Set ASPNETCORE_URLS or pass --urls on
//   the command line. The default below applies only when neither is
//   supplied.

using ApiPilot.AspNetCore.EndpointMetadata;
using ApiPilot.AspNetCore.Middleware;
using ApiPilot.AspNetCore.RateLimiting;
using ApiPilot.AspNetCore.Results;
using ApiPilot.AspNetCore.Serialization;
using ApiPilot.Core.Errors;
using ApiPilot.Core.Metadata;
using ApiPilot.Core.Responses;
using ApiPilot.Security.Csrf;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Safe default URL, overridable through ASPNETCORE_URLS or --urls.
if (string.IsNullOrEmpty(builder.Configuration["urls"]))
{
    builder.WebHost.UseUrls("http://127.0.0.1:5090");
}

// Data Protection is required by the CSRF signer. A minimal host does
// not register it by default; a web host normally does. Register it
// before AddApiPilotCsrf.
builder.Services.AddDataProtection();

// ApiPilot registrations.
builder.Services.AddApiPilotJson();
builder.Services.AddApiPilotCorrelation();
builder.Services.AddApiPilotExceptions();
builder.Services.AddApiPilotCsrf(o =>
{
    o.PreAuthBindingSource = _ => "sample-binding";
});
builder.Services.AddApiPilotRateLimitRejection();

// The application owns the platform limiter. ApiPilot supplies only
// the rejection handler and its options.
builder.Services.AddRateLimiter(o =>
{
    o.OnRejected = RateLimitDiagnosticsHook.HandleAsync;
    o.AddFixedWindowLimiter("sample", w =>
    {
        w.PermitLimit = 3;
        w.Window = TimeSpan.FromSeconds(30);
        w.QueueLimit = 0;
    });
});

var app = builder.Build();

// Pipeline order: correlation, exceptions, routing, CSRF, rate limiter, endpoints.
app.UseApiPilotCorrelation();
app.UseApiPilotExceptions();
app.UseRouting();
app.UseApiPilotCsrfProtection();
app.UseRateLimiter();

// Representative success.
app.MapGet("/api/ok", (HttpContext context) =>
{
    var meta = BuildMetadata(context);
    return ApiResponseBuilder.Ok(new { id = "ORD-10001" }, meta).ToResult();
});

// Representative error (mapped by the exception middleware).
app.MapGet("/api/fail", () => { throw new InvalidOperationException("sample failure"); });

// Representative validation failure (the validation error envelope).
app.MapPost("/api/validate", (HttpContext context, SampleDto dto) =>
{
    if (string.IsNullOrWhiteSpace(dto.Email))
    {
        var meta = BuildMetadata(context);
        var field = ApiErrorField.WithMessage("email", "Email is required.");
        var error = ApiError.Create(
            ApiErrorCode.ValidationError,
            "One or more values are invalid.",
            new[] { field });
        return new ErrorResponse(error, meta).ToResult();
    }
    var okMeta = BuildMetadata(context);
    return ApiResponseBuilder.Ok(new { email = dto.Email }, okMeta).ToResult();
});

// Representative paginated endpoint. The pagination filter populates
// the validated request; the response uses the success envelope with the
// pagination object inside the payload.
app.MapGet("/api/items", (HttpContext context) =>
{
    var request = context.GetPageRequest();
    var pageNumber = request?.Page ?? 1;
    var size = request?.PageSize ?? 20;
    var items = new[] { new { id = 1, name = "Widget" }, new { id = 2, name = "Gadget" } };
    var total = items.Length;
    var payload = new
    {
        items,
        pagination = new
        {
            page = pageNumber,
            pageSize = size,
            totalItems = total,
            totalPages = 1,
            hasNext = false,
            hasPrevious = false
        }
    };
    var meta = BuildMetadata(context);
    return ApiResponseBuilder.Ok(payload, meta).ToResult();
})
.WithApiPilotPagination();

// CSRF bootstrap endpoint (GET, returns the bare { "token": "..." }).
app.MapApiPilotCsrf();

// Protected unsafe endpoint (requires the CSRF header).
app.MapPost("/api/submit", (HttpContext context) =>
{
    var meta = BuildMetadata(context);
    return ApiResponseBuilder.Ok(new { submitted = true }, meta).ToResult();
});

// Deterministic rate-limited endpoint: exceeds the fixed-window limit
// after 3 requests in 30 seconds.
app.MapGet("/api/limited", (HttpContext context) =>
{
    var meta = BuildMetadata(context);
    return ApiResponseBuilder.Ok(new { ok = true }, meta).ToResult();
})
   .RequireRateLimiting("sample");

app.Run();

// Builds response metadata using the correlation accessor when present,
// falling back to the request TraceIdentifier. This mirrors the
// correlation seam the ApiPilot middleware uses.
static ResponseMetadata BuildMetadata(HttpContext context)
{
    var accessor = context.RequestServices.GetService<ICorrelationIdAccessor>();
    var requestId = accessor?.RequestId;
    if (string.IsNullOrEmpty(requestId))
    {
        requestId = context.TraceIdentifier;
    }
    return new ResponseMetadata { RequestId = requestId };
}

// Top-level-statement files place their implicit Program class in the
// global namespace. A supporting DTO referenced by the top-level code
// must therefore also live in the global namespace. CA1050 (declare
// types in namespaces) is scoped off for this declaration only, with
// this comment as the justification.
#pragma warning disable CA1050
/// <summary>A minimal sample DTO for the validation endpoint.</summary>
public sealed record SampleDto
{
    /// <summary>The email address. Required for the sample validation path.</summary>
    public string? Email { get; init; }
}
#pragma warning restore CA1050

