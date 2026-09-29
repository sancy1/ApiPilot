// filepath: dotnet/src/ApiPilot.Core/Metadata/ICorrelationIdGenerator.cs
// layer: Metadata | package: ApiPilot.Core | since: v0.1.0-alpha.0
// purpose: Contract for generating a new correlation identifier per request
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (interface)
//   Depends on : n/a
//   Used by    : ASP.NET Core correlation middleware (Phase 1.6)
//   See also   : DefaultCorrelationIdGenerator.cs, CorrelationOptions.cs, SPEC.md
// -----------------------------------------------------------------------------

namespace ApiPilot.Core.Metadata;

/// <summary>
/// Produces new correlation identifiers. Implementations must be
/// thread-safe because the generator is invoked once per request and
/// requests may arrive concurrently.
/// </summary>
public interface ICorrelationIdGenerator
{
    /// <summary>
    /// Returns a new correlation identifier. The value must be non-null
    /// and non-empty, and must satisfy the default validation pattern
    /// defined in <see cref="CorrelationOptions"/> unless the application
    /// widens that pattern.
    /// </summary>
    string NewId();
}

