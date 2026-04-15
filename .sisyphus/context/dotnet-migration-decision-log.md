# .NET Migration Decision Log

## Accepted Decisions

### D001 - Migration strategy is incremental strangler
- Status: Accepted
- Reason: Lowest-risk path for this repo shape.
- Implication: Route-level replacement and compatibility preservation are preferred over a big-bang rewrite.

### D002 - End-state is a single ASP.NET Core web surface
- Status: Accepted
- Reason: Simplifies deployment and removes PHP from runtime.
- Implication: Shell, assets, deep-link fallback, and `/v2/*` should ultimately be served from ASP.NET Core.

### D003 - Planner-heavy UI should migrate to Blazor
- Status: Accepted
- Reason: The planner is highly interactive and stateful.
- Implication: Do not optimize the main migration around a mostly MVC/Razor Pages approach.

### D004 - JSON remains canonical during runtime migration
- Status: Accepted
- Reason: Runtime and storage migration should not be coupled.
- Implication: Database/admin work is deferred until after host/planner stabilization.

### D005 - `/v2/solver` and `/v2/share` remain stable compatibility anchors
- Status: Accepted
- Reason: Existing live deployment and frontend flows already depend on them.
- Implication: Avoid API redesign during the main UI/runtime migration.

### D006 - M3 visualization parity follows the active `ProductionResult` + `VisualizationComponentController` path
- Status: Accepted
- Reason: That is the live result-rendering path in the Angular app, while `ProductionToolResult.ts` is a legacy direct-visualization path and not the primary current source of truth.
- Implication: The C# result visualization/view-model port should mirror node/edge/ELK payload shaping from the active path first and treat `ProductionToolResult.ts` only as a secondary cross-check.

### D007 - Planner result composition must preserve the TS split between public solver request and local planner-facing composition state
- Status: Accepted
- Reason: The frontend strips planner-only fields such as `powerConsumptionMultiplier` before `/v2/solver` but still uses the full planner-facing request when composing local planner results.
- Implication: The C# bridge must derive the unchanged stripped solver request for public contract parity, then restore planner-only state only on an internal composition request when building planner-facing results.

### D008 - M3 slice 5 establishes a host-resolved internal calculation seam before any public planner-result contract work
- Status: Accepted
- Reason: The migration still needs one internal entrypoint that later runtime planner flows can adopt incrementally without binding those future consumers directly to the lower-level composition bridge.
- Implication: `InternalPlannerCalculationService` is an acceptable narrow consumer seam for M3 slice 5, but the next slice must wire it into the first real non-test planner-facing runtime flow while keeping `/v2/*` unchanged.

## Open Decisions

### O001 - Extend existing ASP.NET Core host project or create a new unified host project
- Current leaning: extend the existing host first for speed, refactor later if needed.
- Decision needed by: before M1 implementation begins.

### O002 - Final Blazor hosting mode for planner
- Options: Blazor Web App with interactive client-side execution, or another Blazor-hosting shape.
- Decision needed by: before M4 implementation begins.

### O003 - Graph rendering strategy in Blazor
- Options: retain current JS graph libraries with interop, or replace later.
- Decision needed by: during M3-M4.

### O004 - Database engine and admin auth model
- Options: PostgreSQL vs SQL Server; auth strategy for admin backend.
- Decision needed by: before M7 implementation begins.

## Decision Rules

1. Every meaningful architectural choice gets recorded here.
2. Do not silently change migration direction without updating this file.
3. Every session handoff should reference any open decisions touched that session.
