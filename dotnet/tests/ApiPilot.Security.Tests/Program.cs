// filepath: dotnet/tests/ApiPilot.Security.Tests/Program.cs
// layer: TestInfrastructure | package: ApiPilot.Security.Tests | since: v0.3.0
// purpose: Entry point for the repository-local test harness
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (top-level statements)
//   Depends on : TestRunner.DiscoverAndRun
//   Used by    : dotnet run, CI test step
//   See also   : TestRunner.cs, TestAssert.cs
// -----------------------------------------------------------------------------

using ApiPilot.Security.Tests;

return TestRunner.DiscoverAndRun();

