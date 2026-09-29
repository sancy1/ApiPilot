// filepath: dotnet/src/ApiPilot.Core/Validation/ValidationErrors.cs
// layer: Validation | package: ApiPilot.Core | since: v0.1.0-alpha.0
// purpose: Immutable, deterministic, deduplicated collection of validation field errors
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (sealed class)
//   Depends on : ApiErrorField
//   Used by    : ValidationResponseFactory, application validators
//   See also   : ApiErrorField.cs, ValidationResponseFactory.cs
// -----------------------------------------------------------------------------

using ApiPilot.Core.Errors;

namespace ApiPilot.Core.Validation;

/// <summary>
/// An immutable, deterministic collection of validation field errors.
/// Fields are sorted by name (ordinal, case-sensitive) and merged by field
/// name. Messages within a merged field preserve first-occurrence order and
/// are deduplicated. The collection is intended as the single source of
/// truth for validation failures before they are wrapped into an
/// ErrorResponse by ValidationResponseFactory.
/// </summary>
/// <remarks>
/// Instances are immutable. Modifications produce new instances. The type
/// does not provide structural value equality because its internal list
/// uses reference equality; consumers compare individual Fields instead.
/// </remarks>
public sealed class ValidationErrors
{
    private readonly IReadOnlyList<ApiErrorField> _fields;

    /// <summary>The canonical empty collection.</summary>
    public static ValidationErrors Empty { get; } =
        new ValidationErrors(Array.Empty<ApiErrorField>());

    private ValidationErrors(IReadOnlyList<ApiErrorField> fields)
    {
        _fields = fields;
    }

    /// <summary>The sorted, deduplicated list of field errors.</summary>
    public IReadOnlyList<ApiErrorField> Fields => _fields;

    /// <summary>The number of unique field names in the collection.</summary>
    public int Count => _fields.Count;

    /// <summary>True when the collection contains no field errors.</summary>
    public bool IsEmpty => _fields.Count == 0;

    /// <summary>
    /// Creates a collection from a sequence of field errors. Nulls and
    /// null entries are rejected. Fields are merged by name and sorted.
    /// </summary>
    /// <param name="errors">The source field errors. Must not be null.</param>
    /// <returns>A new, deduplicated, sorted collection.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="errors"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the sequence contains a null entry.
    /// </exception>
    public static ValidationErrors From(IEnumerable<ApiErrorField> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        var snapshot = new List<ApiErrorField>();
        foreach (var e in errors)
        {
            if (e is null)
            {
                throw new ArgumentException("Validation error entries must not be null.", nameof(errors));
            }
            snapshot.Add(e);
        }

        return Build(snapshot);
    }

    /// <summary>
    /// Returns a new collection with the additional field error merged in.
    /// </summary>
    /// <param name="error">The field error to merge. Must not be null.</param>
    /// <returns>A new collection.</returns>
    public ValidationErrors With(ApiErrorField error)
    {
        ArgumentNullException.ThrowIfNull(error);
        var combined = new List<ApiErrorField>(_fields);
        combined.Add(error);
        return Build(combined);
    }

    /// <summary>
    /// Returns a new collection with the additional field errors merged in.
    /// </summary>
    /// <param name="errors">The field errors to merge. Must not be null.</param>
    /// <returns>A new collection.</returns>
    public ValidationErrors WithRange(IEnumerable<ApiErrorField> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        var combined = new List<ApiErrorField>(_fields);
        foreach (var e in errors)
        {
            if (e is null)
            {
                throw new ArgumentException("Validation error entries must not be null.", nameof(errors));
            }
            combined.Add(e);
        }
        return Build(combined);
    }

    private static ValidationErrors Build(List<ApiErrorField> source)
    {
        if (source.Count == 0)
        {
            return Empty;
        }

        // Merge by field name, deduplicate messages, preserve first-occurrence order.
        var merged = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var error in source)
        {
            if (!merged.TryGetValue(error.Field, out var messages))
            {
                messages = new List<string>();
                merged[error.Field] = messages;
            }
            foreach (var message in error.Messages)
            {
                if (!messages.Contains(message))
                {
                    messages.Add(message);
                }
            }
        }

        // Sort field names ordinally, then build ApiErrorField instances.
        var sortedFields = new List<ApiErrorField>(merged.Count);
        foreach (var key in merged.Keys.OrderBy(k => k, StringComparer.Ordinal))
        {
            sortedFields.Add(ApiErrorField.Create(key, merged[key]));
        }

        return new ValidationErrors(sortedFields);
    }
}

