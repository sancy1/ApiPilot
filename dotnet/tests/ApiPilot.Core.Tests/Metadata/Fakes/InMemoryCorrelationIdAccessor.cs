// filepath: dotnet/tests/ApiPilot.Core.Tests/Metadata/Fakes/InMemoryCorrelationIdAccessor.cs
// layer: Tests | package: ApiPilot.Core.Tests | since: v0.1.0-alpha.0
// purpose: In-memory test double for the ICorrelationIdAccessor contract
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : ICorrelationIdAccessor
//   Depends on : ApiPilot.Core.Metadata
//   Used by    : DefaultCorrelationIdGeneratorTests, future integration tests
//   See also   : ICorrelationIdAccessor.cs
// -----------------------------------------------------------------------------

using ApiPilot.Core.Metadata;

namespace ApiPilot.Core.Tests.Metadata.Fakes;

/// <summary>
/// A simple in-memory implementation of ICorrelationIdAccessor. Used as a
/// test double until the ASP.NET Core implementation is available in
/// Phase 1.6. The request ID is stored in a mutable property.
/// </summary>
internal sealed class InMemoryCorrelationIdAccessor : ICorrelationIdAccessor
{
    /// <summary>
    /// The current request ID. May be null when no correlation ID has been
    /// set. The setter is provided for test scenarios only.
    /// </summary>
    public string? RequestId { get; set; }
}

