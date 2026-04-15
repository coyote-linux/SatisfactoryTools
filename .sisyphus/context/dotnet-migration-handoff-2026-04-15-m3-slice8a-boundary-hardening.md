# .NET Migration Session Handoff

## Session
- Date: 2026-04-15
- Milestone: M3 - Planner Domain Port Complete
- Atomic slice: M3 slice 8a - guarded planner boundary hardening

## Summary
- What was completed:
  - Tightened the guarded planner/result type boundary so guarded Angular results now remain visualization-backed instead of graph-shaped.
  - Hardened the shared-entry bootstrap path so `localStorage` values of `[]` no longer create an empty default tab over a shared planner route.
  - Removed the shell/runtime-configurable `internalPlannerCalculateUrl` surface and kept the guarded path on a fixed same-origin internal route.
  - Narrowed `/_internal/planner/calculate` success payloads to planner-facing `details` and `visualization`, with opt-in debug only.
  - Genericized internal planner validation/JSON bad-request errors so the route no longer reflects raw validation text.
  - Added and updated xUnit coverage for the narrowed internal response shape, generic internal validation failures, and the removed shell-configurable internal route URL surface.
  - Landed the two non-blocking post-review cleanup follow-ups: the guarded client callback result type now matches the narrowed guarded result contract, and an unused internal response DTO was removed.
- What remains incomplete:
  - Browser/frontend regression coverage for guarded share activation, visualization layout, and hardened internal-route behavior is still deferred to slice 8b.
  - No default-on decision has been made.
  - No Blazor/UI cutover work has started.

## Files / Areas Touched
- `src/Globals.ts`
- `src/Module/Controllers/ProductionController.ts`
- `src/Tools/Production/IProductionPlanResult.ts`
- `src/Tools/Production/PlannerCalculationClient.ts`
- `www/index.php`
- `docker-compose.yml`
- `SolverService/SatisfactoryTools.Solver.Api/Program.cs`
- `SolverService/SatisfactoryTools.Solver.Api/Services/InternalPlannerCalculationResponse.cs`
- `SolverService/SatisfactoryTools.Solver.Api/Services/SpaShellRenderer.cs`
- `SolverService/SatisfactoryTools.Solver.Api.Tests/TestApplicationFactoryExtensions.cs`
- `SolverService/SatisfactoryTools.Solver.Api.Tests/HostRoutingIntegrationTests.cs`
- `SolverService/SatisfactoryTools.Solver.Api.Tests/InternalPlannerCalculationServiceTests.cs`
- `SolverService/SatisfactoryTools.Solver.Api.Tests/SolverApiTests.cs`
- `.sisyphus/plans/dotnet-migration-plan.md`
- `.sisyphus/context/dotnet-migration-resume-guide.md`
- `.sisyphus/context/dotnet-migration-route-parity-matrix.md`
- `.sisyphus/context/dotnet-migration-decision-log.md`
- `.sisyphus/context/dotnet-migration-planner-fixtures.md`
- `.sisyphus/context/dotnet-migration-handoff-2026-04-15-m3-slice8a-boundary-hardening.md`
- `CHANGELOG.md`

## Validation
- Commands run:
  - `dotnet test "SolverService/SatisfactoryTools.Solver.Api.Tests/SatisfactoryTools.Solver.Api.Tests.csproj" --filter "FullyQualifiedName~HostRoutingIntegrationTests|FullyQualifiedName~SolverApiTests|FullyQualifiedName~InternalPlannerCalculationServiceTests" --artifacts-path /tmp/satisfactorytools-m3-slice8a-targeted-artifacts-3 --logger "console;verbosity=minimal"`
  - `dotnet test "SolverService/SatisfactoryTools.Solver.Api.Tests/SatisfactoryTools.Solver.Api.Tests.csproj" --artifacts-path /tmp/satisfactorytools-m3-slice8a-full-artifacts --logger "console;verbosity=minimal"`
  - `dotnet test "SolverService/SatisfactoryTools.Solver.Api.Tests/SatisfactoryTools.Solver.Api.Tests.csproj" --filter "FullyQualifiedName~HostRoutingIntegrationTests|FullyQualifiedName~SolverApiTests|FullyQualifiedName~InternalPlannerCalculationServiceTests" --artifacts-path /tmp/satisfactorytools-m3-slice8a-postreview-artifacts --logger "console;verbosity=minimal"`
  - `yarn build`
  - Live guarded browser/API QA on `http://127.0.0.1:8094` and `http://127.0.0.1:8095`
- Results:
  - targeted slice-8a tests: passed `64/64`
  - full `dotnet test`: passed `148/148`
  - post-review focused tests: passed `64/64`
  - `yarn build`: passed (existing Sass/Bootstrap deprecation warnings remain)
  - live QA verified: no `internalPlannerCalculateUrl` in the shell, guarded share load still uses `/v2/share/*`, guarded solve still uses `/_internal/planner/calculate`, `localStorage.production12 = []` plus `?share=` lands on a single shared tab, visualization node positions remain distinct, no-result debug still works without `graph`, and invalid guarded planner requests return `Unable to calculate planner result.`

## Review Status
- Goal review: PASS.
- Code review: PASS with two non-blocking cleanup findings, both landed in the working tree before final documentation/commit.
- Security review: PASS with one low-severity note that `/_internal/planner/calculate` is still a callable endpoint even though its browser/config exposure is reduced.
- Context review: PASS; the only follow-up was to record the 8a/8b split explicitly in checked-in docs.
- QA sub-agents repeatedly failed with runner-side errors, but the same six QA checks were executed manually in this session and are reflected in the validation section above.

## Last Green State
- Commit SHA: `566dad1`
- Why this is green:
  - This is the last committed branch state before the slice-8a working-tree changes; slice 8a validation is green in the current working tree with `/v2/*` compatibility still unchanged.

## Open Issues / Blockers
- Browser/frontend regression coverage is still required in 8b before any default-on decision.
- `/_internal/planner/calculate` remains a callable same-origin endpoint; 8a reduced its browser/config and response surface, but 8b/default-on review should revisit whether further access restriction is required.
- The legacy Angular solver path still exists by design and must remain until the guarded path plus browser/frontend coverage are accepted.

## Decisions Updated
- M3 slice 8 now proceeds as 8a boundary hardening followed by 8b browser/frontend regression coverage before any default-on decision.

## Exact Next Slice
- M3 slice 8b - add browser/frontend regression coverage for guarded share activation, visualization layout stability, and hardened internal planner route behavior before any default-on decision, without changing `/v2/*` contracts or starting Blazor UI cutover.

## Resume Notes For The Next Session
1. Read `.sisyphus/plans/dotnet-migration-plan.md`
2. Read `.sisyphus/context/dotnet-migration-resume-guide.md`
3. Read `.sisyphus/context/dotnet-migration-planner-fixtures.md`
4. Read `.sisyphus/context/dotnet-migration-route-parity-matrix.md`
5. Read `.sisyphus/context/dotnet-migration-decision-log.md`
6. Read `.sisyphus/context/dotnet-migration-handoff-2026-04-15-m3-slice8a-boundary-hardening.md`
7. Keep the guarded planner path default-off and land browser/frontend regression coverage before revisiting any default-on decision.
