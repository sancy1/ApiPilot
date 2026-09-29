// filepath: dotnet/src/ApiPilot.Core/Pagination/FilterRequest.cs
// layer: Pagination | package: ApiPilot.Core | since: v0.1.0-alpha.0
// purpose: Explicit key/value filter pairs for collection queries
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (sealed class)
//   Depends on : n/a
//   Used by    : ASP.NET Core pagination endpoint filter (Phase 1.5)
//   See also   : PageRequest.cs, SortRequest.cs, SPEC.md (query validation)
// -----------------------------------------------------------------------------

namespace ApiPilot.Core.Pagination;

/// <summary>
/// An immutable set of filter criteria expressed as explicit key/value
/// pairs. The library does not parse arbitrary filter expressions and
/// does not translate filter strings into SQL or LINQ. The application
/// interprets the pairs.
/// </summary>
public sealed class FilterRequest
{
    /// <summary>The canonical empty filter request.</summary>
    public static FilterRequest Empty { get; } =
        new FilterRequest(new Dictionary<string, string>());

    /// <summary>
    /// The filter pairs, keyed by filter name. Never null; may be empty.
    /// </summary>
    public IReadOnlyDictionary<string, string> Pairs { get; }

    private FilterRequest(IReadOnlyDictionary<string, string> pairs)
    {
        Pairs = pairs;
    }

    /// <summary>
    /// Creates a filter request from a sequence of key/value pairs. Keys
    /// must be non-null and non-empty. Values may be empty strings.
    /// Duplicate keys are rejected.
    /// </summary>
    /// <param name="pairs">The filter pairs. Must not be null.</param>
    /// <returns>A validated filter request.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="pairs"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when a key is empty or whitespace, or when two keys are the same.
    /// </exception>
    public static FilterRequest Create(IEnumerable<KeyValuePair<string, string>> pairs)
    {
        ArgumentNullException.ThrowIfNull(pairs);

        var dict = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in pairs)
        {
            var key = pair.Key;
            if (key is null)
            {
                throw new ArgumentException("Filter key must not be null.", nameof(pairs));
            }
            var trimmedKey = key.Trim();
            if (trimmedKey.Length == 0)
            {
                throw new ArgumentException("Filter key must not be empty or whitespace.", nameof(pairs));
            }
            if (!dict.TryAdd(trimmedKey, pair.Value ?? string.Empty))
            {
                throw new ArgumentException($"Duplicate filter key: {trimmedKey}.", nameof(pairs));
            }
        }

        if (dict.Count == 0)
        {
            return Empty;
        }

        return new FilterRequest(dict);
    }
}

