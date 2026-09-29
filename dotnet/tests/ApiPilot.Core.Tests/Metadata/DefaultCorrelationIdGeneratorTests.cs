// filepath: dotnet/tests/ApiPilot.Core.Tests/Metadata/DefaultCorrelationIdGeneratorTests.cs
// layer: Tests | package: ApiPilot.Core.Tests | since: v0.1.0-alpha.0
// purpose: Contract tests for DefaultCorrelationIdGenerator and the accessor fake
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute, ApiPilot.Core.Metadata,
//                ApiPilot.Core.Tests.Metadata.Fakes
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : DefaultCorrelationIdGenerator.cs, ICorrelationIdAccessor.cs
// -----------------------------------------------------------------------------

using ApiPilot.Core.Metadata;
using ApiPilot.Core.Tests.Metadata.Fakes;

namespace ApiPilot.Core.Tests.Metadata;

/// <summary>
/// Contract tests for DefaultCorrelationIdGenerator. Verifies format,
/// uniqueness, and pattern compliance.
/// </summary>
[TestClass]
public sealed class DefaultCorrelationIdGeneratorTests
{
    /// <summary>NewId returns a non-null value.</summary>
    [Test]
    public void NewId_ReturnsNonNullValue()
    {
        var generator = new DefaultCorrelationIdGenerator();
        TestAssert.NotNull(generator.NewId());
    }

    /// <summary>NewId returns a non-empty value.</summary>
    [Test]
    public void NewId_ReturnsNonEmptyValue()
    {
        var generator = new DefaultCorrelationIdGenerator();
        var id = generator.NewId();
        TestAssert.True(id.Length > 0);
    }

    /// <summary>NewId returns a value in standard GUID format.</summary>
    [Test]
    public void NewId_ReturnsGuidFormat()
    {
        var generator = new DefaultCorrelationIdGenerator();
        var id = generator.NewId();
        TestAssert.Equal(36, id.Length);
        TestAssert.Equal('-', id[8]);
        TestAssert.Equal('-', id[13]);
        TestAssert.Equal('-', id[18]);
        TestAssert.Equal('-', id[23]);
    }

    /// <summary>NewId produces unique values across calls.</summary>
    [Test]
    public void NewId_ProducesUniqueIds()
    {
        var generator = new DefaultCorrelationIdGenerator();
        var ids = new HashSet<string>();
        for (var i = 0; i < 100; i++)
        {
            ids.Add(generator.NewId());
        }
        TestAssert.Equal(100, ids.Count);
    }

    /// <summary>NewId produces values that satisfy the default validation pattern.</summary>
    [Test]
    public void NewId_SatisfiesDefaultValidationPattern()
    {
        var generator = new DefaultCorrelationIdGenerator();
        var validator = new CorrelationIdValidator("^[A-Za-z0-9_-]{8,128}$");
        for (var i = 0; i < 10; i++)
        {
            TestAssert.True(validator.IsValid(generator.NewId()));
        }
    }

    /// <summary>Two instances produce independent unique IDs.</summary>
    [Test]
    public void NewId_TwoInstances_ProduceIndependentIds()
    {
        var a = new DefaultCorrelationIdGenerator();
        var b = new DefaultCorrelationIdGenerator();
        TestAssert.False(a.NewId() == b.NewId());
    }
}

/// <summary>
/// Contract tests for the InMemoryCorrelationIdAccessor test double.
/// </summary>
[TestClass]
public sealed class InMemoryCorrelationIdAccessorTests
{
    /// <summary>RequestId defaults to null.</summary>
    [Test]
    public void RequestId_DefaultsToNull()
    {
        var accessor = new InMemoryCorrelationIdAccessor();
        TestAssert.Null(accessor.RequestId);
    }

    /// <summary>Setting the request ID updates the value.</summary>
    [Test]
    public void SetRequestId_UpdatesValue()
    {
        var accessor = new InMemoryCorrelationIdAccessor { RequestId = "abc-123" };
        TestAssert.Equal("abc-123", accessor.RequestId);
    }

    /// <summary>Setting the request ID to null clears it.</summary>
    [Test]
    public void SetRequestId_Null_ClearsValue()
    {
        var accessor = new InMemoryCorrelationIdAccessor { RequestId = "abc-123" };
        accessor.RequestId = null;
        TestAssert.Null(accessor.RequestId);
    }
}

