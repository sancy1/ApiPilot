// filepath: dotnet/tests/ApiPilot.Core.Tests/Program.cs
// layer: TestInfrastructure | package: ApiPilot.Core.Tests | since: v0.1.0-alpha.0
// purpose: Entry point for the repository-local test harness
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (top-level statements)
//   Depends on : TestRunner.DiscoverAndRun
//   Used by    : dotnet run, CI test step
//   See also   : TestRunner.cs, TestAssert.cs
// -----------------------------------------------------------------------------

using ApiPilot.Core.Tests;

return TestRunner.DiscoverAndRun();

