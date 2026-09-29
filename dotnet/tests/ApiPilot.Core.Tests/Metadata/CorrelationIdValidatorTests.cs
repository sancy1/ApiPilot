// filepath: dotnet/tests/ApiPilot.Core.Tests/Metadata/CorrelationIdValidatorTests.cs
// layer: Tests | package: ApiPilot.Core.Tests | since: v0.1.0-alpha.0
// purpose: Contract tests for the CorrelationIdValidator
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute, ApiPilot.Core.Metadata
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : CorrelationIdValidator.cs, CorrelationOptionsTests.cs
// -----------------------------------------------------------------------------

using ApiPilot.Core.Metadata;

namespace ApiPilot.Core.Tests.Metadata;

/// <summary>
/// Contract tests for CorrelationIdValidator. Verifies pattern matching
/// at boundary lengths, rejection of invalid characters, and null handling.
/// </summary>
[TestClass]
public sealed class CorrelationIdValidatorTests
{
    private const string DefaultPattern = "^[A-Za-z0-9_-]{8,128}$";

    /// <summary>A standard GUID passes the default pattern.</summary>
    [Test]
    public void IsValid_ValidPatternMatch_ReturnsTrue()
    {
        var validator = new CorrelationIdValidator(DefaultPattern);
        var guid = "01J8Z3K7-X9Y2-F0A6-M3N5-P7Q1R2S3T4U5";
        TestAssert.True(validator.IsValid(guid));
    }

    /// <summary>An 8-character value passes (the minimum).</summary>
    [Test]
    public void IsValid_ShortValidValue_ReturnsTrue()
    {
        var validator = new CorrelationIdValidator(DefaultPattern);
        TestAssert.True(validator.IsValid("abcdef12"));
    }

    /// <summary>A 128-character value passes (the maximum).</summary>
    [Test]
    public void IsValid_LongValidValue_ReturnsTrue()
    {
        var validator = new CorrelationIdValidator(DefaultPattern);
        var value = new string('a', 128);
        TestAssert.True(validator.IsValid(value));
    }

    /// <summary>A 7-character value is rejected.</summary>
    [Test]
    public void IsValid_ValueTooShort_ReturnsFalse()
    {
        var validator = new CorrelationIdValidator(DefaultPattern);
        TestAssert.False(validator.IsValid("abcdef1"));
    }

    /// <summary>A 129-character value is rejected.</summary>
    [Test]
    public void IsValid_ValueTooLong_ReturnsFalse()
    {
        var validator = new CorrelationIdValidator(DefaultPattern);
        var value = new string('a', 129);
        TestAssert.False(validator.IsValid(value));
    }

    /// <summary>Values with spaces or punctuation are rejected.</summary>
    [Test]
    public void IsValid_ValueWithInvalidChars_ReturnsFalse()
    {
        var validator = new CorrelationIdValidator(DefaultPattern);
        TestAssert.False(validator.IsValid("has space here"));
        TestAssert.False(validator.IsValid("semi;colon"));
        TestAssert.False(validator.IsValid("dot.dot.dot"));
    }

    /// <summary>A null value is rejected.</summary>
    [Test]
    public void IsValid_NullValue_ReturnsFalse()
    {
        var validator = new CorrelationIdValidator(DefaultPattern);
        TestAssert.False(validator.IsValid(null));
    }

    /// <summary>A null pattern is rejected by the constructor.</summary>
    [Test]
    public void Constructor_NullPattern_Throws()
    {
        TestAssert.Throws<ArgumentNullException>(() =>
            new CorrelationIdValidator(null!));
    }
}

