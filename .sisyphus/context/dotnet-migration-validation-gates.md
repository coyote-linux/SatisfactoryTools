# .NET Migration Validation Gates

## Purpose

This file turns milestone gates into a concrete checklist.

## G0 -> G1

- [ ] `dotnet test "SolverService/SatisfactoryTools.Solver.Api.Tests/SatisfactoryTools.Solver.Api.Tests.csproj"` passes
- [ ] `yarn build` passes
- [ ] route/version behavior is documented from `src/Module/AppModule.ts`
- [ ] planner fixture catalog exists
- [ ] continuity docs exist under `.sisyphus/`

## G1 -> G2

- [ ] unified ASP.NET Core shell serves current app unchanged
- [ ] deep-link fallback works for representative routes
- [ ] static assets are served correctly
- [ ] `/v2/solver` and `/v2/share` still pass contract tests
- [ ] `dotnet test "Tests/SatisfactoryTools.Web.Host.Tests/SatisfactoryTools.Web.Host.Tests.csproj"` passes once created

## G2 -> G3

- [ ] route ownership is explicit for all route groups
- [ ] no ambiguous route overlaps remain
- [ ] planner beta route strategy is defined
- [ ] route ownership checks run through `Tests/SatisfactoryTools.Web.Host.Tests/SatisfactoryTools.Web.Host.Tests.csproj`

## G3 -> G4

- [ ] C# planner parity suite passes for representative fixtures
- [ ] version handling matches current semantics
- [ ] result graph and aggregates match expected current outputs
- [ ] `dotnet test "Tests/SatisfactoryTools.Planner.Parity.Tests/SatisfactoryTools.Planner.Parity.Tests.csproj"` passes once created

## G4 -> G5

- [ ] Blazor planner beta workflow smoke tests pass
- [ ] share load/save works
- [ ] local storage compatibility is acceptable
- [ ] known parity gaps are explicitly recorded
- [ ] `dotnet test "Tests/SatisfactoryTools.Web.E2E.Tests/SatisfactoryTools.Web.E2E.Tests.csproj"` passes once created

## G5 -> G6

- [ ] `/{version}/production` cutover is stable
- [ ] old share URLs still load
- [ ] rollback plan exists and is current
- [ ] planner smoke tests pass on the main route in `Tests/SatisfactoryTools.Web.E2E.Tests`

## G6 -> G7

- [ ] Angular runtime no longer owns required production routes
- [ ] remaining route migration inventory is complete
- [ ] DB/admin scope is approved and isolated from runtime migration
- [ ] host + E2E route suites both pass

## G7 -> G8

- [ ] JSON and DB outputs compare cleanly for selected datasets
- [ ] admin import/validate/diff/publish flow works in staging
- [ ] rollback plan for DB cutover exists
- [ ] `dotnet test "Tests/SatisfactoryTools.Data.Platform.Tests/SatisfactoryTools.Data.Platform.Tests.csproj"` passes once created

## Rule

Do not advance a milestone unless its checklist is green and the session handoff records the evidence.
