# .NET Migration Session Handoff

## Session
- Date: 2026-04-14
- Milestone: M1 - Unified ASP.NET Core Shell
- Atomic slice: M1 - Unified ASP.NET Core Shell

## Summary
- What was completed:
  - Extended `SolverService/SatisfactoryTools.Solver.Api` so the existing ASP.NET host now serves the Angular shell for `/` and HTML5 deep links, serves the `www/` asset tree, and keeps `/v2/*` endpoints mapped ahead of shell fallback.
  - Preserved runtime shell behavior by using `www/index.php` as the template source of truth, replacing the PHP `SOLVER_URL` and `filemtime(...)` snippets at runtime inside ASP.NET instead of changing Angular code, including the empty-`SOLVER_URL` fallback behavior.
  - Added integration coverage for root shell serving, supported bare version roots, deep-link fallback, runtime config injection parity, static asset serving, and explicit non-fallback behavior for unknown `/v2/*` routes.
  - Replaced the active local PHP/Apache Docker/docs path with the unified ASP.NET host path and removed the now-obsolete Apache proxy config.
- What remains incomplete:
  - ASP.NET is the unified host, but route ownership is still mostly legacy Angular fallback rather than an explicit strangler split.
  - No planner-domain or Blazor route migration work has started yet.

## Files / Areas Touched
- `SolverService/SatisfactoryTools.Solver.Api/Program.cs`
- `SolverService/SatisfactoryTools.Solver.Api/SatisfactoryTools.Solver.Api.csproj`
- `SolverService/SatisfactoryTools.Solver.Api/Services/FrontendRootResolver.cs`
- `SolverService/SatisfactoryTools.Solver.Api/Services/SpaShellRenderer.cs`
- `SolverService/SatisfactoryTools.Solver.Api.Tests/SolverApiTests.cs`
- `docker-compose.yml`
- `README.md`
- `docs/architecture.md`
- `CHANGELOG.md`
- `.sisyphus/context/dotnet-migration-route-parity-matrix.md`
- `.sisyphus/context/dotnet-migration-resume-guide.md`
- `.sisyphus/context/dotnet-migration-handoff-2026-04-14-m1-unified-shell.md`
- `docker/apache/local-v2-proxy.conf` (removed)

## Validation
- Commands run:
  - `dotnet test "SolverService/SatisfactoryTools.Solver.Api.Tests/SatisfactoryTools.Solver.Api.Tests.csproj"`
  - `yarn build`
  - `docker compose config`
  - local unified-host compose smoke test against `/`, `/1.2/production`, `/assets/app.js`, `/v2/`, `POST /v2/share/?version=1.2`, `GET /v2/share/{shareId}`, and `/assets/missing.js`
- Results:
  - `dotnet test`: passed `52/52`
  - `yarn build`: passed (existing Sass deprecation warnings remain)
  - `docker compose config`: passed
  - unified-host compose smoke test: passed for shell load, bare version-root load, deep-link fallback, static asset serving, health, share create/load, and missing-asset `404`

## Last Green State
- Commit SHA: `cf66273`
- Why this is green:
  - This is the last committed M0-complete branch state before the M1 working-tree changes.

## Open Issues / Blockers
- None recorded during M1 implementation.

## Decisions Updated
- Effectively resolves open decision `O001` in practice by extending the existing ASP.NET host project rather than creating a second public web app.

## Exact Next Slice
- M2 - Route-Level Strangler Scaffold

## Resume Notes For The Next Session
1. Read `.sisyphus/plans/dotnet-migration-plan.md`
2. Read `.sisyphus/context/dotnet-migration-resume-guide.md`
3. Read `.sisyphus/context/dotnet-migration-decision-log.md`
4. Read `.sisyphus/context/dotnet-migration-route-parity-matrix.md`
5. Re-run the M1 validation commands before starting further edits if the working tree changed again after this handoff note
6. Start M2 only after keeping the new shell fallback behavior and `/v2/*` non-regression tests green
