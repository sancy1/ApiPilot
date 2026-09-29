// filepath: dotnet/src/ApiPilot.Core/Pagination/SortRequest.cs
// layer: Pagination | package: ApiPilot.Core | since: v0.1.0-alpha.0
// purpose: Validated value object describing a sort field and direction
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (sealed class, enum, static extensions)
//   Depends on : n/a
//   Used by    : ASP.NET Core pagination endpoint filter (Phase 1.5)
//   See also   : PageRequest.cs, FilterRequest.cs, SPEC.md
// -----------------------------------------------------------------------------

namespace ApiPilot.Core.Pagination;

/// <summary>
/// A validated sort request describing the field to order by and the
/// direction. The field name is not validated against an application
/// allow-list; the application decides which sort fields are acceptable.
/// </summary>
public sealed class SortRequest
{
    /// <summary>The field to sort by. Non-null and non-empty.</summary>
    public string Field { get; }

    /// <summary>The sort direction.</summary>
    public SortDirection Direction { get; }

    private SortRequest(string field, SortDirection direction)
    {
        Field = field;
        Direction = direction;
    }

    /// <summary>
    /// Creates a sort request from a field name and direction.
    /// </summary>
    /// <param name="field">The field name. Must be non-null and non-empty.</param>
    /// <param name="direction">The sort direction.</param>
    /// <returns>A validated sort request.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="field"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the field name is empty or whitespace.
    /// </exception>
    public static SortRequest Create(string field, SortDirection direction)
    {
        ArgumentNullException.ThrowIfNull(field);
        var trimmed = field.Trim();
        if (trimmed.Length == 0)
        {
            throw new ArgumentException("Sort field must not be empty or whitespace.", nameof(field));
        }
        return new SortRequest(trimmed, direction);
    }
}

/// <summary>The direction of a sort. Transport-neutral.</summary>
public enum SortDirection
{
    /// <summary>Ascending order.</summary>
    Ascending,

    /// <summary>Descending order.</summary>
    Descending,
}

/// <summary>
/// Helpers for converting <see cref="SortDirection"/> to and from the
/// wire values used by query parameters. The wire uses "asc" and "desc".
/// </summary>
public static class SortDirectionExtensions
{
    /// <summary>
    /// Returns the wire value for the direction: "asc" or "desc".
    /// </summary>
    public static string ToWireValue(this SortDirection direction)
    {
        return direction == SortDirection.Ascending ? "asc" : "desc";
    }

    /// <summary>
    /// Attempts to parse a wire value into a sort direction. Accepts
    /// "asc" and "desc" case-insensitively. Returns false for any other
    /// input.
    /// </summary>
    public static bool TryParseWireValue(string? value, out SortDirection direction)
    {
        direction = SortDirection.Ascending;
        if (value is null)
        {
            return false;
        }
        if (string.Equals(value, "asc", StringComparison.OrdinalIgnoreCase))
        {
            direction = SortDirection.Ascending;
            return true;
        }
        if (string.Equals(value, "desc", StringComparison.OrdinalIgnoreCase))
        {
            direction = SortDirection.Descending;
            return true;
        }
        return false;
    }
}

