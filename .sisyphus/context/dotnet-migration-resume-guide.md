# .NET Migration Resume Guide

## Purpose

This file is the fast-start guide for resuming the migration across sessions.

The goal is that a new session should be able to re-enter the work in under 10 minutes without rediscovering the repo.

## Read Order At The Start Of Every Session

1. `.sisyphus/plans/dotnet-migration-plan.md`
2. `.sisyphus/context/dotnet-migration-decision-log.md`
3. `.sisyphus/context/dotnet-migration-route-parity-matrix.md`
4. the latest session handoff note created from `.sisyphus/context/dotnet-migration-handoff-template.md`

## Current Status

- Current phase: **M0 Slice 1B completed**
- Current milestone: **M0 - Migration Baseline**
- Current recommended slice: **M1 - Unified ASP.NET Core Shell**

## Completed Slice

### M0 Slice 1A

#### Outcome
Locked the first compatibility-anchor edge contracts in the existing ASP.NET integration test harness and replaced placeholder migration docs with concrete route/version and fixture seed information.

#### Completion Signals
1. `SolverApiTests.cs` includes share-version, share-validation, invalid-share-id, and unknown-solver-member baseline tests.
2. Route inventory and version facts are now explicit in the route parity matrix.
3. Planner fixture catalog is seeded with current backing tests and next parity assertions.

### M0 Slice 1B

#### Outcome
Captured file-backed planner parity fixtures F001-F008 inside the existing solver API test project, executed them through the current ASP.NET harness, and closed the local same-origin `/v2/*` testing gap in Docker Compose/docs.

#### Completion Signals
1. `SolverApiTests.cs` loads `Fixtures/Planner/F001.json` through `F008.json` and executes fixture-backed route/storage, solve, and share assertions.
2. The planner fixtures doc marks F001-F008 as captured rather than seeded.
3. `docker-compose.yml` and local docs describe same-origin `/v2/solver` and `/v2/share/*` proxying through the web container.

## Immediate Next Slice

### M1

#### Objective
Replace `www/index.php` and Apache-specific runtime responsibilities with ASP.NET Core hosting while keeping the Angular app behavior unchanged.

#### Expected Work
1. Add an ASP.NET Core shell equivalent for the current PHP front controller.
2. Preserve deep-link fallback, asset serving, and runtime solver config injection behavior.
3. Keep `/v2/*` compatibility endpoints green while the host ownership changes.

#### Do Not Start Yet
1. Do not start the Blazor planner route during host-shell takeover.
2. Do not mix planner-domain porting into the shell replacement slice.
3. Do not change route shapes or `/v2/*` contracts while moving host ownership.

## Key Repo Truths To Preserve

1. AngularJS SPA bootstrap: `src/app.ts`
2. Route/version ownership: `src/Module/AppModule.ts`
3. Current shell: `www/index.php`
4. Current deep-link behavior: `www/.htaccess`
5. Current frontend build output: `www/assets/app.js`
6. Existing compatibility endpoints: `SolverService/SatisfactoryTools.Solver.Api/Program.cs`
7. Planner-heavy client logic lives in `ProductionController.ts`, `ProductionTab.ts`, `ProductionResultFactory.ts`, `Graph.ts`, `ProductionResult.ts`, and related node classes.

## Commands Worth Remembering

### Current App Verification
- `yarn build`
- `dotnet test "SolverService/SatisfactoryTools.Solver.Api.Tests/SatisfactoryTools.Solver.Api.Tests.csproj"`

### Current Live Deployment Pattern
- app source deployed to `/srv/satisfactorytools/current`
- solver published to `/srv/satisfactorytools/publish/solver`
- service: `satisfactorytools-solver.service`

## Green Checkpoint Rules

A slice is only green when:

1. tests relevant to the slice pass,
2. working tree is intentional and understood,
3. the handoff note records exactly what changed,
4. the next slice is named explicitly.

## Resume Risk Reminders

1. Do not mix host migration with data-platform migration.
2. Do not mix planner UI cutover with planner-domain porting in one slice.
3. Keep `/v2/*`, share links, and route shapes stable until a later explicit decision.
4. Keep JSON canonical until the runtime migration is stable.

## What “Done” Looks Like For The Whole Migration

1. ASP.NET Core is the only public host.
2. Blazor owns the planner route.
3. AngularJS runtime is removed.
4. PHP runtime is removed.
5. Node is removed from web runtime delivery.
6. Database/admin work happens after runtime stability, not mixed into the risky early phases.
