// filepath: dotnet/src/ApiPilot.AspNetCore/EndpointMetadata/PaginationEndpointFilter.cs
// layer: EndpointMetadata | package: ApiPilot.AspNetCore | since: v0.2.0-alpha.0
// purpose: Minimal API endpoint filter that parses and validates pagination query parameters
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : Microsoft.AspNetCore.Http.IEndpointFilter
//   Depends on : PaginationOptions, PaginationOverrides, PaginationResolver,
//                PageRequest, SortRequest, FilterRequest, ErrorResponseResult
//   Used by    : PaginationEndpointExtensions (WithApiPilotPagination)
//   See also   : PaginationOptions.cs, PaginationResolver.cs, SPEC.md (query parameters)
// -----------------------------------------------------------------------------

using System.Globalization;
using ApiPilot.AspNetCore.Configuration;
using ApiPilot.AspNetCore.Results;
using ApiPilot.Core.Errors;
using ApiPilot.Core.Metadata;
using ApiPilot.Core.Pagination;
using ApiPilot.Core.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace ApiPilot.AspNetCore.EndpointMetadata;

/// <summary>
/// A minimal API endpoint filter that reads pagination query parameters,
/// validates them against the effective options for the endpoint (global
/// plus any per-endpoint overrides), and either short-circuits the
/// pipeline with a standard VALIDATION_ERROR envelope or stores the
/// validated PageRequest, SortRequest, and FilterRequest on the
/// HttpContext for the endpoint handler to read.
/// </summary>
/// <remarks>
/// The filter consults every Option D override exposed by PaginationOptions:
/// ParameterNames, PageNumberBase, SortDirectionParser, SortParser,
/// IsFilterParameter, IntegerParser, StrictQueryValidation, and the
/// page size defaults and maximums. Per-endpoint overrides are read from
/// two sources and merged: the fluent PaginationOverrides attached via
/// WithApiPilotPagination, and the PaginationMetadataAttribute attached
/// via WithMetadata. The default behavior is used when an application
/// supplies no configuration.
/// </remarks>
public sealed class PaginationEndpointFilter : IEndpointFilter
{
    private readonly IOptions<PaginationOptions> _globalOptions;

    /// <summary>Key under which the validated PageRequest is stored.</summary>
    internal static readonly object PageRequestKey = new();

    /// <summary>Key under which the validated SortRequest is stored.</summary>
    internal static readonly object SortRequestKey = new();

    /// <summary>Key under which the validated FilterRequest is stored.</summary>
    internal static readonly object FilterRequestKey = new();

    /// <summary>Key under which the effective PaginationOptions is stored.</summary>
    internal static readonly object EffectiveOptionsKey = new();

    /// <summary>
    /// Creates the filter with the global pagination options.
    /// </summary>
    /// <param name="globalOptions">The global options. Must not be null.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="globalOptions"/> is null.
    /// </exception>
    public PaginationEndpointFilter(IOptions<PaginationOptions> globalOptions)
    {
        ArgumentNullException.ThrowIfNull(globalOptions);
        _globalOptions = globalOptions;
    }

    /// <inheritdoc />
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var httpContext = context.HttpContext;

        var endpoint = httpContext.GetEndpoint();
        var fluentOverrides = endpoint?.Metadata.GetMetadata<PaginationOverrides>();
        var attribute = endpoint?.Metadata.GetMetadata<PaginationMetadataAttribute>();
        var attributeOverrides = PaginationResolver.FromAttribute(attribute);
        var merged = PaginationResolver.MergeOverrides(fluentOverrides, attributeOverrides);
        var effective = PaginationResolver.Resolve(_globalOptions.Value, merged);

        httpContext.Items[EffectiveOptionsKey] = effective;

        var names = effective.ParameterNames;
        var query = httpContext.Request.Query;
        var rawPage = query[names.Page].ToString();
        var rawPageSize = query[names.PageSize].ToString();
        var rawSort = query[names.Sort].ToString();
        var rawDirection = query[names.Direction].ToString();

        var errors = new List<ApiErrorField>();
        var parser = effective.IntegerParser ?? DefaultIntegerParser;

        int? page = null;
        if (!string.IsNullOrEmpty(rawPage))
        {
            page = parser(rawPage);
            if (page is null)
            {
                errors.Add(ApiErrorField.WithMessage(names.Page, "The page number is not a valid integer."));
            }
        }

        int? pageSize = null;
        if (!string.IsNullOrEmpty(rawPageSize))
        {
            pageSize = parser(rawPageSize);
            if (pageSize is null)
            {
                errors.Add(ApiErrorField.WithMessage(names.PageSize, "The page size is not a valid integer."));
            }
        }

        if (page is not null && page < effective.PageNumberBase)
        {
            errors.Add(ApiErrorField.WithMessage(names.Page,
                $"The page number must be at least {effective.PageNumberBase}."));
        }

        if (pageSize is not null)
        {
            if (pageSize < 1)
            {
                errors.Add(ApiErrorField.WithMessage(names.PageSize, "The page size must be at least 1."));
            }
            else if (pageSize > effective.MaxPageSize)
            {
                errors.Add(ApiErrorField.WithMessage(names.PageSize,
                    $"The page size must not exceed {effective.MaxPageSize}."));
            }
        }

        SortRequest? sort = null;
        if (!string.IsNullOrEmpty(rawSort) || !string.IsNullOrEmpty(rawDirection))
        {
            var sortParser = effective.SortParser ?? BuildDefaultSortParser(effective.SortDirectionParser);
            sort = sortParser(rawSort, rawDirection);
            if (sort is null)
            {
                errors.Add(ApiErrorField.WithMessage(names.Sort, "The sort expression is not valid."));
            }
        }

        var filters = new List<KeyValuePair<string, string>>();
        var isFilter = effective.IsFilterParameter;
        foreach (var kv in query)
        {
            var key = kv.Key;
            if (key == names.Page || key == names.PageSize || key == names.Sort || key == names.Direction)
            {
                continue;
            }

            var accepted = isFilter is not null ? isFilter(key) : (bool?)null;
            if (accepted == false)
            {
                if (effective.StrictQueryValidation)
                {
                    errors.Add(ApiErrorField.WithMessage(key, "This query parameter is not recognized."));
                }
                continue;
            }

            if (accepted is null && effective.StrictQueryValidation)
            {
                errors.Add(ApiErrorField.WithMessage(key, "This query parameter is not recognized."));
                continue;
            }

            filters.Add(new KeyValuePair<string, string>(key, kv.Value.ToString()));
        }

        if (errors.Count > 0)
        {
            var meta = new ResponseMetadata { RequestId = httpContext.TraceIdentifier };
            var response = ValidationResponseFactory.FromFields(errors, meta);
            return new ErrorResponseResult(response);
        }

        var validatedPage = PageRequest.FromQuery(
            page.HasValue ? page.Value - effective.PageNumberBase + 1 : (int?)null,
            pageSize,
            effective);

        httpContext.Items[PageRequestKey] = validatedPage;
        if (sort is not null)
        {
            httpContext.Items[SortRequestKey] = sort;
        }
        httpContext.Items[FilterRequestKey] = filters.Count == 0
            ? FilterRequest.Empty
            : FilterRequest.Create(filters);

        return await next(context);
    }

    private static int? DefaultIntegerParser(string value)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
            ? result
            : (int?)null;
    }

    private static Func<string, string, SortRequest?> BuildDefaultSortParser(
        Func<string, SortDirection?>? directionParser)
    {
        return (field, direction) =>
        {
            if (string.IsNullOrEmpty(field))
            {
                return null;
            }

            var resolved = directionParser is not null
                ? directionParser(direction)
                : ParseDefaultDirection(direction);

            if (resolved is null)
            {
                return null;
            }

            try
            {
                return SortRequest.Create(field, resolved.Value);
            }
            catch (ArgumentException)
            {
                return null;
            }
        };
    }

    private static SortDirection? ParseDefaultDirection(string direction)
    {
        return SortDirectionExtensions.TryParseWireValue(direction, out var parsed)
            ? parsed
            : (SortDirection?)null;
    }
}

