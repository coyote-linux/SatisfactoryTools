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

- Current phase: **M3 in progress**
- Current milestone: **M3 - Planner Domain Port Complete**
- Current completed slice: **M3 slice 1 - planner compatibility/request-shaping layer**
- Current recommended slice: **M3 slice 2 - result-domain port (graph/result aggregation only)**

## Completed Slice

### M1

#### Outcome
The existing ASP.NET host now serves the Angular shell and `www/` asset tree directly, preserves runtime solver URL injection and deep-link fallback behavior, and keeps `/v2/*` API ownership unchanged.

#### Completion Signals
1. `Program.cs` renders `www/index.php` without requiring PHP at runtime and serves static assets from the resolved frontend root.
2. Integration tests cover root shell serving, bare version-root and deep-link fallback, runtime config injection parity, and `/v2/*` non-regression against shell fallback.
3. Local Docker/docs now use the unified ASP.NET host instead of a PHP/Apache proxy path.

### M2

#### Outcome
ASP.NET Core now owns an explicit, testable host route-ownership policy while preserving the current external behavior: `/v2/*` remains API-owned, static/file-like misses do not shell-fallback, supported bare version roots remain eligible for the legacy Angular shell, and all current Angular UI routes still stay on legacy-shell fallback.

#### Completion Signals
1. `Program.cs` delegates shell-fallback eligibility to `Services/HostRouteOwnershipPolicy.cs` instead of embedding the policy inline.
2. Focused host-routing tests cover policy classification and integration boundaries for API-owned routes, shell-fallback-eligible routes, and non-fallback file-like/asset paths.
3. The route ownership docs make the M2 boundary explicit and name the next slice without widening scope into Blazor or planner-domain migration.

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

### M3

#### Objective
Continue porting planner-side business logic to C# under tests before any production-planner UI cutover.

#### Expected Work
1. Keep the new planner compatibility/request-shaping layer stable and reusable.
2. Port the result-domain slice next: `ProductionResultFactory`, `Graph`, `ProductionResult`, and related aggregation/parsing logic.
3. Gate the result-domain port with the existing parity fixtures plus any missing focused planner-domain tests.
4. Keep route ownership and `/v2/*` contracts unchanged while planner-domain parity work lands.

#### Do Not Start Yet
1. Do not start the Blazor planner UI route during planner-domain porting.
2. Do not change route shapes or `/v2/*` contracts while porting planner logic.
3. Do not mix deployment or remaining content-route migration into M3.

## Key Repo Truths To Preserve

1. AngularJS SPA bootstrap: `src/app.ts`
2. Route/version ownership: `src/Module/AppModule.ts`
3. Current shell template source: `www/index.php`
4. Current deep-link fallback owner: `SolverService/SatisfactoryTools.Solver.Api/Program.cs`
5. Current frontend build output: `www/assets/app.js`
6. Existing compatibility endpoints: `SolverService/SatisfactoryTools.Solver.Api/Program.cs`
7. Planner-heavy client logic lives in `ProductionController.ts`, `ProductionTab.ts`, `ProductionResultFactory.ts`, `Graph.ts`, `ProductionResult.ts`, and related node classes.

## Commands Worth Remembering

### Current App Verification
- `yarn build`
- `dotnet test "SolverService/SatisfactoryTools.Solver.Api.Tests/SatisfactoryTools.Solver.Api.Tests.csproj"`
- If the local `SolverService/.../obj/` tree is permission-restricted on this machine, use `dotnet test "SolverService/SatisfactoryTools.Solver.Api.Tests/SatisfactoryTools.Solver.Api.Tests.csproj" --artifacts-path /tmp/satisfactorytools-dotnet-test-artifacts --logger "console;verbosity=minimal"`

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
