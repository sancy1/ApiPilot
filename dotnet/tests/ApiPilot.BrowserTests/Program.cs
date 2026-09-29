// filepath: dotnet/tests/ApiPilot.BrowserTests/Program.cs
// layer: TestInfrastructure | package: ApiPilot.BrowserTests | since: v0.6.0
// purpose: Entry point for the repository-local browser security harness
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (top-level statements)
//   Depends on : TestRunner.DiscoverAndRun
//   Used by    : dotnet run, CI test step
//   See also   : TestRunner.cs, TestAssert.cs
// -----------------------------------------------------------------------------

using ApiPilot.BrowserTests;

return TestRunner.DiscoverAndRun();

