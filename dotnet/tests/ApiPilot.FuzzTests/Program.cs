// filepath: dotnet/tests/ApiPilot.FuzzTests/Program.cs
// layer: TestInfrastructure | package: ApiPilot.FuzzTests | since: v0.6.0
// purpose: Entry point for the repository-local fuzz and abuse harness
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (top-level statements)
//   Depends on : TestRunner.DiscoverAndRun
//   Used by    : dotnet run, CI test step
//   See also   : TestRunner.cs, TestAssert.cs
// -----------------------------------------------------------------------------

using ApiPilot.FuzzTests;

return TestRunner.DiscoverAndRun();

