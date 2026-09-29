// filepath: dotnet/tests/ApiPilot.AspNetCore.Tests/Diagnostics/ApiPilotActivitySourceTests.cs
// layer: Diagnostics.Tests | package: ApiPilot.AspNetCore.Tests | since: v0.5.0
// purpose: Tests for the ApiPilotActivitySource static class
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : ApiPilotActivitySource, System.Diagnostics
//   Used by    : the test harness
//   See also   : ApiPilotActivitySource.cs, CHANGELOG.md (finding A-212)
// -----------------------------------------------------------------------------

using System.Diagnostics;
using ApiPilot.AspNetCore.Diagnostics;

namespace ApiPilot.AspNetCore.Tests.Diagnostics;

[TestClass]
public sealed class ApiPilotActivitySourceTests
{
    [Test]
    public void Name_IsApiPilot()
    {
        TestAssert.Equal("ApiPilot", ApiPilotActivitySource.Name);
    }

    [Test]
    public void Version_IsTheCurrentAssemblyVersion()
    {
        TestAssert.Equal("1.0.0", ApiPilotActivitySource.Version);
    }

    [Test]
    public void Source_HasExpectedName()
    {
        TestAssert.NotNull(ApiPilotActivitySource.Source);
        TestAssert.Equal("ApiPilot", ApiPilotActivitySource.Source.Name);
    }

    [Test]
    public void StartCsrfValidation_ReturnsNullWithoutListener()
    {
        var activity = ApiPilotActivitySource.StartCsrfValidation("GET");
        TestAssert.Null(activity);
    }

    [Test]
    public void StartCsrfValidation_EmitsActivityWithListener()
    {
        using var listener = CreateListener();
        using var activity = ApiPilotActivitySource.StartCsrfValidation("POST");
        TestAssert.NotNull(activity);
        TestAssert.Equal("apipilot.csrf.validation", activity!.DisplayName);
        TestAssert.Equal("POST", activity.GetTagItem("http.method"));
    }

    [Test]
    public void StartExceptionMapping_EmitsActivityWithListener()
    {
        using var listener = CreateListener();
        using var activity = ApiPilotActivitySource.StartExceptionMapping("VALIDATION_ERROR");
        TestAssert.NotNull(activity);
        TestAssert.Equal("apipilot.exception.mapping", activity!.DisplayName);
        TestAssert.Equal("VALIDATION_ERROR", activity.GetTagItem("apipilot.error.code"));
    }

    [Test]
    public void StartCsrfValidation_DoesNotSetPathTag()
    {
        using var listener = CreateListener();
        using var activity = ApiPilotActivitySource.StartCsrfValidation("GET");
        TestAssert.NotNull(activity);
        TestAssert.Null(activity!.GetTagItem("http.path"));
        TestAssert.Null(activity.GetTagItem("http.url"));
        TestAssert.Null(activity.GetTagItem("url.path"));
    }

    [Test]
    public void StartExceptionMapping_DoesNotSetExceptionTypeTag()
    {
        using var listener = CreateListener();
        using var activity = ApiPilotActivitySource.StartExceptionMapping("INTERNAL_ERROR");
        TestAssert.NotNull(activity);
        TestAssert.Null(activity!.GetTagItem("exception.type"));
        TestAssert.Null(activity.GetTagItem("exception.message"));
    }

    [Test]
    public void StartCsrfValidation_NullMethodThrows()
    {
        TestAssert.Throws<ArgumentNullException>(() => ApiPilotActivitySource.StartCsrfValidation(null!));
    }

    [Test]
    public void StartExceptionMapping_NullCodeThrows()
    {
        TestAssert.Throws<ArgumentNullException>(() => ApiPilotActivitySource.StartExceptionMapping(null!));
    }

    private static ActivityListener CreateListener()
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "ApiPilot",
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllDataAndRecorded,
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }
}

