// filepath: dotnet/tests/ApiPilot.ContractTests/Program.cs
// layer: TestInfrastructure | package: ApiPilot.ContractTests | since: v0.6.0
// purpose: Entry point for the repository-local contract-test harness
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (top-level statements)
//   Depends on : TestRunner.DiscoverAndRun
//   Used by    : dotnet run, CI test step
//   See also   : TestRunner.cs, TestAssert.cs
// -----------------------------------------------------------------------------

using ApiPilot.ContractTests;

return TestRunner.DiscoverAndRun();

