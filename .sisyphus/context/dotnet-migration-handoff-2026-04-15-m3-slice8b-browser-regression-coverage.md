# .NET Migration Session Handoff

## Session
- Date: 2026-04-15
- Milestone: M3 - Planner Domain Port Complete
- Atomic slice: M3 slice 8b - guarded browser/frontend regression coverage

## Summary
- What was completed:
  - Added committed real-browser regression coverage for the guarded planner path in `PlannerBrowserRegressionTests.cs`.
  - Added a browser host fixture that starts the built ASP.NET solver host under test, injects guarded runtime config, and creates isolated share-store state for browser regressions.
  - Locked the guarded F004 share bootstrap path in a browser: shared routes with `localStorage` equal to `[]` now verify a single shared tab, query cleanup, and guarded internal-route usage without falling back to `/v2/solver`.
  - Locked guarded visualization-backed rendering in a browser by asserting the visualization component consumes guarded `visualization` payloads without legacy `graph` data and ends up with distinct rendered node positions.
  - Locked the guarded F007 no-result path in a browser by asserting debug output can be surfaced through the guarded internal route without falling back to `/v2/solver`.
  - Updated continuity docs and fixture notes so M3 slice 8b is now recorded as complete and the next slice is an explicit post-8b guarded default-on/access review.
- What remains incomplete:
  - The guarded planner path is still default-off.
  - No decision has been made yet on whether `/_internal/planner/calculate` needs further restriction before guarded-path widening.
  - No Blazor/UI cutover work has started.

## Files / Areas Touched
- `SolverService/SatisfactoryTools.Solver.Api.Tests/SatisfactoryTools.Solver.Api.Tests.csproj`
- `SolverService/SatisfactoryTools.Solver.Api.Tests/BrowserRegressionWebApplicationFactory.cs`
- `SolverService/SatisfactoryTools.Solver.Api.Tests/PlannerBrowserRegressionTests.cs`
- `README.md`
- `.sisyphus/plans/dotnet-migration-plan.md`
- `.sisyphus/context/dotnet-migration-resume-guide.md`
- `.sisyphus/context/dotnet-migration-decision-log.md`
- `.sisyphus/context/dotnet-migration-route-parity-matrix.md`
- `.sisyphus/context/dotnet-migration-planner-fixtures.md`
- `.sisyphus/context/dotnet-migration-handoff-2026-04-15-m3-slice8b-browser-regression-coverage.md`
- `CHANGELOG.md`

## Validation
- Commands run:
  - `dotnet build "SolverService/SatisfactoryTools.Solver.Api.Tests/SatisfactoryTools.Solver.Api.Tests.csproj" --artifacts-path /tmp/satisfactorytools-m3-slice8b-build-artifacts`
  - `dotnet exec --runtimeconfig "/tmp/satisfactorytools-m3-slice8b-build-artifacts/bin/SatisfactoryTools.Solver.Api.Tests/debug/SatisfactoryTools.Solver.Api.Tests.runtimeconfig.json" --depsfile "/tmp/satisfactorytools-m3-slice8b-build-artifacts/bin/SatisfactoryTools.Solver.Api.Tests/debug/SatisfactoryTools.Solver.Api.Tests.deps.json" "/tmp/satisfactorytools-m3-slice8b-build-artifacts/bin/SatisfactoryTools.Solver.Api.Tests/debug/Microsoft.Playwright.dll" install chromium`
  - `dotnet test "SolverService/SatisfactoryTools.Solver.Api.Tests/SatisfactoryTools.Solver.Api.Tests.csproj" --filter "FullyQualifiedName~PlannerBrowserRegressionTests" --artifacts-path /tmp/satisfactorytools-m3-slice8b-browser-test-artifacts --logger "console;verbosity=minimal"`
  - `dotnet test "SolverService/SatisfactoryTools.Solver.Api.Tests/SatisfactoryTools.Solver.Api.Tests.csproj" --artifacts-path /tmp/satisfactorytools-m3-slice8b-full-test-artifacts --logger "console;verbosity=minimal"`
  - `yarn build`
- Results:
  - browser regression suite: passed `3/3`
  - full `dotnet test`: passed `151/151`
  - changed C# files: diagnostics clean
  - `yarn build`: passed with existing Sass deprecation warnings only

## Review Status
- Implementation verification: PASS for the slice scope.
- Browser regression gate: PASS.
- Full .NET suite: PASS.
- Frontend webpack rebuild: PASS (`yarn build` completed locally with Sass deprecation warnings only).

## Last Green State
- Commit SHA: `7ebb38c`
- Why this is green:
  - This is the last committed state before the 8b browser-regression and documentation updates; all new .NET/browser tests are green in the working tree and `/v2/*` compatibility remains unchanged.

## Open Issues / Blockers
- The next slice still needs an explicit decision on guarded default-on readiness versus keeping the guarded path default-off.
- `/_internal/planner/calculate` is still callable same-origin; 8b added committed browser coverage, but the post-8b review should decide whether further access restriction is required before widening guarded-path usage.
- CircleCI still enforces only the frontend `yarn buildCI` path; repo-facing workflow docs now also describe the local .NET and Playwright browser regression verification path for slice 8b.

## Decisions Updated
- Post-8b guarded-path widening remains a separate explicit gate after browser/frontend coverage is green.

## Exact Next Slice
- M3 slice 9 - review guarded default-on readiness, internal-route access restriction, and rollback expectations before widening guarded-path usage, without changing `/v2/*` contracts or starting Blazor UI cutover.

## Resume Notes For The Next Session
1. Read `.sisyphus/plans/dotnet-migration-plan.md`
2. Read `.sisyphus/context/dotnet-migration-resume-guide.md`
3. Read `.sisyphus/context/dotnet-migration-planner-fixtures.md`
4. Read `.sisyphus/context/dotnet-migration-route-parity-matrix.md`
5. Read `.sisyphus/context/dotnet-migration-decision-log.md`
6. Read `.sisyphus/context/dotnet-migration-handoff-2026-04-15-m3-slice8b-browser-regression-coverage.md`
7. Keep the guarded planner path default-off until the explicit post-8b readiness/access review is completed.
