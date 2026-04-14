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

- Current phase: **Planning complete, execution not started**
- Current milestone: **M0 - Migration Baseline**
- Current recommended slice: **M0 Slice 1 - Migration operating baseline**

## Immediate Next Slice

### M0 Slice 1

#### Objective
Create the operating baseline required for safe migration work.

#### Expected Work
1. Add contract tests for `/v2/solver` and `/v2/share`.
2. Create the first route/version inventory from `src/Module/AppModule.ts`.
3. Capture the first planner characterization fixtures.
4. Seed the remaining internal docs needed for execution if they do not already exist.

#### Do Not Start Yet
1. Do not replace `www/index.php` before the baseline tests exist.
2. Do not port planner logic before the fixture set exists.
3. Do not change route ownership before route parity inventory exists.

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
