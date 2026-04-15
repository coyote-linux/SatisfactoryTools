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

### D009 - M3 slice 6 adopts the internal calculation seam through a host-internal route rather than through `/v2/solver`
- Status: Accepted
- Reason: `InternalPlannerCalculationService` requires full `PlannerState`, including planner-only fields such as `powerConsumptionMultiplier`, while `/v2/solver` intentionally remains the stripped public raw-solver contract.
- Implication: The first real runtime consumer should be a host-internal route such as `/_internal/planner/calculate`, keeping `/v2/solver` and `/v2/share` unchanged while still moving planner-facing runtime behavior onto the server.

### D010 - M3 slice 7 keeps guarded internal planner adoption above `Solver.solveProduction()`
- Status: Accepted
- Reason: `/_internal/planner/calculate` already returns planner-facing `graph`/`details`/`visualization`, while `Solver.solveProduction()` and `/v2/solver` remain the legacy raw-solver compatibility path.
- Implication: The first guarded non-test caller should use a planner-specific client and local adapter in the Angular production planner, not overload `Solver.solveProduction()` or retrofit the internal route back into the public `{ code, result, debug }` envelope.

### D011 - M3 slice 8 hardens the guarded planner boundary before any default-on decision
- Status: Accepted
- Reason: The final slice-7 review passed, but it surfaced non-blocking follow-ups around the local planner/result type contract, the guarded share-entry empty-array edge case, internal-route exposure/debug-leakage assumptions, and the absence of frontend regression coverage for the guarded Angular path.
- Implication: The next slice should tighten those four areas without widening `/v2/*`, without deleting the legacy Angular solver path, and without starting UI cutover work.

### D012 - M3 slice 8 proceeds as 8a boundary hardening before 8b browser/frontend regression coverage
- Status: Accepted
- Reason: The repo already has strong xUnit/API coverage for the guarded planner path, but it does not yet have committed browser/frontend regression infrastructure. That makes guarded boundary hardening a coherent first slice and browser/frontend regression coverage a coherent follow-on slice.
- Implication: 8a may land the planner/result boundary tightening, shared-entry empty-array hardening, and internal-route/config/debug surface reduction without making any default-on decision; 8b is still required before widening guarded-path usage.

### D013 - Post-8b guarded-path widening remains a separate explicit gate
- Status: Accepted
- Reason: Browser/frontend regression coverage is now committed, but the guarded path is still default-off and `/_internal/planner/calculate` remains a callable same-origin route whose exposure should be reviewed deliberately before widening usage.
- Implication: The next slice should evaluate rollback, route-access restriction, and whether guarded default-on is acceptable without mixing that decision into Blazor/UI cutover work.

### D014 - M3 slice 9 hardens the internal planner route before any guarded default-on rollout
- Status: Accepted
- Reason: The smallest safe follow-on slice after 8b is to put an explicit host-side same-origin gate on `/_internal/planner/calculate` while leaving the guarded planner path default-off and keeping rollback to the existing runtime flag.
- Implication: The next slice may review guarded default-on rollout behind the hardened internal route, but it should not revisit `/v2/*` contracts or couple rollout with UI cutover work.

### D015 - M3 slice 10 enables guarded planner calculation by default while keeping explicit rollback
- Status: Accepted
- Reason: The guarded internal planner route now has the minimum hardening and browser/frontend coverage needed for the repo-side default flip, and the smallest safe slice is to change the injected shell default without widening contracts or route ownership.
- Implication: The unified host now prefers same-origin `/_internal/planner/calculate` for planner solves, while `Planner:UseInternalCalculate=false` / `USE_INTERNAL_PLANNER_CALCULATE=false` remains the one-flag rollback path back to `/v2/solver`. The next slice should validate deployed proxy/topology behavior rather than widen scope into UI cutover.

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
