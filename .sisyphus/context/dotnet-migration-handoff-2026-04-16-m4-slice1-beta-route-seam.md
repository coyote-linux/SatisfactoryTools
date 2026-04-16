# .NET Migration Session Handoff

## Session
- Date: 2026-04-16
- Milestone: M4 - Blazor Planner Beta
- Atomic slice: M4 slice 1 - host-owned beta route seam

## Summary
- What was completed:
  - Added a host-owned `/beta/*` reservation to the ASP.NET route ownership policy so beta paths never shell-fallback into the legacy Angular HTML.
  - Added a dedicated host-only flag, `Planner:BetaRouteEnabled` / `Planner__BetaRouteEnabled`, with a default of `false`.
  - Added the smallest Blazor beta surface as a server-rendered placeholder for `GET /beta/production`, returning `404` when the beta flag is disabled.
  - Kept the new beta seam isolated from Angular: `window.SATISFACTORY_TOOLS_CONFIG` is unchanged, `/{version}/production` remains Angular-owned, and the current guarded planner rollout stays intact.
  - Added route ownership and host integration coverage for the beta seam while preserving `/v2/*` and `/_internal/planner/*` behavior.
  - Verified the seam locally with direct flag-off and flag-on host smoke: disabled `/beta/production` returns `404`, enabled `/beta/production` returns the placeholder marker, `/{version}/production` still serves Angular shell HTML, and unknown `/beta/*` stays hard-404.
- What remains incomplete:
  - The beta route is only a placeholder seam; no planner UI, graphing, share-link compatibility, or client migration work has been started.
  - M5 remains the cutover milestone for `/{version}/production`.

## Files / Areas Touched
- `SolverService/SatisfactoryTools.Solver.Api/Program.cs`
- `SolverService/SatisfactoryTools.Solver.Api/Services/HostRouteOwnershipPolicy.cs`
- `SolverService/SatisfactoryTools.Solver.Api/Components/BetaProductionPlaceholder.cs`
- `SolverService/SatisfactoryTools.Solver.Api.Tests/HostRouteOwnershipPolicyTests.cs`
- `SolverService/SatisfactoryTools.Solver.Api.Tests/HostRoutingIntegrationTests.cs`
- `SolverService/SatisfactoryTools.Solver.Api.Tests/TestApplicationFactoryExtensions.cs`
- `docs/architecture.md`
- `CHANGELOG.md`
- `.sisyphus/plans/dotnet-migration-plan.md`
- `.sisyphus/context/dotnet-migration-resume-guide.md`
- `.sisyphus/context/dotnet-migration-route-parity-matrix.md`
- `.sisyphus/context/dotnet-migration-decision-log.md`
- `.sisyphus/context/dotnet-migration-planner-fixtures.md`
- `.sisyphus/context/dotnet-migration-handoff-2026-04-16-m4-slice1-beta-route-seam.md`

## Validation
- Commands run:
  - `dotnet test "SolverService/SatisfactoryTools.Solver.Api.Tests/SatisfactoryTools.Solver.Api.Tests.csproj" --filter "FullyQualifiedName~HostRouteOwnershipPolicyTests|FullyQualifiedName~HostRoutingIntegrationTests" --artifacts-path /tmp/satisfactorytools-m4-slice1-targeted-artifacts --logger "console;verbosity=minimal"`
  - `dotnet test "SolverService/SatisfactoryTools.Solver.Api.Tests/SatisfactoryTools.Solver.Api.Tests.csproj" -c Release --artifacts-path /tmp/satisfactorytools-m4-slice1-full-artifacts --logger "console;verbosity=minimal"`
  - `yarn build`
  - local host smoke against Release output with `Planner__BetaRouteEnabled` unset and set to `true`
- Results:
  - route ownership and host integration tests: passed `45/45`
  - full solver API test suite: passed `168/168`
  - frontend build: passed with existing Sass/Bootstrap deprecation warnings only
  - local flag-off smoke: `GET /beta/production` => `404`; `GET /1.2/production` => `200` Angular shell HTML
  - local flag-on smoke: `GET /beta/production` => `200` with `beta-production-placeholder`; `GET /beta/not-a-route` => `404`
  - review pass: goal verification PASS, QA PASS, code quality PASS, security PASS, context mining PASS

## Open Issues / Blockers
- None identified inside M4 slice 1.

## Exact Next Step
- Start M4 slice 2 by replacing the placeholder body behind `/beta/production` with the first real isolated planner shell or layout while keeping `/{version}/production` Angular-owned and leaving the new `Planner:BetaRouteEnabled` rollback lever in place.

## Resume Notes For The Next Session
1. Read `.sisyphus/plans/dotnet-migration-plan.md`
2. Read `.sisyphus/context/dotnet-migration-resume-guide.md`
3. Read `.sisyphus/context/dotnet-migration-route-parity-matrix.md`
4. Read `.sisyphus/context/dotnet-migration-decision-log.md`
5. Read `.sisyphus/context/dotnet-migration-handoff-2026-04-16-m3-slice11-public-origin-completion.md`
6. Read `.sisyphus/context/dotnet-migration-handoff-2026-04-16-m4-slice1-beta-route-seam.md`
7. Continue building only the isolated `/beta/production` seam until M5 explicitly authorizes cutover.
