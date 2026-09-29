// filepath: dotnet/src/ApiPilot.Core/Errors/ApiErrorField.cs
// layer: Errors | package: ApiPilot.Core | since: v0.1.0-alpha.0
// purpose: Immutable value object pairing a field name with its validation messages
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (sealed record)
//   Depends on : n/a
//   Used by    : ApiError, ErrorResponse, validation response factory
//   See also   : SPEC.md (error envelope fields), ApiError.cs
// -----------------------------------------------------------------------------

namespace ApiPilot.Core.Errors;

/// <summary>
/// An immutable pairing of a field name and the array of validation
/// messages associated with that field. Used inside the fields dictionary
/// of a validation error envelope.
/// </summary>
/// <remarks>
/// Messages are stored as an immutable snapshot. The record does not
/// provide structural value equality over the messages collection; two
/// instances compare by value only when their message collections are
/// reference-identical. Consumers rely on property-level equality, not
/// on record equality, for this type.
/// </remarks>
public sealed record ApiErrorField
{
    /// <summary>
    /// The field name as it appears on the wire, for example "email" or
    /// "orders[0].email". Always non-null and non-empty.
    /// </summary>
    public string Field { get; }

    /// <summary>
    /// The validation messages for this field. Always non-null and
    /// contains at least one message. The collection is an immutable
    /// snapshot taken at construction.
    /// </summary>
    public IReadOnlyList<string> Messages { get; }

    private ApiErrorField(string field, IReadOnlyList<string> messages)
    {
        Field = field;
        Messages = messages;
    }

    /// <summary>
    /// Creates a field error from a field name and a collection of messages.
    /// The messages are copied into an immutable array. Null or empty
    /// arguments are rejected.
    /// </summary>
    /// <param name="field">The field name. Must be non-null and non-empty.</param>
    /// <param name="messages">The messages. Must contain at least one non-null entry.</param>
    /// <returns>A new field error.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="field"/> or <paramref name="messages"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the field is empty or whitespace, or when the messages
    /// collection is empty or contains a null entry.
    /// </exception>
    public static ApiErrorField Create(string field, IEnumerable<string> messages)
    {
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(messages);

        var trimmed = field.Trim();
        if (trimmed.Length == 0)
        {
            throw new ArgumentException("Field name must not be empty or whitespace.", nameof(field));
        }

        var snapshot = new List<string>();
        foreach (var m in messages)
        {
            if (m is null)
            {
                throw new ArgumentException("Message entries must not be null.", nameof(messages));
            }
            snapshot.Add(m);
        }

        if (snapshot.Count == 0)
        {
            throw new ArgumentException("At least one message is required.", nameof(messages));
        }

        return new ApiErrorField(trimmed, snapshot.ToArray());
    }

    /// <summary>
    /// Creates a field error carrying a single message.
    /// </summary>
    /// <param name="field">The field name. Must be non-null and non-empty.</param>
    /// <param name="message">The message. Must be non-null.</param>
    /// <returns>A new field error.</returns>
    public static ApiErrorField WithMessage(string field, string message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return Create(field, new[] { message });
    }
}

