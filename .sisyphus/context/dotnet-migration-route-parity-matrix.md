# .NET Migration Route Parity Matrix

## Purpose

Track route-by-route migration from AngularJS ownership to ASP.NET Core ownership.

## Route Groups

| Route / State Group | Current Owner | Key Files | Target Owner | Planned Milestone | Compatibility Requirements | Status |
|---|---|---|---|---|---|---|
| Shell bootstrap | PHP + AngularJS | `www/index.php`, `src/app.ts` | ASP.NET Core host | M1 | preserve app shell behavior, `solverUrl` injection, asset loading, base href assumptions | Planned |
| Deep-link fallback | Apache `.htaccess` | `www/.htaccess` | ASP.NET Core middleware/fallback routing | M1 | preserve current HTML5-mode deep links | Planned |
| Version route wrapper | AngularJS ui-router | `src/Module/AppModule.ts`, `src/Data/DataProvider.ts` | ASP.NET Core + migrated UI route layer | M2-M4 | preserve supported versions and normalization | Planned |
| Home | AngularJS | `AppModule.ts`, `HomeController.ts`, `templates/Controllers/home.html` | ASP.NET Core UI | M6 | preserve path, visible behavior, version context | Planned |
| Items list/detail | AngularJS | `ItemController.ts`, item templates, filters | ASP.NET Core UI | M6 | preserve item routes and detail semantics | Planned |
| Buildings list/detail | AngularJS | `BuildingController.ts`, building templates/components | ASP.NET Core UI | M6 | preserve building routes and dynamic detail behavior | Planned |
| Schematics list/detail | AngularJS | `SchematicController.ts`, schematic templates/components | ASP.NET Core UI | M6 | preserve route shape and graph/detail flows | Planned |
| Production planner | AngularJS | `ProductionController.ts`, `ProductionTab.ts`, `templates/Controllers/production.html` | Blazor | M4-M5 | preserve `/{version}/production`, share links, version behavior, planner workflows | Planned |
| Solver API | ASP.NET Core | `SolverService/.../Program.cs` | ASP.NET Core | M0-M8 | preserve `/v2/solver` contract | Preserved |
| Share API | ASP.NET Core | `SolverService/.../Program.cs`, `ShareStore.cs` | ASP.NET Core | M0-M8 | preserve `/v2/share` contract and share-link format | Preserved |

## Notes

1. This matrix should be updated whenever route ownership changes.
2. No route should be marked complete until its parity gate is green.
