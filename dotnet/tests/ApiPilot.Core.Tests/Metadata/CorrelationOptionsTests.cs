// filepath: dotnet/tests/ApiPilot.Core.Tests/Metadata/CorrelationOptionsTests.cs
// layer: Tests | package: ApiPilot.Core.Tests | since: v0.1.0-alpha.0
// purpose: Contract tests for the CorrelationOptions data holder
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute, ApiPilot.Core.Metadata
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : CorrelationOptions.cs, CorrelationIdValidatorTests.cs
// -----------------------------------------------------------------------------

using ApiPilot.Core.Metadata;

namespace ApiPilot.Core.Tests.Metadata;

/// <summary>
/// Contract tests for CorrelationOptions. Verifies defaults and mutability.
/// </summary>
[TestClass]
public sealed class CorrelationOptionsTests
{
    /// <summary>Default values match the documented defaults.</summary>
    [Test]
    public void Defaults_AreSensible()
    {
        var options = new CorrelationOptions();
        TestAssert.Equal("X-Request-Id", options.HeaderName);
        TestAssert.True(options.EchoInResponseBody);
        TestAssert.True(options.EchoInResponseHeader);
        TestAssert.True(options.ValidateIncoming);
        TestAssert.Equal("^[A-Za-z0-9_-]{8,128}$", options.ValidationPattern);
    }

    /// <summary>Properties can be updated after construction.</summary>
    [Test]
    public void Properties_AreMutable()
    {
        var options = new CorrelationOptions();
        options.HeaderName = "X-Correlation-Id";
        options.EchoInResponseBody = false;
        options.EchoInResponseHeader = false;
        options.ValidateIncoming = false;
        options.ValidationPattern = "^.{1,256}$";
        TestAssert.Equal("X-Correlation-Id", options.HeaderName);
        TestAssert.False(options.EchoInResponseBody);
        TestAssert.False(options.EchoInResponseHeader);
        TestAssert.False(options.ValidateIncoming);
        TestAssert.Equal("^.{1,256}$", options.ValidationPattern);
    }

    /// <summary>Defaults can be overridden via object initializer.</summary>
    [Test]
    public void Defaults_CanBeOverridden()
    {
        var options = new CorrelationOptions
        {
            HeaderName = "Trace-Id",
            ValidateIncoming = false
        };
        TestAssert.Equal("Trace-Id", options.HeaderName);
        TestAssert.False(options.ValidateIncoming);
    }

    /// <summary>The default pattern matches a standard hyphenated GUID.</summary>
    [Test]
    public void DefaultPattern_MatchesGuid()
    {
        var options = new CorrelationOptions();
        var validator = new CorrelationIdValidator(options.ValidationPattern);
        TestAssert.True(validator.IsValid("01J8Z3K7-X9Y2-F0A6-M3N5-P7Q1R2S3T4U5"));
    }

    /// <summary>The default pattern rejects short values.</summary>
    [Test]
    public void DefaultPattern_RejectsShortValues()
    {
        var options = new CorrelationOptions();
        var validator = new CorrelationIdValidator(options.ValidationPattern);
        TestAssert.False(validator.IsValid("abc"));
    }
}

