// filepath: dotnet/tests/ApiPilot.AspNetCore.Tests/Validation/ApiPilotValidationFilterTests.cs
// layer: Tests | package: ApiPilot.AspNetCore.Tests | since: v0.2.0-alpha.0
// purpose: Unit tests for the internal ApiPilotValidationFilter
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute,
//                ApiPilot.AspNetCore.Configuration, ApiPilot.AspNetCore.Results,
//                ApiPilot.AspNetCore.Validation, Microsoft.Extensions.Options
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : ApiPilotValidationFilter.cs, ApiPilotValidateAttribute.cs
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.Configuration;
using ApiPilot.AspNetCore.Results;
using ApiPilot.AspNetCore.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using System.IO;

namespace ApiPilot.AspNetCore.Tests.Validation;

/// <summary>
/// Unit tests for the internal ApiPilotValidationFilter. Exercises the
/// filter with a fake ActionExecutingContext and a controllable
/// ApiPilotValidationOptions instance.
/// </summary>
[TestClass]
public sealed class ApiPilotValidationFilterTests
{
    private static ApiPilotValidationFilter BuildFilter(
        Action<ApiPilotValidationOptions>? configure = null)
    {
        var options = new ApiPilotValidationOptions();
        configure?.Invoke(options);
        var exceptionOptions = Options.Create(new ApiExceptionOptions());
        return new ApiPilotValidationFilter(Options.Create(options), exceptionOptions);
    }

    private static ActionExecutingContext BuildContext(
        Action<Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary>? configureState = null)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor());

        var context = new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            new object());

        configureState?.Invoke(context.ModelState);
        return context;
    }

    /// <summary>A valid model state does not set a result.</summary>
    [Test]
    public void OnActionExecuting_ValidModelState_DoesNothing()
    {
        var filter = BuildFilter();
        var context = BuildContext();
        filter.OnActionExecuting(context);
        TestAssert.Null(context.Result);
    }

    /// <summary>An invalid model state sets an ErrorResponseResult.</summary>
    [Test]
    public void OnActionExecuting_InvalidModelState_SetsErrorResult()
    {
        var filter = BuildFilter();
        var context = BuildContext(s => s.AddModelError("Email", "Required."));
        filter.OnActionExecuting(context);
        TestAssert.NotNull(context.Result);
        TestAssert.True(context.Result is ErrorResponseResult);
    }

    /// <summary>With no configured transform, the default camelCase normalizer applies.</summary>
    [Test]
    public void OnActionExecuting_InvalidModelState_UsesDefaultKeyTransform()
    {
        var filter = BuildFilter();
        var context = BuildContext(s => s.AddModelError("Items[0].Price", "Invalid."));
        filter.OnActionExecuting(context);
        TestAssert.NotNull(context.Result);

        var errorResponse = ((ErrorResponseResult)context.Result!).Response;
        TestAssert.NotNull(errorResponse.Error.Fields);
        TestAssert.True(errorResponse.Error.Fields!.ContainsKey("items[0].price"));
    }

    /// <summary>A configured transform is applied.</summary>
    [Test]
    public void OnActionExecuting_InvalidModelState_UsesConfiguredKeyTransform()
    {
        var filter = BuildFilter(o => o.KeyTransform = k => "custom." + k);
        var context = BuildContext(s => s.AddModelError("Email", "Required."));
        filter.OnActionExecuting(context);
        TestAssert.NotNull(context.Result);

        var errorResponse = ((ErrorResponseResult)context.Result!).Response;
        TestAssert.NotNull(errorResponse.Error.Fields);
        TestAssert.True(errorResponse.Error.Fields!.ContainsKey("custom.Email"));
    }

    /// <summary>The identity transform leaves keys unchanged.</summary>
    [Test]
    public void OnActionExecuting_InvalidModelState_IdentityTransform_PreservesKeys()
    {
        var filter = BuildFilter(o => o.KeyTransform = k => k);
        var context = BuildContext(s => s.AddModelError("Email", "Required."));
        filter.OnActionExecuting(context);
        TestAssert.NotNull(context.Result);

        var errorResponse = ((ErrorResponseResult)context.Result!).Response;
        TestAssert.NotNull(errorResponse.Error.Fields);
        TestAssert.True(errorResponse.Error.Fields!.ContainsKey("Email"));
    }

    /// <summary>Null options is rejected.</summary>
    [Test]
    public void Constructor_RejectsNullOptions()
    {
        var exceptionOptions = Options.Create(new ApiExceptionOptions());
        TestAssert.Throws<ArgumentNullException>(() =>
            new ApiPilotValidationFilter(null!, exceptionOptions));
    }
}

