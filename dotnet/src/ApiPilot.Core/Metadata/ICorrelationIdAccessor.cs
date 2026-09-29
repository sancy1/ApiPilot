// filepath: dotnet/src/ApiPilot.Core/Metadata/ICorrelationIdAccessor.cs
// layer: Metadata | package: ApiPilot.Core | since: v0.1.0-alpha.0
// purpose: Read-only ambient accessor for the current correlation identifier
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (interface)
//   Depends on : n/a
//   Used by    : application code, logging, downstream HTTP propagation
//   See also   : ICorrelationIdGenerator.cs, SPEC.md (correlation ID rules)
// -----------------------------------------------------------------------------

namespace ApiPilot.Core.Metadata;

/// <summary>
/// Read-only access to the correlation identifier for the current
/// request. Implementations read from an ambient context such as
/// HttpContext.Items or AsyncLocal. The concrete implementation lives
/// in the ASP.NET Core adapter (Phase 1.6).
/// </summary>
public interface ICorrelationIdAccessor
{
    /// <summary>
    /// The correlation identifier for the current context, or null when
    /// no correlation ID has been established (for example, in code that
    /// runs outside an HTTP request).
    /// </summary>
    string? RequestId { get; }
}

