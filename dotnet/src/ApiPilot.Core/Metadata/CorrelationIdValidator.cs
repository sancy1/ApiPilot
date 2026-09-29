// filepath: dotnet/src/ApiPilot.Core/Metadata/CorrelationIdValidator.cs
// layer: Metadata | package: ApiPilot.Core | since: v0.1.0-alpha.0
// purpose: Validates incoming correlation identifiers against a configured pattern
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (sealed class)
//   Depends on : System.Text.RegularExpressions
//   Used by    : ASP.NET Core correlation middleware (Phase 1.6)
//   See also   : CorrelationOptions.cs, ICorrelationIdGenerator.cs, SPEC.md
// -----------------------------------------------------------------------------

using System.Text.RegularExpressions;

namespace ApiPilot.Core.Metadata;

/// <summary>
/// Validates incoming correlation identifiers against a configured regular
/// expression pattern. The regex is compiled once at construction so that
/// validation is cheap per call. A null or non-matching value is rejected.
/// </summary>
public sealed class CorrelationIdValidator
{
    private readonly Regex _pattern;

    /// <summary>
    /// Creates a validator from a regular expression pattern. The pattern
    /// is compiled once. Invalid patterns throw <see cref="ArgumentException"/>
    /// from the regex compiler.
    /// </summary>
    /// <param name="pattern">The validation pattern. Must not be null or empty.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="pattern"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the pattern is empty or cannot be compiled.
    /// </exception>
    public CorrelationIdValidator(string pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        if (pattern.Length == 0)
        {
            throw new ArgumentException("Validation pattern must not be empty.", nameof(pattern));
        }
        _pattern = new Regex(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant);
    }

    /// <summary>
    /// Returns true when the value matches the validation pattern. A null
    /// value is always invalid.
    /// </summary>
    /// <param name="value">The value to validate. May be null.</param>
    /// <returns>True when the value is valid; false otherwise.</returns>
    public bool IsValid(string? value)
    {
        if (value is null)
        {
            return false;
        }
        return _pattern.IsMatch(value);
    }
}

