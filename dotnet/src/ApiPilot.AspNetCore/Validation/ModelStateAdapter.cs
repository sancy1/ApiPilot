// filepath: dotnet/src/ApiPilot.AspNetCore/Validation/ModelStateAdapter.cs
// layer: Validation | package: ApiPilot.AspNetCore | since: v0.2.0-alpha.0
// purpose: Converts ModelStateDictionary entries into ApiErrorField values
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (static class)
//   Depends on : ApiPilot.Core.Errors.ApiErrorField, FieldKeyNormalizer,
//                Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary
//   Used by    : ApiPilotValidateFilter, ApiPilotValidationEndpointFilter
//   See also   : FieldKeyNormalizer.cs, SPEC.md (validation fields)
// -----------------------------------------------------------------------------

using ApiPilot.Core.Errors;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ApiPilot.AspNetCore.Validation;

/// <summary>
/// Converts <see cref="ModelStateDictionary"/> entries into a list of
/// <see cref="ApiErrorField"/> values suitable for the ApiPilot validation
/// response. Fields whose transformed keys collide are merged, and the
/// result is sorted ordinally by key.
/// </summary>
public static class ModelStateAdapter
{
    /// <summary>
    /// Converts the model state to a list of field errors using the default
    /// key normalizer. This is equivalent to calling the two-argument
    /// overload with a null key transform.
    /// </summary>
    /// <param name="modelState">The model state. Must not be null.</param>
    /// <returns>The field errors, sorted ordinally by key.</returns>
    public static IReadOnlyList<ApiErrorField> ToErrors(ModelStateDictionary modelState)
    {
        return ToErrors(modelState, keyTransform: null);
    }

    /// <summary>
    /// Converts the model state to a list of field errors. When
    /// <paramref name="keyTransform"/> is null, the default camelCase
    /// normalizer is used. The transform receives the raw ModelState key
    /// and returns the key that appears in the response.
    /// </summary>
    /// <param name="modelState">The model state. Must not be null.</param>
    /// <param name="keyTransform">
    /// Optional key transform. When null, FieldKeyNormalizer.Normalize is used.
    /// </param>
    /// <returns>The field errors, sorted ordinally by key.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="modelState"/> is null.
    /// </exception>
    public static IReadOnlyList<ApiErrorField> ToErrors(
        ModelStateDictionary modelState,
        Func<string, string>? keyTransform)
    {
        ArgumentNullException.ThrowIfNull(modelState);

        var transform = keyTransform ?? FieldKeyNormalizer.Normalize;
        var merged = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (var entry in modelState)
        {
            if (entry.Value.Errors.Count == 0)
            {
                continue;
            }

            var rawKey = entry.Key ?? string.Empty;
            var key = transform(rawKey);

            if (!merged.TryGetValue(key, out var messages))
            {
                messages = new List<string>();
                merged[key] = messages;
            }

            foreach (var error in entry.Value.Errors)
            {
                var message = string.IsNullOrEmpty(error.ErrorMessage)
                    ? error.Exception?.Message ?? "Invalid value."
                    : error.ErrorMessage;

                if (!messages.Contains(message))
                {
                    messages.Add(message);
                }
            }
        }

        if (merged.Count == 0)
        {
            return Array.Empty<ApiErrorField>();
        }

        var sorted = new List<ApiErrorField>(merged.Count);
        foreach (var key in merged.Keys.OrderBy(k => k, StringComparer.Ordinal))
        {
            sorted.Add(ApiErrorField.Create(key, merged[key]));
        }

        return sorted;
    }
}

