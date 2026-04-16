# .NET Migration Route Parity Matrix

## Purpose

Track route-by-route migration from AngularJS ownership to ASP.NET Core ownership.

## Route Groups

| Route / State Group | Current Owner | Key Files | Target Owner | Planned Milestone | Compatibility Requirements | Status |
|---|---|---|---|---|---|---|
| Shell bootstrap | ASP.NET Core + AngularJS | `www/index.php`, `src/app.ts`, `SolverService/.../Program.cs` | ASP.NET Core host | M1 | preserve app shell behavior, `solverUrl` injection, asset loading, base href assumptions | Preserved |
| Host route-ownership policy | ASP.NET Core host | `SolverService/.../Program.cs`, `SolverService/.../Services/HostRouteOwnershipPolicy.cs` | ASP.NET Core host | M2-M3 | keep `/v2/*` and host-internal planner runtime routes API-owned, keep static files ahead of shell fallback, keep legacy Angular routes on fallback until later cutover | Preserved |
| Deep-link fallback | ASP.NET Core fallback routing | `www/.htaccess`, `SolverService/.../Program.cs`, `SolverService/.../Services/HostRouteOwnershipPolicy.cs` | ASP.NET Core middleware/fallback routing | M1-M3 | preserve current HTML5-mode deep links while keeping `/v2/*` and host-internal planner runtime routes off the shell fallback | Preserved |
| Version route wrapper | AngularJS ui-router | `src/Module/AppModule.ts`, `src/Data/DataProvider.ts` | ASP.NET Core + migrated UI route layer | M2-M4 | preserve supported versions and normalization | Planned |
| Home | AngularJS | `AppModule.ts`, `HomeController.ts`, `templates/Controllers/home.html` | ASP.NET Core UI | M6 | preserve path, visible behavior, version context | Planned |
| Items list/detail | AngularJS | `ItemController.ts`, item templates, filters | ASP.NET Core UI | M6 | preserve item routes and detail semantics | Planned |
| Buildings list/detail | AngularJS | `BuildingController.ts`, building templates/components | ASP.NET Core UI | M6 | preserve building routes and dynamic detail behavior | Planned |
| Schematics list/detail | AngularJS | `SchematicController.ts`, schematic templates/components | ASP.NET Core UI | M6 | preserve route shape and graph/detail flows | Planned |
| Production planner | AngularJS | `ProductionController.ts`, `ProductionTab.ts`, `templates/Controllers/production.html` | Blazor | M4-M5 | preserve `/{version}/production`, share links, version behavior, planner workflows | Planned |
| Solver API | ASP.NET Core | `SolverService/.../Program.cs` | ASP.NET Core | M0-M8 | preserve `/v2/solver` contract | Preserved |
| Share API | ASP.NET Core | `SolverService/.../Program.cs`, `ShareStore.cs` | ASP.NET Core | M0-M8 | preserve `/v2/share` contract and share-link format | Preserved |
| Internal planner runtime route | ASP.NET Core | `SolverService/.../Program.cs`, `SolverService/.../Services/InternalPlannerCalculationService.cs`, `SolverService/.../Services/HostRouteOwnershipPolicy.cs` | ASP.NET Core | M3 | keep `/_internal/planner/*` API-owned, behind the same-origin request gate, and separate from `/v2/*` compatibility promises | Preserved |

## Current Angular State Inventory

| State | URL | Parent | Abstract | Notes |
|---|---|---|---|---|
| `root` | `` | none | yes | shell root using `root.html` |
| `page_content` | `` | `root` | yes | top-level page wrapper and breadcrumb container |
| `version` | `/{version}?share={shareId}` | `page_content` | yes | version wrapper; onEnter sets breadcrumb label from normalized version |
| `listing` | `` | `version` | yes | shared listing/content wrapper |
| `home` | `/` | `listing` | no | homepage |
| `codex` | `/codex` | `listing` | yes | codex wrapper |
| `schematics` | `/schematics` | `codex` | no | schematic listing |
| `schematic` | `/{item}` | `schematics` | no | schematic detail |
| `buildings` | `/buildings` | `codex` | no | building listing |
| `building` | `/{item}` | `buildings` | no | building detail |
| `items` | `/items` | `codex` | no | item listing |
| `item` | `/{item}` | `items` | no | item detail |
| `production` | `/production` | `listing` | no | production planner |

## Version Facts To Preserve

1. Supported versions are `1.1`, `1.1-ficsmas`, and `1.2`.
2. Default version is `1.1`.
3. Version normalization rules are:
   - `1.0 -> 1.1`
   - `1.0-ficsmas -> 1.1-ficsmas`
   - supported versions pass through unchanged
   - unsupported values normalize to `1.1`
4. `1.1-ficsmas` displays as `1.1 (Ficsmas)` in the UI label.
5. Startup and route transitions both re-normalize the active version and re-run `DataProvider.change(version)`.
6. `DataProvider.change(version)` maps:
   - `1.1-ficsmas` and `1.0-ficsmas` to `data1.0-ficsmas.json`
   - everything else to `data1.0.json`

## Migration Notes

1. The route/state inventory above comes directly from `src/Module/AppModule.ts` and is the source of truth for route parity planning.
2. The `version` state advertises query param `shareId`, while the current share loader in `ProductionController` reads `share` from `$location.search()`. Preserve current observable behavior first; cleanup can be a later deliberate change.
3. M2 extracted the host-side ownership and fallback decision into `SolverService/SatisfactoryTools.Solver.Api/Services/HostRouteOwnershipPolicy.cs` so the boundary is explicit and testable without changing any Angular route behavior.
4. In M2, ASP.NET Core explicitly owned `/v2/*`, `/index.php`, static file handling, and the legacy-shell fallback policy; all current Angular UI routes from `AppModule.ts` remained legacy-shell-owned.
5. In M3 slice 6, ASP.NET Core also gained an internal-only planner runtime route under `/_internal/planner/*`; this is API-owned for host routing, but it is not part of the public `/v2/*` compatibility surface.
6. The host fallback allow-list for dotted bare version roots remains `/1.0`, `/1.0-ficsmas`, `/1.1`, `/1.1-ficsmas`, and `/1.2` even though Angular runtime-supported versions are narrower. Do not collapse that list until a later explicit route migration decision.
7. In M3 slice 7, the legacy Angular production planner gained a default-off guarded caller that can post full planner state to `/_internal/planner/calculate` through host-injected shell config, but `/{version}/production` still remains Angular-owned and `/v2/*` route ownership is unchanged.
8. In M3 slice 8a, the guarded planner path stopped advertising `internalPlannerCalculateUrl` in shell/runtime config, and the internal route response narrowed to planner-facing `details`/`visualization` with opt-in debug only; route ownership remained unchanged and `/v2/*` compatibility promises stayed separate.
9. In M3 slice 8b, `PlannerBrowserRegressionTests.cs` added committed real-browser coverage for guarded F004-style share activation, visualization-backed guarded rendering/layout, and F007 no-result debug/internal-route behavior while `/{version}/production` remained Angular-owned, guarded mode stayed default-off, and `/v2/*` ownership stayed unchanged.
10. In M3 slice 9, `/_internal/planner/calculate` stayed API-owned but gained an explicit same-origin access gate, direct solver tests now lock allowed and rejected origin behavior, and guarded mode still remains default-off while `/{version}/production` and `/v2/*` ownership stay unchanged.
11. In M3 slice 10, the unified shell flipped guarded planner calculation to default-on, browser regressions now exercise that default without test-only opt-in config, and explicit false rollback still sends planner solves through `/v2/solver` while `/{version}/production` and `/v2/*` ownership stay unchanged.
12. In M3 slice 11, the host began consuming forwarded public scheme/host before `/_internal/planner/calculate` authorization, the deployed fork at `https://ficsit.spugnort.com/` now proxies `/v2/*` and `/_internal/planner/*` to the ASP.NET host, real public-origin smoke proved default-on guarded solves hit `POST /_internal/planner/calculate`, and real rollback smoke proved the raw shell can still send planner solves back through `POST /v2/solver` while `/{version}/production` and `/v2/*` ownership stay unchanged.
13. In M4 slice 1, the host reserved `/beta/*` away from legacy shell fallback, added a new off-by-default `GET /beta/production` Blazor placeholder seam behind `Planner:BetaRouteEnabled`, and kept `/{version}/production`, `/v2/*`, and `/_internal/planner/*` ownership unchanged.

## Notes

1. This matrix should be updated whenever route ownership changes.
2. No route should be marked complete until its parity gate is green.
3. The M3 parity gate is green through slice 11, and M4 slice 1 has established the first beta route seam. The next route-parity work should expand `/beta/production` without cutting over `/{version}/production`.
