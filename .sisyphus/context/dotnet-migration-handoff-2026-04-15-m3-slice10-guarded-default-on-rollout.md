# .NET Migration Session Handoff

## Session
- Date: 2026-04-15
- Milestone: M3 - Planner Domain Port Complete
- Atomic slice: M3 slice 10 - guarded internal planner default-on rollout with explicit rollback

## Summary
- What was completed:
  - Flipped the unified shell default so guarded planner calculation now prefers same-origin `POST /_internal/planner/calculate` without changing `/v2/*` contracts or Angular route ownership.
  - Kept explicit rollback on both shell-rendering paths: `Planner:UseInternalCalculate=false` for the ASP.NET host and `USE_INTERNAL_PLANNER_CALCULATE=false` for direct raw `www/index.php` rendering.
  - Aligned `docker compose up` with the new default-on guarded planner behavior while preserving explicit compose-local rollback through `PLANNER_USE_INTERNAL_CALCULATE=false`.
  - Hardened the default-path regression harnesses against inherited runner environment so host and browser tests now prove the real default-on behavior even when ambient planner config is set to rollback values.
  - Updated host integration tests to lock the new default-on shell config plus explicit-false rollback injection.
  - Updated the browser regression harness so the default fixture no longer forces guarded mode on, then added an explicit-false browser regression proving rollback to `POST /v2/solver`.
  - Updated continuity docs, architecture docs, README, and changelog to record slice 10 as complete and to name the next slice explicitly.
- What remains incomplete:
  - Public-host deployment topology validation is still pending for the default-on guarded path.
  - No forwarded-header or reverse-proxy changes have been made in this slice.
  - No Blazor/UI cutover work has started.

## Files / Areas Touched
- `SolverService/SatisfactoryTools.Solver.Api/Services/SpaShellRenderer.cs`
- `www/index.php`
- `SolverService/SatisfactoryTools.Solver.Api.Tests/HostRoutingIntegrationTests.cs`
- `SolverService/SatisfactoryTools.Solver.Api.Tests/TestApplicationFactoryExtensions.cs`
- `SolverService/SatisfactoryTools.Solver.Api.Tests/BrowserRegressionWebApplicationFactory.cs`
- `SolverService/SatisfactoryTools.Solver.Api.Tests/PlannerBrowserRegressionTests.cs`
- `README.md`
- `docker-compose.yml`
- `docs/architecture.md`
- `CHANGELOG.md`
- `.sisyphus/plans/dotnet-migration-plan.md`
- `.sisyphus/context/dotnet-migration-resume-guide.md`
- `.sisyphus/context/dotnet-migration-route-parity-matrix.md`
- `.sisyphus/context/dotnet-migration-decision-log.md`
- `.sisyphus/context/dotnet-migration-planner-fixtures.md`
- `.sisyphus/context/dotnet-migration-handoff-2026-04-15-m3-slice10-guarded-default-on-rollout.md`

## Validation
- Commands run:
  - `yarn build`
  - `dotnet test "SolverService/SatisfactoryTools.Solver.Api.Tests/SatisfactoryTools.Solver.Api.Tests.csproj" --filter "FullyQualifiedName~HostRoutingIntegrationTests" --artifacts-path /tmp/satisfactorytools-slice10-host-routing-artifacts --logger "console;verbosity=minimal"`
  - `dotnet test "SolverService/SatisfactoryTools.Solver.Api.Tests/SatisfactoryTools.Solver.Api.Tests.csproj" --filter "FullyQualifiedName~InternalPlannerAccessPolicyTests|FullyQualifiedName~InternalPlannerCalculateRoute" --artifacts-path /tmp/satisfactorytools-slice10-internal-route-artifacts --logger "console;verbosity=minimal"`
  - `dotnet build "SolverService/SatisfactoryTools.Solver.Api.Tests/SatisfactoryTools.Solver.Api.Tests.csproj" --artifacts-path /tmp/satisfactorytools-slice10-browser-artifacts`
  - `dotnet test "SolverService/SatisfactoryTools.Solver.Api.Tests/SatisfactoryTools.Solver.Api.Tests.csproj" --filter "FullyQualifiedName~PlannerBrowserRegressionTests" --artifacts-path /tmp/satisfactorytools-slice10-browser-artifacts --logger "console;verbosity=minimal"`
  - `Planner__UseInternalCalculate=false SOLVER_URL=https://ambient.example.invalid/v2/solver dotnet test "SolverService/SatisfactoryTools.Solver.Api.Tests/SatisfactoryTools.Solver.Api.Tests.csproj" --filter "FullyQualifiedName~HostRoutingIntegrationTests" --artifacts-path /tmp/satisfactorytools-slice10-host-hermetic-artifacts --logger "console;verbosity=minimal"`
  - `Planner__UseInternalCalculate=false SOLVER_URL=https://ambient.example.invalid/v2/solver dotnet test "SolverService/SatisfactoryTools.Solver.Api.Tests/SatisfactoryTools.Solver.Api.Tests.csproj" --filter "FullyQualifiedName~PlannerBrowserRegressionTests" --artifacts-path /tmp/satisfactorytools-slice10-browser-hermetic-artifacts --logger "console;verbosity=minimal"`
  - `dotnet test "SolverService/SatisfactoryTools.Solver.Api.Tests/SatisfactoryTools.Solver.Api.Tests.csproj" --artifacts-path /tmp/satisfactorytools-slice10-full-artifacts --logger "console;verbosity=minimal"`
  - `dotnet test "SolverService/SatisfactoryTools.Solver.Api.Tests/SatisfactoryTools.Solver.Api.Tests.csproj" --artifacts-path /tmp/satisfactorytools-slice10-final-full-artifacts --logger "console;verbosity=minimal"`
- Results:
  - `yarn build`: passed with existing Sass deprecation warnings only
  - `HostRoutingIntegrationTests`: passed `13/13`
  - `InternalPlannerAccessPolicyTests` + targeted internal planner route tests: passed `11/11`
  - browser regression suite: passed `4/4`
  - targeted hostile-env regression checks: host suite passed `13/13`, browser suite passed `4/4`
  - full `dotnet test`: passed `159/159` before and after the final test-helper/compose alignment changes

## Review Status
- Implementation verification: PASS for the slice scope.
- Shell default-on rollout with explicit rollback: PASS.
- Browser regression gate: PASS.
- Full .NET suite: PASS.
- Frontend webpack rebuild: PASS (`yarn build` completed locally with Sass deprecation warnings only).

## Last Green State
- Commit SHA: `abfa537`
- Why this is green:
  - This is the last committed state before slice 10 default-on rollout and continuity updates; slice 9 same-origin access hardening was already green and `/v2/*` compatibility remained unchanged.

## Open Issues / Blockers
- The next slice still needs deployed public-origin smoke for the default-on guarded path.
- If the public site sits behind a reverse proxy or TLS terminator that changes `request.Scheme` or `request.Host`, the same-origin gate may reject legitimate planner requests until forwarded/public authority handling is confirmed.
- CircleCI still enforces only the frontend `yarn buildCI` path; the .NET and Playwright suites remain repo-local verification paths.

## Decisions Updated
- D015 - M3 slice 10 enables guarded planner calculation by default while keeping explicit rollback.

## Exact Next Slice
- M3 slice 11 - validate the default-on guarded planner path under deployed public-host topology and confirm rollback smoke without widening `/v2/*` or starting Blazor UI cutover.

## Resume Notes For The Next Session
1. Read `.sisyphus/plans/dotnet-migration-plan.md`
2. Read `.sisyphus/context/dotnet-migration-resume-guide.md`
3. Read `.sisyphus/context/dotnet-migration-route-parity-matrix.md`
4. Read `.sisyphus/context/dotnet-migration-decision-log.md`
5. Read `.sisyphus/context/dotnet-migration-planner-fixtures.md`
6. Read `.sisyphus/context/dotnet-migration-handoff-2026-04-15-m3-slice10-guarded-default-on-rollout.md`
7. Validate the default-on guarded planner path against the real deployed public origin before assuming reverse-proxy topology is safe.
