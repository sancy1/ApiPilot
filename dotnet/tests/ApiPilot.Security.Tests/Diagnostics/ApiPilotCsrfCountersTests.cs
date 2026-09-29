// filepath: dotnet/tests/ApiPilot.Security.Tests/Diagnostics/ApiPilotCsrfCountersTests.cs
// layer: Diagnostics.Tests | package: ApiPilot.Security.Tests | since: v0.5.0
// purpose: Tests for the ApiPilotCsrfCounters metrics
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : ApiPilotCsrfCounters, System.Diagnostics.Metrics
//   Used by    : the test harness
//   See also   : ApiPilotCsrfCounters.cs
// -----------------------------------------------------------------------------
//
// NOTE ON METERLISTENER SEMANTICS (A-219)
//   MeterListener receives one measurement per instrument call, not one
//   aggregated measurement. A counter incremented three times produces
//   three listener callbacks. A test that asserts "the total is N" must
//   sum the measurements, not look at a single one.

using System.Diagnostics.Metrics;
using ApiPilot.Security.Diagnostics;

namespace ApiPilot.Security.Tests.Diagnostics;

/// <summary>Contract tests for ApiPilotCsrfCounters. Verifies the meter
/// name and version, the counter and histogram increments, the reason
/// tag, and the rejection of null/empty/whitespace reasons.</summary>
[TestClass]
public sealed class ApiPilotCsrfCountersTests
{
    /// <summary>MeterName is exactly "ApiPilot".</summary>
    [Test]
    public void MeterName_IsApiPilot()
    {
        TestAssert.Equal("ApiPilot", ApiPilotCsrfCounters.MeterName);
    }

    /// <summary>MeterVersion is the expected version.</summary>
    [Test]
    public void MeterVersion_IsTheExpectedVersion()
    {
        TestAssert.Equal("1.0.0", ApiPilotCsrfCounters.MeterVersion);
    }

    /// <summary>TokenIssued increments the issued counter.</summary>
    [Test]
    public void TokenIssued_IncrementsIssuedCounter()
    {
        using var counters = new ApiPilotCsrfCounters();
        var measurements = new List<(string Name, long Value, string? Reason)>();
        using var listener = BuildListener(measurements);
        counters.TokenIssued();
        counters.TokenIssued();
        counters.TokenIssued();
        TestAssert.Equal(3, measurements.Count);
        var totalIssued = 0L;
        foreach (var m in measurements) { totalIssued += m.Value; }
        TestAssert.Equal(3L, totalIssued);
        foreach (var m in measurements) { TestAssert.Equal("apipilot.csrf.issued", m.Name); }
    }

    /// <summary>ValidationSucceeded increments the ok counter.</summary>
    [Test]
    public void ValidationSucceeded_IncrementsOkCounter()
    {
        using var counters = new ApiPilotCsrfCounters();
        var measurements = new List<(string Name, long Value, string? Reason)>();
        using var listener = BuildListener(measurements);
        counters.ValidationSucceeded();
        counters.ValidationSucceeded();
        TestAssert.Equal(2, measurements.Count);
        var totalOk = 0L;
        foreach (var m in measurements) { totalOk += m.Value; }
        TestAssert.Equal(2L, totalOk);
        foreach (var m in measurements) { TestAssert.Equal("apipilot.csrf.validated.ok", m.Name); }
    }

    /// <summary>ValidationFailed records the reason tag.</summary>
    [Test]
    public void ValidationFailed_RecordsReasonTag()
    {
        using var counters = new ApiPilotCsrfCounters();
        var measurements = new List<(string Name, long Value, string? Reason)>();
        using var listener = BuildListener(measurements);
        counters.ValidationFailed("expired");
        TestAssert.Equal(1, measurements.Count);
        TestAssert.Equal("apipilot.csrf.validated.failed", measurements[0].Name);
        TestAssert.Equal("expired", measurements[0].Reason);
    }

    /// <summary>ValidationFailed rejects a null reason.</summary>
    [Test]
    public void ValidationFailed_NullReasonThrows()
    {
        using var counters = new ApiPilotCsrfCounters();
        TestAssert.Throws<ArgumentException>(() => counters.ValidationFailed(null!));
    }

    /// <summary>ValidationFailed rejects an empty reason.</summary>
    [Test]
    public void ValidationFailed_EmptyReasonThrows()
    {
        using var counters = new ApiPilotCsrfCounters();
        TestAssert.Throws<ArgumentException>(() => counters.ValidationFailed(string.Empty));
    }

    /// <summary>ValidationFailed rejects a whitespace reason.</summary>
    [Test]
    public void ValidationFailed_WhitespaceReasonThrows()
    {
        using var counters = new ApiPilotCsrfCounters();
        TestAssert.Throws<ArgumentException>(() => counters.ValidationFailed("   "));
    }

    /// <summary>TokenRotated increments the rotated counter.</summary>
    [Test]
    public void TokenRotated_IncrementsRotatedCounter()
    {
        using var counters = new ApiPilotCsrfCounters();
        var measurements = new List<(string Name, long Value, string? Reason)>();
        using var listener = BuildListener(measurements);
        counters.TokenRotated();
        TestAssert.Equal(1, measurements.Count);
        TestAssert.Equal("apipilot.csrf.rotated", measurements[0].Name);
        TestAssert.Equal(1L, measurements[0].Value);
    }

    /// <summary>RecordValidationDuration records a histogram observation.</summary>
    [Test]
    public void RecordValidationDuration_RecordsHistogram()
    {
        using var counters = new ApiPilotCsrfCounters();
        var histogram = new List<(string Name, double Value)>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name == "ApiPilot")
            {
                l.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<double>((instrument, value, tags, state) =>
        {
            histogram.Add((instrument.Name, value));
        });
        listener.Start();
        counters.RecordValidationDuration(12.5);
        TestAssert.Equal(1, histogram.Count);
        TestAssert.Equal("apipilot.csrf.validation.duration", histogram[0].Name);
        TestAssert.Equal(12.5, histogram[0].Value);
    }

    /// <summary>Dispose can be called twice without throwing.</summary>
    [Test]
    public void Dispose_CanBeCalledTwice()
    {
        var counters = new ApiPilotCsrfCounters();
        counters.Dispose();
        counters.Dispose();
    }

    private static MeterListener BuildListener(List<(string Name, long Value, string? Reason)> measurements)
    {
        var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name == "ApiPilot")
            {
                l.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, value, tags, state) =>
        {
            string? reason = null;
            foreach (var tag in tags)
            {
                if (tag.Key == "reason" && tag.Value is string s)
                {
                    reason = s;
                }
            }
            measurements.Add((instrument.Name, value, reason));
        });
        listener.Start();
        return listener;
    }
}

