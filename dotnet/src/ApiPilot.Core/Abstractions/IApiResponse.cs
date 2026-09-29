// filepath: dotnet/src/ApiPilot.Core/Abstractions/IApiResponse.cs
// layer: Abstractions | package: ApiPilot.Core | since: v0.1.0-alpha.0
// purpose: Non-generic marker interface shared by every ApiPilot response
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (interface)
//   Depends on : ResponseMetadata (in ApiPilot.Core.Metadata)
//   Used by    : ApiResponse, ApiResponse<T>, ErrorResponse
//   See also   : SPEC.md, ApiResponse.cs, ApiResponseOfT.cs
// -----------------------------------------------------------------------------

using ApiPilot.Core.Metadata;

namespace ApiPilot.Core.Abstractions;

/// <summary>
/// Marker interface implemented by every ApiPilot response envelope,
/// successful or not. Exposes only the two members common to every
/// response so that middleware and handlers can inspect or forward
/// responses generically.
/// </summary>
public interface IApiResponse
{
    /// <summary>
    /// True for a successful response; false for an error response.
    /// </summary>
    bool Success { get; }

    /// <summary>
    /// Non-business metadata attached to every response: the correlation
    /// ID, the server timestamp, and an optional dictionary of extra values
    /// the application has chosen to expose.
    /// </summary>
    ResponseMetadata Meta { get; }
}

