// filepath: dotnet/tests/ApiPilot.AspNetCore.Tests/Program.cs
// layer: TestInfrastructure | package: ApiPilot.AspNetCore.Tests | since: v0.2.0-alpha.0
// purpose: Entry point for the repository-local integration test harness
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (top-level statements)
//   Depends on : TestRunner.DiscoverAndRun
//   Used by    : dotnet run, CI test step
//   See also   : TestRunner.cs, TestAssert.cs
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.Tests;

return TestRunner.DiscoverAndRun();

