// filepath: dotnet/src/ApiPilot.AspNetCore/Validation/FieldKeyNormalizer.cs
// layer: Validation | package: ApiPilot.AspNetCore | since: v0.2.0-alpha.0
// purpose: Default camelCase normalizer for model state field keys
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (static class)
//   Depends on : System.Text.Json.JsonNamingPolicy.CamelCase
//   Used by    : ModelStateAdapter
//   See also   : ModelStateAdapter.cs, SPEC.md (validation fields)
// -----------------------------------------------------------------------------

using System.Text;
using System.Text.Json;

namespace ApiPilot.AspNetCore.Validation;

/// <summary>
/// Default key normalizer for validation field names. Converts the various
/// key formats produced by ASP.NET Core model binding into a single canonical
/// form: lowercase camelCase segments, index notation preserved in brackets.
/// </summary>
/// <remarks>
/// ASP.NET Core produces different ModelState key formats depending on how
/// the model was bound: JSONPath with a leading dollar sign for FromBody,
/// bracket notation for form-urlencoded input, plain names for query strings.
/// This normalizer converges all common forms to one canonical representation.
/// Applications that need a different canonical form can supply their own
/// transform to the ModelStateAdapter.
/// </remarks>
public static class FieldKeyNormalizer
{
    /// <summary>
    /// Normalizes a field key into camelCase with bracket-preserved indices.
    /// </summary>
    /// <param name="key">The source key. May be empty.</param>
    /// <returns>The normalized key.</returns>
    public static string Normalize(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return key;
        }

        // Strip a leading dollar sign and any leading dots (JSONPath prefix).
        var start = 0;
        while (start < key.Length && (key[start] == '$' || key[start] == '.'))
        {
            start++;
        }
        if (start >= key.Length)
        {
            return key;
        }

        var builder = new StringBuilder(key.Length);
        var firstSegmentEmitted = false;

        var i = start;
        while (i < key.Length)
        {
            var c = key[i];

            if (c == '.')
            {
                i++;
                continue;
            }

            if (c == '[')
            {
                // Copy the bracket expression verbatim.
                var close = key.IndexOf(']', i);
                if (close < 0)
                {
                    builder.Append(key.AsSpan(i));
                    break;
                }
                builder.Append(key.AsSpan(i, close - i + 1));
                i = close + 1;
                continue;
            }

            // Read a segment up to the next dot or open bracket.
            var segStart = i;
            while (i < key.Length && key[i] != '.' && key[i] != '[')
            {
                i++;
            }
            var segment = key.Substring(segStart, i - segStart);

            var camel = JsonNamingPolicy.CamelCase.ConvertName(segment);

            if (firstSegmentEmitted)
            {
                builder.Append('.');
            }
            builder.Append(camel);
            firstSegmentEmitted = true;
        }

        return builder.ToString();
    }
}

