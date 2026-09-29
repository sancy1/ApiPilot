// filepath: dotnet/tests/ApiPilot.AspNetCore.Tests/Diagnostics/ApiPilotLogEventsTests.cs
// layer: Diagnostics.Tests | package: ApiPilot.AspNetCore.Tests | since: v0.5.0
// purpose: Tests for the ApiPilotLogEvents catalogue
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : ApiPilotLogEvents, System.Reflection
//   Used by    : the test harness
//   See also   : ApiPilotLogEvents.cs, CHANGELOG.md (finding A-214)
// -----------------------------------------------------------------------------

using System.Reflection;
using ApiPilot.AspNetCore.Diagnostics;

namespace ApiPilot.AspNetCore.Tests.Diagnostics;

[TestClass]
public sealed class ApiPilotLogEventsTests
{
    private static readonly FieldInfo[] s_fields =
        typeof(ApiPilotLogEvents).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

    [Test]
    public void Catalogue_ContainsExactly17Constants()
    {
        var intFields = new List<FieldInfo>();
        foreach (var field in s_fields)
        {
            if (field.IsLiteral && field.FieldType == typeof(int))
            {
                intFields.Add(field);
            }
        }
        TestAssert.Equal(18, intFields.Count);
    }

    [Test]
    public void Catalogue_EveryValueIsPairwiseDistinct()
    {
        var seen = new Dictionary<int, string>();
        foreach (var field in s_fields)
        {
            if (!field.IsLiteral || field.FieldType != typeof(int))
            {
                continue;
            }
            var value = (int)field.GetRawConstantValue()!;
            var name = field.Name;
            if (seen.TryGetValue(value, out var first))
            {
                throw new TestFailureException(
                    "Event ID " + value + " is shared by two constants: " + first + " and " + name);
            }
            seen[value] = name;
        }
        TestAssert.Equal(18, seen.Count);
    }

    [Test]
    public void Catalogue_ExceptionMiddlewareBlock_Is1001To1007()
    {
        TestAssert.Equal(1001, ApiPilotLogEvents.ExceptionMappedError);
        TestAssert.Equal(1002, ApiPilotLogEvents.ExceptionMappedWarning);
        TestAssert.Equal(1003, ApiPilotLogEvents.ExceptionMappedInformation);
        TestAssert.Equal(1004, ApiPilotLogEvents.ExceptionMappedDebug);
        TestAssert.Equal(1005, ApiPilotLogEvents.ExceptionMappedCritical);
        TestAssert.Equal(1006, ApiPilotLogEvents.ExceptionMappedTrace);
        TestAssert.Equal(1007, ApiPilotLogEvents.ExceptionAfterResponseStarted);
    }

    [Test]
    public void Catalogue_ComponentScopedValuesMatchShippedComponents()
    {
        TestAssert.Equal(2001, ApiPilotLogEvents.CorrelationInvalidIdWarning);
        TestAssert.Equal(2002, ApiPilotLogEvents.CorrelationInvalidIdRejected);
        TestAssert.Equal(3001, ApiPilotLogEvents.ContentNegotiationUnacceptableAccept);
        TestAssert.Equal(3002, ApiPilotLogEvents.ContentNegotiationUnsupportedMediaType);
        TestAssert.Equal(5001, ApiPilotLogEvents.CsrfRejected);
        TestAssert.Equal(6001, ApiPilotLogEvents.CsrfTransitionBindingUnresolved);
        TestAssert.Equal(6002, ApiPilotLogEvents.CsrfTransitionNoStore);
        TestAssert.Equal(7001, ApiPilotLogEvents.OriginRejected);
        TestAssert.Equal(8001, ApiPilotLogEvents.FetchMetadataRejected);
        TestAssert.Equal(9100, ApiPilotLogEvents.SecurityDiagnosticsWarning);
    }
}

