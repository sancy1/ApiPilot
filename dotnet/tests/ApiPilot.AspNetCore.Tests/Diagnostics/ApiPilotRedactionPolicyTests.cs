// filepath: dotnet/tests/ApiPilot.AspNetCore.Tests/Diagnostics/ApiPilotRedactionPolicyTests.cs
// layer: Diagnostics.Tests | package: ApiPilot.AspNetCore.Tests | since: v0.5.0
// purpose: Tests for the ApiPilotRedactionPolicy named-field redaction rules
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : ApiPilotRedactionPolicy
//   Used by    : the test harness
//   See also   : ApiPilotRedactionPolicy.cs
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.Diagnostics;

namespace ApiPilot.AspNetCore.Tests.Diagnostics;

[TestClass]
public sealed class ApiPilotRedactionPolicyTests
{
    [Test]
    public void RedactedNames_ContainsExactlySevenEntries()
    {
        TestAssert.Equal(7, ApiPilotRedactionPolicy.RedactedNames.Count);
    }

    [Test]
    public void RedactedNames_ContainsTheSevenCanonicalNames()
    {
        TestAssert.True(ApiPilotRedactionPolicy.RedactedNames.Contains("Authorization"));
        TestAssert.True(ApiPilotRedactionPolicy.RedactedNames.Contains("Cookie"));
        TestAssert.True(ApiPilotRedactionPolicy.RedactedNames.Contains("Set-Cookie"));
        TestAssert.True(ApiPilotRedactionPolicy.RedactedNames.Contains("X-CSRF-TOKEN"));
        TestAssert.True(ApiPilotRedactionPolicy.RedactedNames.Contains("X-XSRF-TOKEN"));
        TestAssert.True(ApiPilotRedactionPolicy.RedactedNames.Contains("Proxy-Authorization"));
        TestAssert.True(ApiPilotRedactionPolicy.RedactedNames.Contains("WWW-Authenticate"));
    }

    [Test]
    public void RedactedNames_ComparisonIsCaseInsensitive()
    {
        TestAssert.True(ApiPilotRedactionPolicy.RedactedNames.Contains("AUTHORIZATION"));
        TestAssert.True(ApiPilotRedactionPolicy.RedactedNames.Contains("authorization"));
        TestAssert.True(ApiPilotRedactionPolicy.RedactedNames.Contains("x-csrf-token"));
    }

    [Test]
    public void RedactHeader_RedactsEachCanonicalName()
    {
        TestAssert.Equal("[redacted]", ApiPilotRedactionPolicy.RedactHeader("Authorization", "Bearer xyz"));
        TestAssert.Equal("[redacted]", ApiPilotRedactionPolicy.RedactHeader("Cookie", "session=abc"));
        TestAssert.Equal("[redacted]", ApiPilotRedactionPolicy.RedactHeader("Set-Cookie", "session=abc"));
        TestAssert.Equal("[redacted]", ApiPilotRedactionPolicy.RedactHeader("X-CSRF-TOKEN", "token-value"));
        TestAssert.Equal("[redacted]", ApiPilotRedactionPolicy.RedactHeader("X-XSRF-TOKEN", "token-value"));
        TestAssert.Equal("[redacted]", ApiPilotRedactionPolicy.RedactHeader("Proxy-Authorization", "Basic zzz"));
        TestAssert.Equal("[redacted]", ApiPilotRedactionPolicy.RedactHeader("WWW-Authenticate", "Basic realm=x"));
    }

    [Test]
    public void RedactHeader_IsCaseInsensitiveOnName()
    {
        TestAssert.Equal("[redacted]", ApiPilotRedactionPolicy.RedactHeader("AUTHORIZATION", "Bearer xyz"));
        TestAssert.Equal("[redacted]", ApiPilotRedactionPolicy.RedactHeader("authorization", "Bearer xyz"));
        TestAssert.Equal("[redacted]", ApiPilotRedactionPolicy.RedactHeader("x-csrf-token", "token"));
    }

    [Test]
    public void RedactHeader_PassesThroughNonRedactedName()
    {
        TestAssert.Equal("application/json", ApiPilotRedactionPolicy.RedactHeader("Content-Type", "application/json"));
        TestAssert.Equal("GET", ApiPilotRedactionPolicy.RedactHeader("Method", "GET"));
    }

    [Test]
    public void RedactHeader_NullValueBecomesEmptyString()
    {
        TestAssert.Equal(string.Empty, ApiPilotRedactionPolicy.RedactHeader("Content-Type", null));
        TestAssert.Equal("[redacted]", ApiPilotRedactionPolicy.RedactHeader("Authorization", null));
    }

    [Test]
    public void RedactHeader_NullNameThrows()
    {
        TestAssert.Throws<ArgumentNullException>(
            () => ApiPilotRedactionPolicy.RedactHeader(null!, "value"));
    }

    [Test]
    public void RedactValue_BehavesLikeRedactHeader()
    {
        TestAssert.Equal("[redacted]", ApiPilotRedactionPolicy.RedactValue("Cookie", "session=abc"));
        TestAssert.Equal("[redacted]", ApiPilotRedactionPolicy.RedactValue("X-CSRF-TOKEN", "token"));
        TestAssert.Equal("plain", ApiPilotRedactionPolicy.RedactValue("Detail", "plain"));
    }

    [Test]
    public void RedactValue_NullNameThrows()
    {
        TestAssert.Throws<ArgumentNullException>(
            () => ApiPilotRedactionPolicy.RedactValue(null!, "value"));
    }

    [Test]
    public void RedactedValue_IsTheExactLiteral()
    {
        TestAssert.Equal("[redacted]", ApiPilotRedactionPolicy.RedactedValue);
    }
}

