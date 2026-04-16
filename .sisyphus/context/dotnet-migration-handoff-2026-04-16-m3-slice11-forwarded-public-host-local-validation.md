# .NET Migration Session Handoff

## Session
- Date: 2026-04-16
- Milestone: M3 - Planner Domain Port Complete
- Atomic slice: M3 slice 11 - forwarded public-host normalization for guarded planner authorization (local implementation)

## Summary
- What was completed:
  - Added forwarded-header handling in `Program.cs` so the host now consumes forwarded public scheme/host before `/_internal/planner/calculate` runs the same-origin access check.
  - Kept `InternalPlannerAccessPolicy` unchanged; the policy still compares `Origin` to `request.Scheme://request.Host` after host-level normalization.
  - Added targeted integration coverage for matching forwarded public origin success and mismatched forwarded public origin rejection.
  - Added host-routing rollback coverage proving forwarded public authority does not disturb explicit `useInternalPlannerCalculate: false` shell injection.
  - Tightened README and architecture docs so deployments must overwrite or sanitize `X-Forwarded-Proto` and `X-Forwarded-Host`, restrict accepted public hostnames, and still record a real public-origin smoke before treating slice 11 as green.
  - Updated continuity docs to reflect that slice 11 local work is landed but the deployed smoke gate is still pending.
- What remains incomplete:
  - A real public-origin default-on guarded planner smoke is still required in the deployment environment.
  - A real public-origin rollback smoke with `Planner:UseInternalCalculate=false` is still required in the deployment environment.
  - M3 slice 11 should not be marked complete or rolled into slice 12 until those external smokes are recorded.

## Files / Areas Touched
- `SolverService/SatisfactoryTools.Solver.Api/Program.cs`
- `SolverService/SatisfactoryTools.Solver.Api.Tests/SolverApiTests.cs`
- `SolverService/SatisfactoryTools.Solver.Api.Tests/HostRoutingIntegrationTests.cs`
- `README.md`
- `docs/architecture.md`
- `CHANGELOG.md`
- `.sisyphus/plans/dotnet-migration-plan.md`
- `.sisyphus/context/dotnet-migration-resume-guide.md`
- `.sisyphus/context/dotnet-migration-route-parity-matrix.md`
- `.sisyphus/context/dotnet-migration-decision-log.md`
- `.sisyphus/context/dotnet-migration-planner-fixtures.md`
- `.sisyphus/context/dotnet-migration-handoff-2026-04-16-m3-slice11-forwarded-public-host-local-validation.md`

## Validation
- Repo-local verification completed:
  - `dotnet test "SolverService/SatisfactoryTools.Solver.Api.Tests/SatisfactoryTools.Solver.Api.Tests.csproj" --filter "FullyQualifiedName~HostRoutingIntegrationTests|FullyQualifiedName~InternalPlannerAccessPolicyTests|FullyQualifiedName~InternalPlannerCalculateRoute" --artifacts-path /tmp/satisfactorytools-slice11-targeted-artifacts --logger "console;verbosity=minimal"`
  - `yarn build`
  - `dotnet build "SolverService/SatisfactoryTools.Solver.Api.Tests/SatisfactoryTools.Solver.Api.Tests.csproj" --artifacts-path /tmp/satisfactorytools-slice11-browser-artifacts`
  - Playwright Chromium installed from the built test output with `dotnet exec --runtimeconfig ... --depsfile ... Microsoft.Playwright.dll install chromium`
  - `dotnet test "SolverService/SatisfactoryTools.Solver.Api.Tests/SatisfactoryTools.Solver.Api.Tests.csproj" --filter "FullyQualifiedName~PlannerBrowserRegressionTests" --artifacts-path /tmp/satisfactorytools-slice11-browser-artifacts --logger "console;verbosity=minimal"`
  - `dotnet test "SolverService/SatisfactoryTools.Solver.Api.Tests/SatisfactoryTools.Solver.Api.Tests.csproj" --artifacts-path /tmp/satisfactorytools-slice11-full-artifacts --logger "console;verbosity=minimal"`
- Results:
  - targeted host/internal suite: passed `27/27`
  - browser regressions: passed `4/4`
  - full `.NET` suite: passed `162/162`
  - frontend build: passed with existing Sass deprecation warnings only
- Review status:
  - repo-local goal verification: PASS
  - repo-local QA execution: PASS
  - repo-local code quality: PASS
  - repo-local security review: PASS with deployment-owned trusted-proxy caveat only
  - repo-local context mining: PASS

## Open Issues / Blockers
- External gate still pending: run one real public-origin guarded planner solve against the deployed host while the default-on path is active.
- External gate still pending: run one real public-origin rollback smoke against the deployed host with `Planner:UseInternalCalculate=false` and confirm planner traffic returns to `/v2/solver`.
- Deployment must overwrite or sanitize `X-Forwarded-Proto` and `X-Forwarded-Host` and restrict accepted public hostnames before the host-side same-origin check can be trusted in production.

## Exact Next Step
- Complete M3 slice 11 by recording the deployed public-origin default-on smoke and rollback confirmation, then update continuity docs to mark the slice complete.

## Resume Notes For The Next Session
1. Read `.sisyphus/plans/dotnet-migration-plan.md`
2. Read `.sisyphus/context/dotnet-migration-resume-guide.md`
3. Read `.sisyphus/context/dotnet-migration-route-parity-matrix.md`
4. Read `.sisyphus/context/dotnet-migration-decision-log.md`
5. Read `.sisyphus/context/dotnet-migration-planner-fixtures.md`
6. Read `.sisyphus/context/dotnet-migration-handoff-2026-04-16-m3-slice11-forwarded-public-host-local-validation.md`
7. Run and record the deployed public-origin default-on and rollback smokes before calling M3 slice 11 complete.
