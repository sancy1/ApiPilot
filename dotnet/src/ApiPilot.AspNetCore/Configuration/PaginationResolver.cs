// filepath: dotnet/src/ApiPilot.AspNetCore/Configuration/PaginationResolver.cs
// layer: Configuration | package: ApiPilot.AspNetCore | since: v0.2.0-alpha.0
// purpose: Resolves effective PaginationOptions from global defaults plus endpoint overrides
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (internal static class)
//   Depends on : ApiPilot.Core.Pagination.PaginationOptions, PaginationOverrides, PaginationMetadataAttribute
//   Used by    : PaginationEndpointFilter
//   See also   : PaginationOptions.cs, PaginationOverrides.cs, PaginationMetadataAttribute.cs
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.EndpointMetadata;
using ApiPilot.Core.Pagination;

namespace ApiPilot.AspNetCore.Configuration;

/// <summary>
/// Resolves the effective pagination options for a request by layering
/// endpoint-specific overrides on top of the global configuration.
/// </summary>
internal static class PaginationResolver
{
    /// <summary>
    /// Applies overrides on top of the global options. When
    /// <paramref name="overrides"/> is null, the global instance is
    /// returned unchanged.
    /// </summary>
    public static PaginationOptions Resolve(PaginationOptions global, PaginationOverrides? overrides)
    {
        ArgumentNullException.ThrowIfNull(global);

        if (overrides is null)
        {
            return global;
        }

        return new PaginationOptions
        {
            DefaultPageSize = overrides.DefaultPageSize ?? global.DefaultPageSize,
            MaxPageSize = overrides.MaxPageSize ?? global.MaxPageSize,
            StrictQueryValidation = overrides.StrictQueryValidation ?? global.StrictQueryValidation,
            SuccessStatusCode = overrides.SuccessStatusCode ?? global.SuccessStatusCode,
            ParameterNames = overrides.ParameterNames ?? global.ParameterNames,
            PageNumberBase = overrides.PageNumberBase ?? global.PageNumberBase,
            SortDirectionParser = overrides.SortDirectionParser ?? global.SortDirectionParser,
            SortParser = overrides.SortParser ?? global.SortParser,
            IsFilterParameter = overrides.IsFilterParameter ?? global.IsFilterParameter,
            IntegerParser = overrides.IntegerParser ?? global.IntegerParser,
        };
    }

    /// <summary>
    /// Merges two sources of overrides into one. The fluent source carries
    /// the fluent-declared overrides (attached to the endpoint via the
    /// WithApiPilotPagination extension). The attribute source carries the
    /// attribute-declared overrides. For the primitive fields, the attribute
    /// wins because it is the more specific declaration on the action. For
    /// the delegate and object fields, the fluent source is the only one that
    /// can carry them (attributes cannot express delegates or nested objects),
    /// so the fluent value is used.
    /// </summary>
    /// <param name="fluent">
    /// The fluent-declared overrides, or null when none were supplied.
    /// </param>
    /// <param name="attribute">
    /// The attribute-declared overrides, or null when none were supplied.
    /// </param>
    /// <returns>
    /// A merged PaginationOverrides, or null when both inputs are null.
    /// </returns>
    public static PaginationOverrides? MergeOverrides(
        PaginationOverrides? fluent,
        PaginationOverrides? attribute)
    {
        if (fluent is null && attribute is null)
        {
            return null;
        }

        if (fluent is null)
        {
            return attribute;
        }

        if (attribute is null)
        {
            return fluent;
        }

        return new PaginationOverrides
        {
            DefaultPageSize = attribute.DefaultPageSize ?? fluent.DefaultPageSize,
            MaxPageSize = attribute.MaxPageSize ?? fluent.MaxPageSize,
            StrictQueryValidation = attribute.StrictQueryValidation ?? fluent.StrictQueryValidation,
            SuccessStatusCode = attribute.SuccessStatusCode ?? fluent.SuccessStatusCode,
            PageNumberBase = attribute.PageNumberBase ?? fluent.PageNumberBase,
            ParameterNames = fluent.ParameterNames,
            SortDirectionParser = fluent.SortDirectionParser,
            SortParser = fluent.SortParser,
            IsFilterParameter = fluent.IsFilterParameter,
            IntegerParser = fluent.IntegerParser,
        };
    }

    /// <summary>
    /// Converts a PaginationMetadataAttribute into a PaginationOverrides
    /// instance. Returns null when the attribute is null or when none of
    /// its values would override the global configuration.
    /// </summary>
    /// <remarks>
    /// Only primitive-value overrides are read from the attribute.
    /// Delegate overrides (SortDirectionParser, SortParser, IsFilterParameter,
    /// IntegerParser) and the ParameterNames object cannot be set on an
    /// attribute; applications that need them use the fluent extension
    /// WithApiPilotPagination.
    /// </remarks>
    public static PaginationOverrides? FromAttribute(PaginationMetadataAttribute? attr)
    {
        if (attr is null)
        {
            return null;
        }

        var hasOverride = false;
        int? defaultPageSize = null;
        int? maxPageSize = null;
        bool? strict = null;
        int? successStatusCode = null;
        int? pageNumberBase = null;

        if (attr.DefaultPageSize >= 0)
        {
            defaultPageSize = attr.DefaultPageSize;
            hasOverride = true;
        }

        if (attr.MaxPageSize >= 0)
        {
            maxPageSize = attr.MaxPageSize;
            hasOverride = true;
        }

        if (attr.StrictQueryValidation == TriState.True)
        {
            strict = true;
            hasOverride = true;
        }
        else if (attr.StrictQueryValidation == TriState.False)
        {
            strict = false;
            hasOverride = true;
        }

        if (attr.SuccessStatusCode >= 0)
        {
            successStatusCode = attr.SuccessStatusCode;
            hasOverride = true;
        }

        if (attr.PageNumberBase >= 0)
        {
            pageNumberBase = attr.PageNumberBase;
            hasOverride = true;
        }

        if (!hasOverride)
        {
            return null;
        }

        return new PaginationOverrides
        {
            DefaultPageSize = defaultPageSize,
            MaxPageSize = maxPageSize,
            StrictQueryValidation = strict,
            SuccessStatusCode = successStatusCode,
            PageNumberBase = pageNumberBase,
        };
    }
}

