// filepath: dotnet/src/ApiPilot.AspNetCore/Diagnostics/ApiPilotSafeLogger.cs
// layer: Diagnostics | package: ApiPilot.AspNetCore | since: v0.5.0
// purpose: ILogger decorator that redacts named sensitive fields in structured log state
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : Microsoft.Extensions.Logging.ILogger
//   Depends on : ApiPilotRedactionPolicy, Microsoft.Extensions.Logging
//   Used by    : application setup that chooses to wrap its logger
//   See also   : ApiPilotRedactionPolicy.cs, docs/observability.md (Phase 4.3), CHANGELOG.md (findings A-210, A-211, A-215)
// -----------------------------------------------------------------------------
//
// SCOPE
//   This decorator redacts the VALUES of structured state entries whose
//   KEYS match ApiPilotRedactionPolicy.RedactedNames. It does not redact
//   opaque string content. It does not scrub exception messages. It does
//   not modify scopes. Callers must not place secrets in fields whose
//   names are not in the redacted set.

using Microsoft.Extensions.Logging;

namespace ApiPilot.AspNetCore.Diagnostics;

/// <summary>
/// An <see cref="ILogger"/> decorator that redacts the values of
/// structured state entries whose keys match
/// <see cref="ApiPilotRedactionPolicy.RedactedNames"/>.
/// </summary>
/// <remarks>
/// <para>
/// The decorator preserves the original message template entry
/// (<c>{OriginalFormat}</c>), the non-redacted structured entries, and
/// the exception instance. It replaces the VALUE of each redacted entry
/// with <see cref="ApiPilotRedactionPolicy.RedactedValue"/>.
/// </para>
/// <para>
/// The formatter used after redaction renders the preserved template
/// against the redacted values. This is a documented formatting contract
/// of this decorator. It is not a claim of strict
/// <c>LoggerMessage</c> equivalence; applications that require strict
/// rendering should supply their own decorator.
/// </para>
/// <para>
/// SCOPE. This decorator redacts by KEY NAME. A secret embedded in
/// opaque content (an exception message, a scope payload, a value under
/// a generic key) is outside its reach. The limitation is part of the
/// contract.
/// </para>
/// </remarks>
public sealed class ApiPilotSafeLogger : ILogger
{
    private readonly ILogger _inner;

    /// <summary>
    /// Creates a decorator over the supplied inner logger.
    /// </summary>
    /// <param name="inner">The logger to which redacted entries are forwarded. Must not be null.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="inner"/> is null.
    /// </exception>
    public ApiPilotSafeLogger(ILogger inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        _inner = inner;
    }

    /// <summary>
    /// Wraps an inner logger in an <see cref="ApiPilotSafeLogger"/>.
    /// </summary>
    /// <param name="inner">The logger to wrap. Must not be null.</param>
    /// <returns>The wrapped logger.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="inner"/> is null.
    /// </exception>
    public static ILogger Wrap(ILogger inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        return new ApiPilotSafeLogger(inner);
    }

    /// <inheritdoc/>
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return _inner.BeginScope(state);
    }

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel)
    {
        return _inner.IsEnabled(logLevel);
    }

    /// <inheritdoc/>
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        if (state is not IReadOnlyList<KeyValuePair<string, object?>> pairs)
        {
            _inner.Log(logLevel, eventId, state, exception, formatter);
            return;
        }

        var anyRedacted = false;
        for (var i = 0; i < pairs.Count; i++)
        {
            if (ApiPilotRedactionPolicy.RedactedNames.Contains(pairs[i].Key))
            {
                anyRedacted = true;
                break;
            }
        }

        if (!anyRedacted)
        {
            _inner.Log(logLevel, eventId, state, exception, formatter);
            return;
        }

        var redacted = new List<KeyValuePair<string, object?>>(pairs.Count);
        for (var i = 0; i < pairs.Count; i++)
        {
            var kv = pairs[i];
            if (ApiPilotRedactionPolicy.RedactedNames.Contains(kv.Key))
            {
                redacted.Add(new KeyValuePair<string, object?>(kv.Key, ApiPilotRedactionPolicy.RedactedValue));
            }
            else
            {
                redacted.Add(kv);
            }
        }

        var redactedState = new ApiPilotRedactedLogState(redacted);
        _inner.Log(
            logLevel,
            eventId,
            redactedState,
            exception,
            static (s, e) => FormatRedactedState(s, e));
    }

    private static string FormatRedactedState(ApiPilotRedactedLogState state, Exception? error)
    {
        var template = state.FindTemplate();
        if (template is not null)
        {
            return RenderTemplate(template, state);
        }

        var parts = new List<string>(state.Count);
        for (var i = 0; i < state.Count; i++)
        {
            var kv = state[i];
            parts.Add(kv.Key + ": " + (kv.Value?.ToString() ?? "null"));
        }
        return string.Join(", ", parts);
    }

    private static string RenderTemplate(string template, ApiPilotRedactedLogState state)
    {
        var result = new System.Text.StringBuilder(template.Length);
        var i = 0;
        while (i < template.Length)
        {
            var c = template[i];
            if (c == '{' )
            {
                var close = template.IndexOf('}', i + 1);
                if (close > i)
                {
                    var key = template.Substring(i + 1, close - i - 1);
                    var value = state.FindValue(key);
                    if (value is not null)
                    {
                        result.Append(value.ToString());
                    }
                    else
                    {
                        result.Append(template, i, close - i + 1);
                    }
                    i = close + 1;
                    continue;
                }
            }
            result.Append(c);
            i++;
        }
        return result.ToString();
    }
}

internal sealed class ApiPilotRedactedLogState : IReadOnlyList<KeyValuePair<string, object?>>
{
    private readonly List<KeyValuePair<string, object?>> _entries;

    public ApiPilotRedactedLogState(List<KeyValuePair<string, object?>> entries)
    {
        _entries = entries;
    }

    public KeyValuePair<string, object?> this[int index] => _entries[index];

    public int Count => _entries.Count;

    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => _entries.GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _entries.GetEnumerator();

    public string? FindTemplate()
    {
        for (var i = 0; i < _entries.Count; i++)
        {
            if (_entries[i].Key == "{OriginalFormat}" && _entries[i].Value is string s)
            {
                return s;
            }
        }
        return null;
    }

    public object? FindValue(string key)
    {
        for (var i = 0; i < _entries.Count; i++)
        {
            if (_entries[i].Key == key)
            {
                return _entries[i].Value;
            }
        }
        return null;
    }
}

