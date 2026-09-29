// filepath: dotnet/src/ApiPilot.Security/Csrf/ICsrfRotationStore.cs
// layer: Csrf | package: ApiPilot.Security | since: v0.3.0
// purpose: Server-side store for CSRF rotation markers
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (interface)
//   Depends on : n/a
//   Used by    : CsrfService
//   See also   : CsrfService.cs, CsrfOptions.cs (RotationRequirement)
// -----------------------------------------------------------------------------

namespace ApiPilot.Security.Csrf;

/// <summary>
/// A server-side store for CSRF rotation markers. A rotation marker is
/// the UTC timestamp of the last rotation for a binding. A token whose
/// issue time is before the marker is rejected with
/// <see cref="CsrfTokenParseResult.Rotated"/>.
/// </summary>
/// <remarks>
/// The interface is async because a real implementation is likely to be
/// backed by Redis, a database, or another networked store. A
/// synchronous interface would force such implementations into
/// blocking waits.
///
/// The store is optional. The default registration does not provide a
/// store. When <see cref="CsrfOptions.RotationRequirement"/> is
/// Optional, the service operates in expiry-only rotation mode and
/// reports that old tokens remain valid until natural expiry. When
/// RotationRequirement is Required, the host fails at startup unless
/// an implementation of this interface is registered.
///
/// Implementations must be safe for concurrent use across requests.
/// The service does not serialize access.
/// </remarks>
public interface ICsrfRotationStore
{
    /// <summary>
    /// Reads the rotation marker for the given binding.
    /// </summary>
    /// <param name="binding">
    /// The binding to read. Must not be null or empty.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// The UTC timestamp of the last rotation for the binding, or null
    /// when no rotation has been recorded.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="binding"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="binding"/> is empty or whitespace.
    /// </exception>
    Task<DateTimeOffset?> GetRotationMarkerAsync(
        string binding,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a rotation marker for the given binding. Any previously
    /// recorded marker for the binding is replaced.
    /// </summary>
    /// <param name="binding">
    /// The binding to record. Must not be null or empty.
    /// </param>
    /// <param name="rotatedAtUtc">
    /// The UTC moment of the rotation. Must be a UTC value.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when the marker has been stored.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="binding"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="binding"/> is empty or whitespace, or
    /// when <paramref name="rotatedAtUtc"/> is not a UTC value.
    /// </exception>
    Task SetRotationMarkerAsync(
        string binding,
        DateTimeOffset rotatedAtUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the rotation marker for the given binding. Called on logout
    /// or session teardown. A no-op when no marker is present.
    /// </summary>
    /// <param name="binding">
    /// The binding to clear. Must not be null or empty.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when the marker has been cleared.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="binding"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="binding"/> is empty or whitespace.
    /// </exception>
    Task ClearRotationMarkerAsync(
        string binding,
        CancellationToken cancellationToken = default);
}

