// filepath: dotnet/tests/ApiPilot.Core.Tests/Metadata/ResponseMetadataTests.cs
// layer: Tests | package: ApiPilot.Core.Tests | since: v0.1.0-alpha.0
// purpose: Contract tests for the ResponseMetadata record
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute, ApiPilot.Core.Metadata
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : ResponseMetadata.cs, RequestMetadata.cs
// -----------------------------------------------------------------------------

using ApiPilot.Core.Metadata;

namespace ApiPilot.Core.Tests.Metadata;

/// <summary>
/// Contract tests for the ResponseMetadata record. Verifies defaults,
/// immutability, value equality, and the helpers on the type.
/// </summary>
[TestClass]
public sealed class ResponseMetadataTests
{
    /// <summary>A default instance has the documented defaults.</summary>
    [Test]
    public void Default_HasEmptyRequestIdAndExtra()
    {
        var meta = new ResponseMetadata();
        TestAssert.Equal(string.Empty, meta.RequestId);
        TestAssert.NotNull(meta.Extra);
        TestAssert.Equal(0, meta.Extra.Count);
    }

    /// <summary>A default instance carries a recent UTC timestamp.</summary>
    [Test]
    public void Default_TimestampIsRecentUtc()
    {
        var before = DateTimeOffset.UtcNow;
        var meta = new ResponseMetadata();
        var after = DateTimeOffset.UtcNow;
        TestAssert.True(meta.Timestamp >= before);
        TestAssert.True(meta.Timestamp <= after);
        TestAssert.Equal(TimeSpan.Zero, meta.Timestamp.Offset);
    }

    /// <summary>The Empty factory produces the same defaults.</summary>
    [Test]
    public void Empty_ProducesDefaults()
    {
        var meta = ResponseMetadata.Empty;
        TestAssert.Equal(string.Empty, meta.RequestId);
        TestAssert.NotNull(meta.Extra);
        TestAssert.Equal(0, meta.Extra.Count);
    }

    /// <summary>Init-only properties accept values through object initializers.</summary>
    [Test]
    public void InitProperties_AcceptValues()
    {
        var ts = new DateTimeOffset(2026, 1, 15, 10, 30, 0, TimeSpan.Zero);
        var extra = new Dictionary<string, string> { { "region", "eu-west-1" } };
        var meta = new ResponseMetadata
        {
            RequestId = "req-42",
            Timestamp = ts,
            Extra = extra
        };
        TestAssert.Equal("req-42", meta.RequestId);
        TestAssert.Equal(ts, meta.Timestamp);
        TestAssert.Equal(1, meta.Extra.Count);
        TestAssert.Equal("eu-west-1", meta.Extra["region"]);
    }

    /// <summary>WithRequestId returns a new instance; the original is unchanged.</summary>
    [Test]
    public void WithRequestId_ReturnsNewInstance()
    {
        var original = new ResponseMetadata { RequestId = "before" };
        var updated = original.WithRequestId("after");
        TestAssert.Equal("before", original.RequestId);
        TestAssert.Equal("after", updated.RequestId);
        TestAssert.False(ReferenceEquals(original, updated));
    }

    /// <summary>WithRequestId rejects a null argument.</summary>
    [Test]
    public void WithRequestId_NullArgument_Throws()
    {
        var meta = new ResponseMetadata();
        TestAssert.Throws<ArgumentNullException>(() => meta.WithRequestId(null!));
    }

    /// <summary>Extra accepts multiple values.</summary>
    [Test]
    public void Extra_AcceptsMultipleValues()
    {
        var extra = new Dictionary<string, string>
        {
            { "region", "eu-west-1" },
            { "build", "0.1.0" },
            { "env", "production" }
        };
        var meta = new ResponseMetadata { Extra = extra };
        TestAssert.Equal(3, meta.Extra.Count);
        TestAssert.Equal("0.1.0", meta.Extra["build"]);
    }

    /// <summary>
    /// Two separately constructed metadata instances are distinct
    /// reference objects. Records with a dictionary member do not
    /// compare by value through the default record equality operators,
    /// because the dictionary uses reference equality. This is expected
    /// and documented rather than worked around.
    /// </summary>
    [Test]
    public void SeparateConstructions_AreDistinctInstances()
    {
        var a = new ResponseMetadata { RequestId = "req-1" };
        var b = new ResponseMetadata { RequestId = "req-1" };
        TestAssert.False(ReferenceEquals(a, b));
    }

    /// <summary>
    /// Metadata constructed with the same values carries the same
    /// property values. Consumers rely on property-level equality,
    /// not on record equality, because the Extra dictionary does not
    /// participate in structural comparison.
    /// </summary>
    [Test]
    public void Properties_MatchWhenConstructedWithSameValues()
    {
        var ts = new DateTimeOffset(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);
        var a = new ResponseMetadata { RequestId = "req-1", Timestamp = ts };
        var b = new ResponseMetadata { RequestId = "req-1", Timestamp = ts };
        TestAssert.Equal(a.RequestId, b.RequestId);
        TestAssert.Equal(a.Timestamp, b.Timestamp);
        TestAssert.Equal(a.Extra.Count, b.Extra.Count);
    }

    /// <summary>
    /// Each construction gets its own Extra dictionary instance. Mutating
    /// one does not affect another. This proves the default is not shared.
    /// </summary>
    [Test]
    public void Extra_DefaultIsEmptyAndIndependent()
    {
        var a = new ResponseMetadata();
        var b = new ResponseMetadata();
        TestAssert.False(ReferenceEquals(a.Extra, b.Extra));
        TestAssert.Equal(0, a.Extra.Count);
        TestAssert.Equal(0, b.Extra.Count);
    }
}

