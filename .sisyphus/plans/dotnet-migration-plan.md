# .NET Migration Plan

## Purpose

This plan defines the phased migration of SatisfactoryTools from its current AngularJS + webpack/Node + PHP-shell architecture to a fully .NET-based implementation that can be executed safely across many work sessions.

It is intentionally optimized for:

1. small, resumable slices of work,
2. stable compatibility during migration,
3. explicit phase gates,
4. future evolution from JSON-backed content to a database-backed model and admin backend.

## Planning Assumptions

1. The current `/v2/solver` and `/v2/share` endpoints remain compatibility anchors through the migration.
2. The end-state should be one public ASP.NET Core web surface.
3. The production planner is the highest-value migration target and should move to Blazor before lower-risk routes.
4. Current route shapes, share links, version semantics, and supported datasets should remain stable until there is an explicit product decision to change them.
5. JSON remains the canonical game-data source until the runtime and route migration is stable.

## Recommended End State

The end-state is a single ASP.NET Core application that owns:

1. the application shell,
2. static assets,
3. deep-link fallback,
4. `/v2/solver`,
5. `/v2/share`,
6. migrated UI routes,
7. eventual database-backed content and administration features.

### UI Direction

1. Use Blazor for the production planner and other highly interactive surfaces.
2. Use Razor Pages, MVC views, or static SSR Razor components for simpler content routes where that is operationally cheaper.

## Primary Migration Strategy

Use an incremental strangler approach.

### Why

1. The repo already has an ASP.NET Core backend.
2. The route/version map is centralized in `src/Module/AppModule.ts`.
3. The planner contains substantial client-side business logic that should be ported and validated incrementally.
4. A big-bang rewrite would combine host replacement, route replacement, planner porting, UI rewrite, and deployment changes into one high-risk cutover.

## Compatibility Requirements

These requirements should be preserved through M5 unless explicitly revised:

1. route shapes, especially `/{version}/production`,
2. share URL format,
3. `/v2/solver` contract,
4. `/v2/share` contract,
5. current supported version semantics,
6. same-origin deployment model,
7. current local storage continuity where feasible.

## Explicit Non-Goals During Early Phases

1. Do not redesign the solver algorithm.
2. Do not redesign the planner UX while migrating frameworks.
3. Do not move game-data runtime reads to a database before the runtime/web migration is stable.
4. Do not port the offline Node tooling before the web/runtime migration is stable.

## Workstreams

| Workstream | Scope | Starts | Ends |
|---|---|---:|---:|
| A. Host Unification | Replace PHP shell and Apache-specific fallback with ASP.NET Core hosting | M0 | M2 |
| B. Contract and Regression Harness | Lock current behavior with tests, fixtures, and parity documentation | M0 | M8 |
| C. Planner Domain Port | Move planner-side TypeScript business logic into shared C# domain code | M2 | M4 |
| D. Blazor Planner UI | Build and cut over the production planner route | M3 | M5 |
| E. Remaining Route Migration | Move browse/detail/content routes off AngularJS | M5 | M6 |
| F. Data Platform and Admin | JSON-to-DB evolution, admin backend, publish pipeline | M4 planning, M6 execution | M8 |
| G. Documentation and Handoff | Maintain resumable internal docs, gates, runbooks, and decision history | M0 | M8 |

## Milestones

### M0. Migration Baseline

#### Goal
Freeze current observable behavior before architecture changes.

#### Outputs
1. contract tests for `/v2/*`,
2. route/version inventory,
3. planner characterization fixtures,
4. parity matrices,
5. internal handoff package.

#### Exit Gate
1. Core contracts are test-locked.
2. Representative planner fixtures exist for `1.1`, `1.1-ficsmas`, and `1.2`.
3. Resume artifacts are in place.

### M1. Unified ASP.NET Core Shell

#### Goal
Replace `www/index.php` and `.htaccess` runtime responsibilities with ASP.NET Core while Angular still renders the app unchanged.

#### Outputs
1. ASP.NET Core shell equivalent,
2. static asset serving,
3. deep-link fallback,
4. runtime solver config injection parity,
5. updated dev/deploy path.

#### Exit Gate
1. Angular app still works unchanged under ASP.NET Core hosting.
2. PHP is no longer required in runtime.
3. `/v2/*` still passes existing tests.

### M2. Route-Level Strangler Scaffold

#### Goal
Make ASP.NET Core the route owner and explicitly split migrated routes from legacy Angular routes.

#### Outputs
1. route ownership map,
2. feature-flag or route-switch strategy,
3. explicit fallback behavior.

#### Exit Gate
1. Route ownership is testable and unambiguous.
2. Deep links still behave correctly.

### M3. Planner Domain Port Complete

#### Goal
Port planner-side business logic to C# under tests before UI cutover.

#### Outputs
1. version normalization logic,
2. request shaping and compatibility logic,
3. result graph parsing,
4. result aggregation,
5. parity suite against locked fixtures.

#### Exit Gate
1. C# planner domain matches current representative outputs.
2. No critical parity gaps remain in request/result shaping.

### M4. Blazor Planner Beta

#### Goal
Build a Blazor implementation of the production planner behind a safe route or feature flag.

#### Outputs
1. Blazor planner route,
2. version handling,
3. share flows,
4. local persistence compatibility,
5. graph rendering integration.

#### Exit Gate
1. Planner smoke tests pass.
2. Known gaps are explicit and acceptable.

### M5. Production Planner Cutover

#### Goal
Make Blazor the main owner of `/{version}/production`.

#### Outputs
1. route cutover,
2. preserved share URL behavior,
3. rollback path,
4. parity report for cutover.

#### Exit Gate
1. Planner route is stable under real traffic.
2. Old share links still load.
3. Storage compatibility is acceptable.

### M6. Remaining Route Migration

#### Goal
Migrate home, codex, detail, and browse routes off AngularJS.

#### Outputs
1. migrated read-only and browse routes,
2. route parity completion,
3. Angular runtime removal readiness.

#### Exit Gate
1. Angular-only runtime path is no longer needed.
2. The site can be served fully by ASP.NET Core without Angular fallback.

### M7. Data Platform Foundation

#### Goal
Introduce the database and admin backend safely after the runtime migration is stable.

#### Outputs
1. DB schema,
2. JSON import pipeline,
3. admin auth shell,
4. admin publish workflow,
5. JSON-vs-DB parity validation.

#### Exit Gate
1. DB model is proven against current JSON data.
2. Admin publish flow is safe in staging.

### M8. Database Cutover and Admin Expansion

#### Goal
Switch runtime content reads from JSON to the database and expand administrative capabilities.

#### Outputs
1. DB-backed runtime reads,
2. publish/version management,
3. extended admin backend,
4. optional DB-backed share persistence.

#### Exit Gate
1. DB-backed runtime passes parity suite.
2. Rollback procedure is documented and tested.

## JSON to Database Strategy

### Recommendation
Keep JSON canonical through the runtime migration and only introduce database-backed game data after the host and planner migration are stable.

### Sequencing

1. **M0-M5**: JSON remains canonical for runtime reads.
2. **M4-M7**: Design DB schema and build import/publish tooling in shadow mode.
3. **M7**: Introduce admin backend and import/validate/publish flow.
4. **M8**: Move runtime reads to DB only after JSON-vs-DB parity is proven.

### Why

1. Current version switching is tightly coupled to JSON-backed behavior.
2. The web/runtime migration is already a large enough source of change.
3. Keeping JSON canonical early makes failures easier to attribute.

## Administration Backend Strategy

### First Admin Release Scope

1. Authentication and roles.
2. Import JSON dataset.
3. Validate import.
4. Show diff against currently published data.
5. Publish/unpublish a dataset version.
6. Audit trail for admin actions.

### Defer Initially

1. Rich per-record manual editing.
2. Complex editorial workflows.
3. Scope that is not required to safely publish versioned site data.

## Acceptance Gates Between Phases

### G0 -> G1
1. Contract tests exist.
2. Route/version behavior is documented.
3. Planner fixtures exist.

### G1 -> G2
1. ASP.NET Core shell serves the current app with parity.
2. Deep links and static assets work.
3. `/v2/*` behavior remains stable.

### G2 -> G3
1. Route ownership is explicit.
2. Planner beta path is defined.

### G3 -> G4
1. C# planner domain passes parity fixtures.
2. Request/result shaping is stable.

### G4 -> G5
1. Blazor planner beta passes workflow smoke tests.
2. Local storage/share behavior is acceptable.

### G5 -> G6
1. Production planner cutover is stable.
2. Rollback is documented.

### G6 -> G7
1. Angular runtime dependency is nearly or fully removable.
2. Data/admin scope is approved and isolated.

### G7 -> G8
1. JSON-vs-DB parity is proven.
2. Admin publish flow is staging-safe.
3. Rollback exists for DB cutover.

## Required Internal Documentation

This plan depends on the following internal docs being maintained:

1. `.sisyphus/plans/dotnet-migration-plan.md`
2. `.sisyphus/context/dotnet-migration-resume-guide.md`
3. `.sisyphus/context/dotnet-migration-decision-log.md`
4. `.sisyphus/context/dotnet-migration-route-parity-matrix.md`
5. `.sisyphus/context/dotnet-migration-handoff-template.md`
6. `.sisyphus/context/dotnet-migration-risk-register.md`
7. `.sisyphus/context/dotnet-migration-validation-gates.md`
8. `.sisyphus/context/dotnet-migration-planner-fixtures.md`

## Suggested First Execution Slice

### Slice Name
M0 Slice 1 - Migration operating baseline.

### Goal
Create the safety and continuity layer needed for the whole migration.

### Deliverables
1. Internal planning package.
2. Contract tests for `/v2/solver` and `/v2/share`.
3. Route/version inventory from `AppModule.ts`.
4. Initial planner fixture set.

### Acceptance
1. Baseline tests are green.
2. Resume documents are in place.
3. The next slice is obvious: ASP.NET Core shell takeover.

### Executable QA Scenario
1. Run `dotnet test "SolverService/SatisfactoryTools.Solver.Api.Tests/SatisfactoryTools.Solver.Api.Tests.csproj"`.
2. Run `yarn build`.
3. Verify the following files exist and are updated:
   - `.sisyphus/plans/dotnet-migration-plan.md`
   - `.sisyphus/context/dotnet-migration-resume-guide.md`
   - `.sisyphus/context/dotnet-migration-decision-log.md`
   - `.sisyphus/context/dotnet-migration-route-parity-matrix.md`
   - `.sisyphus/context/dotnet-migration-handoff-template.md`
   - `.sisyphus/context/dotnet-migration-risk-register.md`
   - `.sisyphus/context/dotnet-migration-validation-gates.md`
   - `.sisyphus/context/dotnet-migration-planner-fixtures.md`
4. Expected result:
   - existing solver tests pass,
   - webpack build passes,
   - all migration continuity docs are present,
   - the next slice is recorded as M1 shell takeover.

## Atomic Slice Rules

1. Every session targets one atomic slice only.
2. Every slice ends at a green checkpoint.
3. Every session updates the handoff artifact.
4. No phase transition occurs without a documented gate pass.

## Planned Verification Projects

These are the planned test assets to be created and then used consistently in later phases.

1. `Tests/SatisfactoryTools.Web.Host.Tests/SatisfactoryTools.Web.Host.Tests.csproj`
   - host shell, route fallback, route ownership, and `/v2/*` integration coverage
2. `Tests/SatisfactoryTools.Planner.Parity.Tests/SatisfactoryTools.Planner.Parity.Tests.csproj`
   - planner request normalization, result graphing, aggregation, fixture parity
3. `Tests/SatisfactoryTools.Web.E2E.Tests/SatisfactoryTools.Web.E2E.Tests.csproj`
   - browser-level smoke coverage for the planner and migrated routes
4. `Tests/SatisfactoryTools.Data.Platform.Tests/SatisfactoryTools.Data.Platform.Tests.csproj`
   - JSON-to-DB import, parity, publish, and admin workflow checks

## Executable QA Scenarios By Milestone

### M0. Migration Baseline
1. Run `dotnet test "SolverService/SatisfactoryTools.Solver.Api.Tests/SatisfactoryTools.Solver.Api.Tests.csproj"`.
2. Run `yarn build`.
3. Confirm all continuity docs and the seeded route/fixture docs exist under `.sisyphus/`.
4. Expected result: current repo still builds/tests cleanly and the migration operating baseline exists.

### M1. Unified ASP.NET Core Shell
1. Start the ASP.NET Core host locally.
2. Run `dotnet test "Tests/SatisfactoryTools.Web.Host.Tests/SatisfactoryTools.Web.Host.Tests.csproj"` once that project is created in this milestone.
3. Run these explicit HTTP checks against the local host with `curl` or an equivalent script:
   - `GET /`
   - `GET /1.1/production`
   - `GET /1.2/production`
   - `GET /items`
   - `GET /assets/app.js`
   - `POST /v2/solver`
4. Expected result: all return the same success semantics as the legacy stack, and Angular still renders unchanged.

### M2. Route-Level Strangler Scaffold
1. Run `dotnet test "Tests/SatisfactoryTools.Web.Host.Tests/SatisfactoryTools.Web.Host.Tests.csproj"`.
2. Run route ownership tests for every route group listed in `.sisyphus/context/dotnet-migration-route-parity-matrix.md`.
3. Verify that migrated and legacy routes do not overlap incorrectly by exercising the route matrix with HTTP requests.
4. Expected result: route ownership is deterministic and deep links still work.

### M3. Planner Domain Port Complete
1. Run `dotnet test "Tests/SatisfactoryTools.Planner.Parity.Tests/SatisfactoryTools.Planner.Parity.Tests.csproj"` once that project is created in this milestone.
2. Execute representative fixture comparisons for `1.1`, `1.1-ficsmas`, `1.2`, share-load, multiplier, and no-result cases defined in `.sisyphus/context/dotnet-migration-planner-fixtures.md`.
3. Expected result: C# request shaping, result graphing, and aggregates match locked current behavior for the covered cases.

### M4. Blazor Planner Beta
1. Run `dotnet test "Tests/SatisfactoryTools.Planner.Parity.Tests/SatisfactoryTools.Planner.Parity.Tests.csproj"`.
2. Run `dotnet test "Tests/SatisfactoryTools.Web.E2E.Tests/SatisfactoryTools.Web.E2E.Tests.csproj"` once the E2E project is created in this milestone.
3. Execute these browser flows in the E2E suite:
   - open planner,
   - create tab,
   - calculate result,
   - save/load share,
   - import/export,
   - switch supported versions.
4. Expected result: beta route is usable and key planner workflows are green behind the feature flag or alternate route.

### M5. Production Planner Cutover
1. Run `dotnet test "Tests/SatisfactoryTools.Web.E2E.Tests/SatisfactoryTools.Web.E2E.Tests.csproj"` against the main `/{version}/production` route.
2. Verify old share URLs still load by covering at least one historical share case in the E2E suite.
3. Verify existing local storage keys still hydrate appropriately using browser tests or targeted component tests.
4. Expected result: cutover route is stable and rollback remains possible.

### M6. Remaining Route Migration
1. Run `dotnet test "Tests/SatisfactoryTools.Web.Host.Tests/SatisfactoryTools.Web.Host.Tests.csproj"`.
2. Run `dotnet test "Tests/SatisfactoryTools.Web.E2E.Tests/SatisfactoryTools.Web.E2E.Tests.csproj"`.
3. Execute route smoke coverage for home, items, buildings, schematics, and detail routes.
4. Verify route parity matrix rows are marked complete only when migrated routes are green.
3. Expected result: the site can run without Angular route fallback.

### M7. Data Platform Foundation
1. Run `dotnet test "Tests/SatisfactoryTools.Data.Platform.Tests/SatisfactoryTools.Data.Platform.Tests.csproj"` once the project is created in this milestone.
2. Execute JSON-import-to-DB parity checks for selected published datasets.
3. Run admin workflow tests for import, validate, diff, and publish in staging.
3. Expected result: DB/admin path is safe in shadow mode without changing runtime reads.

### M8. Database Cutover and Admin Expansion
1. Run `dotnet test "Tests/SatisfactoryTools.Data.Platform.Tests/SatisfactoryTools.Data.Platform.Tests.csproj"`.
2. Run `dotnet test "Tests/SatisfactoryTools.Web.E2E.Tests/SatisfactoryTools.Web.E2E.Tests.csproj"` against the DB-backed runtime.
3. Run admin publish workflow validation.
4. Execute rollback drill documented in the delivery runbook.
4. Expected result: DB-backed runtime is accepted and rollback is proven.

## Success Criteria

1. ASP.NET Core is the only public host.
2. `/{version}/production` is Blazor-backed.
3. Remaining routes have been migrated off AngularJS.
4. PHP is gone.
5. The AngularJS runtime is gone.
6. Node is no longer needed for web runtime delivery.
7. Later, the site can read its content from a DB-backed model and be managed through an admin backend.
