// filepath: dotnet/src/ApiPilot.AspNetCore/Configuration/ContentNegotiationOptionsValidator.cs
// layer: Configuration | package: ApiPilot.AspNetCore | since: v0.2.0-alpha.0
// purpose: Startup validation for ContentNegotiationOptions
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : Microsoft.Extensions.Options.IValidateOptions<ContentNegotiationOptions>
//   Depends on : ApiPilot.Core.Configuration.ContentNegotiationOptions
//   Used by    : AddApiPilotContentNegotiation via TryAddEnumerable
//   See also   : ContentNegotiationOptions.cs, ApiPilotContentNegotiationExtensions.cs
// -----------------------------------------------------------------------------

using ApiPilot.Core.Configuration;
using Microsoft.Extensions.Options;

namespace ApiPilot.AspNetCore.Configuration;

/// <summary>
/// Validates the ContentNegotiationOptions configuration at startup.
/// Fails when the acceptable response or request media type sets are
/// empty, when an entry is not a syntactically valid media type, when
/// the body-carrying method set is empty, when an entry is not a valid
/// HTTP method token, or when AcceptWildcard is false and the response
/// media type set is empty.
/// </summary>
/// <remarks>
/// This validator is registered by AddApiPilotContentNegotiation. To
/// replace or disable it, remove the IValidateOptions service from the
/// container and register your own before the container is built.
/// </remarks>
public sealed class ContentNegotiationOptionsValidator : IValidateOptions<ContentNegotiationOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, ContentNegotiationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (options.AcceptableResponseMediaTypes.Count == 0)
        {
            failures.Add("ContentNegotiationOptions.AcceptableResponseMediaTypes must not be empty.");
        }
        else
        {
            foreach (var mediaType in options.AcceptableResponseMediaTypes)
            {
                if (!IsValidMediaType(mediaType))
                {
                    failures.Add($"ContentNegotiationOptions.AcceptableResponseMediaTypes entry '{mediaType}' is not a valid media type.");
                }
            }
        }

        if (options.AcceptableRequestMediaTypes.Count == 0)
        {
            failures.Add("ContentNegotiationOptions.AcceptableRequestMediaTypes must not be empty.");
        }
        else
        {
            foreach (var mediaType in options.AcceptableRequestMediaTypes)
            {
                if (!IsValidMediaType(mediaType))
                {
                    failures.Add($"ContentNegotiationOptions.AcceptableRequestMediaTypes entry '{mediaType}' is not a valid media type.");
                }
            }
        }

        if (options.BodyCarryingMethods.Count == 0)
        {
            failures.Add("ContentNegotiationOptions.BodyCarryingMethods must not be empty.");
        }
        else
        {
            foreach (var method in options.BodyCarryingMethods)
            {
                if (!IsValidHttpMethod(method))
                {
                    failures.Add($"ContentNegotiationOptions.BodyCarryingMethods entry '{method}' is not a valid HTTP method.");
                }
            }
        }

        if (!options.AcceptWildcard && options.AcceptableResponseMediaTypes.Count == 0)
        {
            failures.Add("ContentNegotiationOptions: AcceptWildcard is false and AcceptableResponseMediaTypes is empty, so no Accept header can ever be acceptable.");
        }

        if (failures.Count > 0)
        {
            return ValidateOptionsResult.Fail(failures);
        }

        return ValidateOptionsResult.Success;
    }

    private static bool IsValidMediaType(string mediaType)
    {
        if (string.IsNullOrEmpty(mediaType))
        {
            return false;
        }
        if (mediaType.IndexOf('/') < 1)
        {
            return false;
        }
        foreach (var c in mediaType)
        {
            if (char.IsWhiteSpace(c))
            {
                return false;
            }
        }
        return true;
    }

    private static bool IsValidHttpMethod(string method)
    {
        if (string.IsNullOrEmpty(method))
        {
            return false;
        }
        foreach (var c in method)
        {
            if (!char.IsLetter(c) && c != '-')
            {
                return false;
            }
        }
        return true;
    }
}

