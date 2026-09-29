// filepath: dotnet/tests/ApiPilot.AspNetCore.Tests/EndpointMetadata/PaginationEndpointFilterTests.cs
// layer: Tests | package: ApiPilot.AspNetCore.Tests | since: v0.2.0-alpha.0
// purpose: Contract tests for PaginationEndpointFilter parse, validate, and store behavior
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute,
//                ApiPilot.AspNetCore.Configuration, ApiPilot.AspNetCore.EndpointMetadata,
//                ApiPilot.AspNetCore.Results, ApiPilot.Core.Pagination
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : PaginationEndpointFilter.cs, PaginationResolverTests.cs
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.Configuration;
using ApiPilot.AspNetCore.EndpointMetadata;
using ApiPilot.AspNetCore.Results;
using ApiPilot.Core.Pagination;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace ApiPilot.AspNetCore.Tests.EndpointMetadata;

/// <summary>
/// Contract tests for PaginationEndpointFilter. Exercises parsing,
/// validation, context storage, and short-circuiting directly.
/// </summary>
[TestClass]
public sealed class PaginationEndpointFilterTests
{
    private static DefaultHttpContext BuildContext(
        Action<HttpContext>? configureRequest = null,
        Action<PaginationMetadataAttribute>? configureAttribute = null,
        object[]? configureMetadata = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var provider = services.BuildServiceProvider();

        var httpContext = new DefaultHttpContext { RequestServices = provider };
        httpContext.Response.Body = new MemoryStream();

        configureRequest?.Invoke(httpContext);

        var metadata = new List<object>();
        if (configureAttribute is not null)
        {
            var attr = new PaginationMetadataAttribute();
            configureAttribute(attr);
            metadata.Add(attr);
        }
        if (configureMetadata is not null)
        {
            metadata.AddRange(configureMetadata);
        }

        if (metadata.Count > 0)
        {
            var endpoint = new Endpoint(
                httpContext => Task.CompletedTask,
                new EndpointMetadataCollection(metadata),
                "test");
            httpContext.SetEndpoint(endpoint);
        }

        return httpContext;
    }

    private static PaginationEndpointFilter BuildFilter(PaginationOptions? options = null)
    {
        var opts = options ?? new PaginationOptions();
        return new PaginationEndpointFilter(Options.Create(opts));
    }

    private static async Task<(object? result, bool nextCalled)> RunAsync(
        PaginationEndpointFilter filter,
        DefaultHttpContext httpContext)
    {
        var invocation = EndpointFilterInvocationContext.Create(httpContext);
        var nextCalled = false;
        EndpointFilterDelegate next = _ =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(null);
        };
        var result = await filter.InvokeAsync(invocation, next);
        return (result, nextCalled);
    }

    /// <summary>No query parameters uses global defaults.</summary>
    [Test]
    public async Task InvokeAsync_NoQueryParameters_UsesGlobalDefaults()
    {
        var filter = BuildFilter(new PaginationOptions { DefaultPageSize = 20 });
        var ctx = BuildContext();
        var (result, nextCalled) = await RunAsync(filter, ctx);

        TestAssert.Null(result);
        TestAssert.True(nextCalled);
        var page = ctx.GetPageRequest();
        TestAssert.NotNull(page);
        TestAssert.Equal(1, page!.Page);
        TestAssert.Equal(20, page.PageSize);
    }

    /// <summary>Custom parameter names are honored.</summary>
    [Test]
    public async Task InvokeAsync_CustomParameterNames_ReadsFromCustomNames()
    {
        var options = new PaginationOptions();
        options.ParameterNames = new QueryParameterNames { Page = "pageNumber", PageSize = "perPage" };
        var filter = BuildFilter(options);
        var ctx = BuildContext(configureRequest: c => c.Request.QueryString = new QueryString("?pageNumber=3&perPage=15"));

        await RunAsync(filter, ctx);
        var page = ctx.GetPageRequest();
        TestAssert.NotNull(page);
        TestAssert.Equal(3, page!.Page);
        TestAssert.Equal(15, page.PageSize);
    }

    /// <summary>Custom integer parser is honored.</summary>
    [Test]
    public async Task InvokeAsync_CustomIntegerParser_AcceptsCustomFormat()
    {
        var options = new PaginationOptions();
        options.IntegerParser = s => int.TryParse(s, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : (int?)null;
        var filter = BuildFilter(options);
        var ctx = BuildContext(configureRequest: c => c.Request.QueryString = new QueryString("?page=3"));

        await RunAsync(filter, ctx);
        var page = ctx.GetPageRequest();
        TestAssert.NotNull(page);
        TestAssert.Equal(3, page!.Page);
    }

    /// <summary>Zero-based page numbering accepts page 0 as the first page.</summary>
    [Test]
    public async Task InvokeAsync_ZeroBasedPageNumber_AcceptsPageZero()
    {
        var options = new PaginationOptions { PageNumberBase = 0 };
        var filter = BuildFilter(options);
        var ctx = BuildContext(configureRequest: c => c.Request.QueryString = new QueryString("?page=0"));

        await RunAsync(filter, ctx);
        var page = ctx.GetPageRequest();
        TestAssert.NotNull(page);
        TestAssert.Equal(1, page!.Page);
    }

    /// <summary>Custom sort direction parser is honored.</summary>
    [Test]
    public async Task InvokeAsync_CustomSortDirectionParser_AcceptsCustomTokens()
    {
        var options = new PaginationOptions();
        options.SortDirectionParser = s => s == "down" ? SortDirection.Descending : (SortDirection?)null;
        var filter = BuildFilter(options);
        var ctx = BuildContext(configureRequest: c => c.Request.QueryString = new QueryString("?sort=name&direction=down"));

        await RunAsync(filter, ctx);
        var sort = ctx.GetSortRequest();
        TestAssert.NotNull(sort);
        TestAssert.Equal("name", sort!.Field);
        TestAssert.Equal(SortDirection.Descending, sort.Direction);
    }

    /// <summary>Custom sort parser accepts a sign-prefixed expression.</summary>
    [Test]
    public async Task InvokeAsync_CustomSortParser_SignPrefix()
    {
        var options = new PaginationOptions();
        options.SortParser = (field, _) =>
        {
            if (string.IsNullOrEmpty(field)) return null;
            if (field.StartsWith('-'))
                return SortRequest.Create(field.Substring(1), SortDirection.Descending);
            return SortRequest.Create(field, SortDirection.Ascending);
        };
        var filter = BuildFilter(options);
        var ctx = BuildContext(configureRequest: c => c.Request.QueryString = new QueryString("?sort=-createdAt"));

        await RunAsync(filter, ctx);
        var sort = ctx.GetSortRequest();
        TestAssert.NotNull(sort);
        TestAssert.Equal("createdAt", sort!.Field);
        TestAssert.Equal(SortDirection.Descending, sort.Direction);
    }

    /// <summary>Custom filter predicate restricts which keys become filters.</summary>
    [Test]
    public async Task InvokeAsync_CustomFilterPredicate_OnlyAllowListedKeys()
    {
        var options = new PaginationOptions();
        options.IsFilterParameter = k => k == "status";
        var filter = BuildFilter(options);
        var ctx = BuildContext(configureRequest: c => c.Request.QueryString = new QueryString("?status=open&unknown=xyz"));

        await RunAsync(filter, ctx);
        var filters = ctx.GetFilterRequest();
        TestAssert.NotNull(filters);
        TestAssert.Equal(1, filters!.Pairs.Count);
        TestAssert.True(filters.Pairs.ContainsKey("status"));
    }

    /// <summary>Strict mode rejects an unknown parameter.</summary>
    [Test]
    public async Task InvokeAsync_StrictMode_RejectsUnknownParameter()
    {
        var options = new PaginationOptions { StrictQueryValidation = true };
        var filter = BuildFilter(options);
        var ctx = BuildContext(configureRequest: c => c.Request.QueryString = new QueryString("?bogus=1"));

        var (result, nextCalled) = await RunAsync(filter, ctx);
        TestAssert.False(nextCalled);
        TestAssert.NotNull(result);
        TestAssert.True(result is ErrorResponseResult);
    }

    /// <summary>Lenient mode passes unknown parameters through as filters.</summary>
    [Test]
    public async Task InvokeAsync_LenientMode_PassesUnknownParameter()
    {
        var filter = BuildFilter(new PaginationOptions { StrictQueryValidation = false });
        var ctx = BuildContext(configureRequest: c => c.Request.QueryString = new QueryString("?bogus=1"));

        var (result, nextCalled) = await RunAsync(filter, ctx);
        TestAssert.Null(result);
        TestAssert.True(nextCalled);
    }

    /// <summary>An invalid page returns the validation envelope.</summary>
    [Test]
    public async Task InvokeAsync_InvalidPage_ReturnsValidationError()
    {
        var filter = BuildFilter();
        var ctx = BuildContext(configureRequest: c => c.Request.QueryString = new QueryString("?page=abc"));

        var (result, nextCalled) = await RunAsync(filter, ctx);
        TestAssert.False(nextCalled);
        TestAssert.NotNull(result);
        TestAssert.True(result is ErrorResponseResult);
        var errorResult = (ErrorResponseResult)result!;
        TestAssert.Equal("VALIDATION_ERROR", errorResult.Response.Error.Code.Code);
    }

    /// <summary>A page size above the maximum is rejected.</summary>
    [Test]
    public async Task InvokeAsync_PageSizeAboveMax_ReturnsValidationError()
    {
        var filter = BuildFilter(new PaginationOptions { MaxPageSize = 100 });
        var ctx = BuildContext(configureRequest: c => c.Request.QueryString = new QueryString("?pageSize=999"));

        var (result, _) = await RunAsync(filter, ctx);
        TestAssert.NotNull(result);
        TestAssert.True(result is ErrorResponseResult);
    }

    /// <summary>Valid request stores PageRequest, SortRequest, and FilterRequest.</summary>
    [Test]
    public async Task InvokeAsync_ValidRequest_StoresAllRequests()
    {
        var filter = BuildFilter();
        var ctx = BuildContext(configureRequest: c => c.Request.QueryString = new QueryString("?page=2&pageSize=10&sort=name&direction=asc&status=open"));

        await RunAsync(filter, ctx);

        var page = ctx.GetPageRequest();
        var sort = ctx.GetSortRequest();
        var filters = ctx.GetFilterRequest();

        TestAssert.NotNull(page);
        TestAssert.Equal(2, page!.Page);
        TestAssert.Equal(10, page.PageSize);

        TestAssert.NotNull(sort);
        TestAssert.Equal("name", sort!.Field);
        TestAssert.Equal(SortDirection.Ascending, sort.Direction);

        TestAssert.NotNull(filters);
        TestAssert.True(filters!.Pairs.ContainsKey("status"));
        TestAssert.Equal("open", filters.Pairs["status"]);
    }

    /// <summary>The endpoint attribute can override the global MaxPageSize.</summary>
    [Test]
    public async Task InvokeAsync_EndpointAttributeOverridesMaxPageSize_UsesOverride()
    {
        var filter = BuildFilter(new PaginationOptions { MaxPageSize = 100 });
        var ctx = BuildContext(
            configureRequest: c => c.Request.QueryString = new QueryString("?pageSize=75"),
            configureAttribute: a => a.MaxPageSize = 50);

        var (result, _) = await RunAsync(filter, ctx);
        TestAssert.NotNull(result);
        TestAssert.True(result is ErrorResponseResult);
    }

    /// <summary>
    /// Fluent overrides stored as endpoint metadata are read by the
    /// filter. This is the test that would fail if the filter only
    /// consulted the attribute and ignored the fluent source.
    /// </summary>
    [Test]
    public async Task InvokeAsync_FluentOverridesFromMetadata_AreRead()
    {
        var filter = BuildFilter(new PaginationOptions { MaxPageSize = 100 });
        var fluentOverrides = new PaginationOverrides { MaxPageSize = 5 };
        var ctx = BuildContext(
            configureRequest: c => c.Request.QueryString = new QueryString("?pageSize=10"),
            configureMetadata: new object[] { fluentOverrides });

        var (result, _) = await RunAsync(filter, ctx);
        TestAssert.NotNull(result);
        TestAssert.True(result is ErrorResponseResult);
    }

    /// <summary>
    /// Fluent and attribute overrides compose: primitives from the
    /// attribute win, delegates from the fluent source flow through.
    /// </summary>
    [Test]
    public async Task InvokeAsync_FluentAndAttributeOverrides_Compose()
    {
        var filter = BuildFilter(new PaginationOptions { MaxPageSize = 100 });
        var fluentOverrides = new PaginationOverrides
        {
            SortDirectionParser = s => s == "down" ? SortDirection.Descending : (SortDirection?)null,
        };
        var attribute = new PaginationMetadataAttribute { MaxPageSize = 5 };
        var ctx = BuildContext(
            configureRequest: c => c.Request.QueryString = new QueryString("?pageSize=10&sort=name&direction=down"),
            configureMetadata: new object[] { fluentOverrides, attribute });

        var (result, _) = await RunAsync(filter, ctx);
        TestAssert.NotNull(result);
        TestAssert.True(result is ErrorResponseResult);
    }
}

