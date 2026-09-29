// filepath: dotnet/src/ApiPilot.Core/Metadata/DefaultCorrelationIdGenerator.cs
// layer: Metadata | package: ApiPilot.Core | since: v0.1.0-alpha.0
// purpose: Default generator producing time-ordered GUIDv7 correlation identifiers
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : ICorrelationIdGenerator
//   Depends on : System.Guid (BCL)
//   Used by    : ASP.NET Core correlation middleware (Phase 1.6)
//   See also   : ICorrelationIdGenerator.cs, SPEC.md (correlation ID rules)
// -----------------------------------------------------------------------------

namespace ApiPilot.Core.Metadata;

/// <summary>
/// Default correlation ID generator. Produces a version 7 GUID in the
/// standard hyphenated form. Version 7 GUIDs are time-ordered and
/// unpredictable, which makes them well suited to log correlation and
/// safe to expose in response metadata.
/// </summary>
public sealed class DefaultCorrelationIdGenerator : ICorrelationIdGenerator
{
    /// <inheritdoc />
    public string NewId()
    {
        return Guid.CreateVersion7().ToString("D");
    }
}

