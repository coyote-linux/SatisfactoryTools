# .NET Migration Session Handoff

## Session
- Date: 2026-04-16
- Milestone: M3 - Planner Domain Port Complete
- Atomic slice: M3 slice 11 - public-origin guarded planner validation and rollback confirmation

## Summary
- What was completed:
  - Confirmed the correct production target is the fork at `https://ficsit.spugnort.com/` on `root@swgn1`, not the original `satisfactorytools.com` site.
  - Preserved the fork's customized PHP shell branding while updating the deployed shell config to expose `useInternalPlannerCalculate` with the slice-10 default-on behavior.
  - Updated the Apache vhost on `swgn1` so the public site now proxies `/v2/*` and `/_internal/planner/*` to the ASP.NET host, forwards sanitized `X-Forwarded-Proto` and `X-Forwarded-Host`, and keeps the live site on the intended same-origin path.
  - Rebuilt the frontend bundle on `swgn1`, published the updated ASP.NET host to `/srv/satisfactorytools/publish/solver`, and restarted `satisfactorytools-solver.service` successfully.
  - Ran the real public-origin default-on smoke on `https://ficsit.spugnort.com/` and observed `POST /_internal/planner/calculate?showDebugOutput=false => 200` from a live share-backed planner load.
  - Temporarily enabled the raw-shell rollback flag with `USE_INTERNAL_PLANNER_CALCULATE=false`, reloaded Apache, and ran the rollback smoke on the same public host, observing `GET /v2/share/<id> => 200` and repeated `POST /v2/solver => 200` with no internal planner route usage.
  - Restored the site to the intended default-on post-slice-11 state after the rollback smoke.
- What remains incomplete:
  - M3 slice 11 itself is complete.
  - The next work should move into M4 Blazor planner beta planning rather than more M3 deployment gating.

## Files / Areas Touched
- Production host: `root@swgn1`
- Deployed source tree: `/srv/satisfactorytools/current`
- Published solver: `/srv/satisfactorytools/publish/solver`
- Apache vhost: `/etc/httpd/conf.d/satisfactorytools.conf`
- Deployed shell template: `/srv/satisfactorytools/current/www/index.php`
- Deployed frontend bundle: `/srv/satisfactorytools/current/www/assets/app.js`
- Local continuity docs updated for completion:
  - `.sisyphus/plans/dotnet-migration-plan.md`
  - `.sisyphus/context/dotnet-migration-resume-guide.md`
  - `.sisyphus/context/dotnet-migration-route-parity-matrix.md`
  - `.sisyphus/context/dotnet-migration-decision-log.md`
  - `.sisyphus/context/dotnet-migration-planner-fixtures.md`
  - `.sisyphus/context/dotnet-migration-handoff-2026-04-16-m3-slice11-public-origin-completion.md`

## Validation
- Deployment change steps executed:
  - backed up `/etc/httpd/conf.d/satisfactorytools.conf`, deployed `www/index.php`, live `app.js`, and the published solver tree under `/root/satisfactorytools-backups/<timestamp>` on `swgn1`
  - synced updated source onto `/srv/satisfactorytools/current` while preserving the fork-specific branded shell
  - rebuilt frontend bundle with `yarn build`
  - published ASP.NET host with `dotnet publish SolverService/SatisfactoryTools.Solver.Api/SatisfactoryTools.Solver.Api.csproj -c Release -o /srv/satisfactorytools/publish/solver.new`
  - validated Apache config with `apachectl configtest`
  - swapped in the new publish directory and restarted `satisfactorytools-solver.service`
  - reloaded Apache successfully
- Public-origin smoke evidence:
  - Environment: production fork
  - Public host: `https://ficsit.spugnort.com/`
  - Share URL used: `https://ficsit.spugnort.com/1.2/production?share=emCiLJ0yN2qnLFs5`
  - Default-on guarded path:
    - observed `POST https://ficsit.spugnort.com/_internal/planner/calculate?showDebugOutput=false => 200`
    - no `POST /v2/solver` observed for that default-on smoke
  - Rollback path:
    - temporarily set raw-shell rollback with `USE_INTERNAL_PLANNER_CALCULATE=false` in Apache vhost
    - observed `GET https://ficsit.spugnort.com/v2/share/emCiLJ0yN2qnLFs5 => 200`
    - observed repeated `POST https://ficsit.spugnort.com/v2/solver => 200`
    - no `/_internal/planner/calculate` requests were observed in the rollback smoke
  - Final state restored:
    - public shell config again advertises `useInternalPlannerCalculate: true`
    - `/v2/` now returns the ASP.NET JSON health payload through the public origin

## Open Issues / Blockers
- No remaining blocker for M3 slice 11.
- Preserve the current Apache proxy/header sanitization when future deployment changes are made on `swgn1`.

## Exact Next Step
- Start M4 slice 1 by scaffolding the first Blazor planner beta route or feature flag without cutting over `/{version}/production`.

## Resume Notes For The Next Session
1. Read `.sisyphus/plans/dotnet-migration-plan.md`
2. Read `.sisyphus/context/dotnet-migration-resume-guide.md`
3. Read `.sisyphus/context/dotnet-migration-route-parity-matrix.md`
4. Read `.sisyphus/context/dotnet-migration-decision-log.md`
5. Read `.sisyphus/context/dotnet-migration-planner-fixtures.md`
6. Read `.sisyphus/context/dotnet-migration-handoff-2026-04-16-m3-slice11-public-origin-completion.md`
7. Start the first M4 beta-route scaffold without disturbing the now-green guarded planner deployment path.
