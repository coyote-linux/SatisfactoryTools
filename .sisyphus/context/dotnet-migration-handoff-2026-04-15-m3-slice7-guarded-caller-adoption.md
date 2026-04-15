# .NET Migration Session Handoff

## Session
- Date: 2026-04-15
- Milestone: M3 - Planner Domain Port Complete
- Atomic slice: M3 slice 7 - guarded non-test caller adoption of the internal planner runtime route

## Summary
- What was completed:
  - Added the first guarded non-test runtime caller of `POST /_internal/planner/calculate` in the legacy Angular production planner.
  - Kept `Solver.solveProduction()` and `/v2/solver` unchanged as the default raw compatibility path.
  - Added host-injected default-off shell config for `useInternalPlannerCalculate` and `internalPlannerCalculateUrl` so the guarded path can be enabled without changing public route ownership or `/v2/*` contracts.
  - Added a planner-specific Angular client plus a narrow local result-view-model seam so the guarded path can consume planner-facing `details` and server-built `visualization` without broad UI cutover.
  - Extended the internal planner route response to include optional debug output and wired the Angular guarded path to preserve no-result debug behavior.
  - Fixed the first review-pass regressions by rebinding the visualization network to live data sets so ELK positions apply again, parsing the raw PHP shell flag as a real boolean, and making share-loaded planner tabs stay active instead of dropping users onto an empty default tab.
  - Updated local Docker Compose so guarded internal planner testing can be enabled through environment overrides.
- What remains incomplete:
  - The guarded internal planner caller is still default-off and has not been widened to default-on behavior.
  - No Blazor/UI cutover work has started.

## Files / Areas Touched
- `src/Globals.ts`
- `src/Solver/Solver.ts`
- `src/Tools/Production/IProductionPlanResult.ts`
- `src/Tools/Production/PlannerCalculationClient.ts`
- `src/Module/Controllers/ProductionController.ts`
- `src/Tools/Production/ProductionTab.ts`
- `src/Tools/Production/Result/IVisEdge.ts`
- `src/Module/Components/VisualizationComponentController.ts`
- `www/index.php`
- `docker-compose.yml`
- `SolverService/SatisfactoryTools.Solver.Api/Program.cs`
- `SolverService/SatisfactoryTools.Solver.Api/Services/SpaShellRenderer.cs`
- `SolverService/SatisfactoryTools.Solver.Api/Services/InternalPlannerCalculationResponse.cs`
- `SolverService/SatisfactoryTools.Solver.Api.Tests/TestApplicationFactoryExtensions.cs`
- `SolverService/SatisfactoryTools.Solver.Api.Tests/HostRoutingIntegrationTests.cs`
- `SolverService/SatisfactoryTools.Solver.Api.Tests/InternalPlannerCalculationServiceTests.cs`
- `SolverService/SatisfactoryTools.Solver.Api.Tests/SolverApiTests.cs`
- `.sisyphus/plans/dotnet-migration-plan.md`
- `.sisyphus/context/dotnet-migration-resume-guide.md`
- `.sisyphus/context/dotnet-migration-route-parity-matrix.md`
- `.sisyphus/context/dotnet-migration-decision-log.md`
- `.sisyphus/context/dotnet-migration-planner-fixtures.md`
- `.sisyphus/context/dotnet-migration-handoff-2026-04-15-m3-slice7-guarded-caller-adoption.md`

## Validation
- Commands run:
  - `dotnet test "SolverService/SatisfactoryTools.Solver.Api.Tests/SatisfactoryTools.Solver.Api.Tests.csproj" --filter "FullyQualifiedName~HostRoutingIntegrationTests|FullyQualifiedName~SolverApiTests|FullyQualifiedName~InternalPlannerCalculationServiceTests" --artifacts-path /tmp/satisfactorytools-m3-slice7-targeted-artifacts --logger "console;verbosity=minimal"`
  - `dotnet test "SolverService/SatisfactoryTools.Solver.Api.Tests/SatisfactoryTools.Solver.Api.Tests.csproj" --artifacts-path /tmp/satisfactorytools-m3-slice7-full-artifacts --logger "console;verbosity=minimal"`
  - `yarn build`
  - Live guarded host smoke on `http://127.0.0.1:8092`
- Results:
  - targeted slice-7 tests: passed `63/63`
  - full `dotnet test` with isolated artifact path: passed `147/147`
  - `yarn build`: passed (existing Sass/Bootstrap deprecation warnings remain)
  - live guarded host smoke: shell config injected `useInternalPlannerCalculate: true`, browser share load still hit `/v2/share`, planner solve traffic hit `/_internal/planner/calculate`, and the shared `F003` planner tab rendered successfully under the guarded path
  - internal no-result debug smoke: `F007` through `/_internal/planner/calculate?showDebugOutput=true` returned `details.hasOutput=false` plus the expected debug message
  - regression-fix smoke: raw PHP boolean parsing now emits `false` for `USE_INTERNAL_PLANNER_CALCULATE=false`; guarded share-load lands directly on the shared planner tab; vis-network node positions update to distinct ELK coordinates in the live Angular visualization controller

## Review Status
- Oracle review accepted the slice-7 boundary only if the guarded internal-route adoption stayed above `Solver.solveProduction()` and did not retrofit planner-facing results back into the public `/v2/solver` contract.
- The implemented slice follows that boundary: the guarded planner client is local to the Angular production planner, while `/v2/solver` remains the untouched legacy path.

## Last Green State
- Commit SHA: `fc22c4f`
- Why this is green:
  - This is the last committed branch state before the slice-7 working-tree changes; slice-7 validation is green in the current working tree with no public `/v2/*` contract changes applied.

## Open Issues / Blockers
- The guarded internal planner caller is intentionally default-off until wider parity confidence is built.
- `/_internal/planner/calculate` now includes optional debug payloads for guarded runtime parity; keep that response internal-only and separate from `/v2/solver`.
- The Angular planner still has two runtime solve paths; do not delete the legacy path until a later explicit default-on or cutover slice proves the guarded path ready.

## Decisions Updated
- M3 slice 7 is accepted as a guarded planner-specific client above `Solver.solveProduction()` rather than as a retrofit of planner-facing output into the public `/v2/solver` envelope.

## Exact Next Slice
- M3 slice 8 - deepen guarded planner parity coverage and default-on readiness for the internal planner caller without changing `/v2/*` contracts or starting Blazor UI cutover.

## Resume Notes For The Next Session
1. Read `.sisyphus/plans/dotnet-migration-plan.md`
2. Read `.sisyphus/context/dotnet-migration-resume-guide.md`
3. Read `.sisyphus/context/dotnet-migration-planner-fixtures.md`
4. Read `.sisyphus/context/dotnet-migration-route-parity-matrix.md`
5. Read `.sisyphus/context/dotnet-migration-decision-log.md`
6. Read `.sisyphus/context/dotnet-migration-handoff-2026-04-15-m3-slice7-guarded-caller-adoption.md`
7. Keep the guarded planner client default-off while tightening parity coverage around no-result/debug behavior, visualization stability, and default-on readiness.
