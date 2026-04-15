# .NET Migration Session Handoff

## Session
- Date: 2026-04-15
- Milestone: M3 - Planner Domain Port Complete
- Atomic slice: M3 slice 6 - host-internal planner runtime route over the internal calculation seam

## Summary
- What was completed:
  - Added the first real non-test runtime adopter of `InternalPlannerCalculationService` through `POST /_internal/planner/calculate` in `Program.cs`.
  - Kept `/v2/solver` on its legacy raw `SolverRequest -> ProductionPlannerSolver -> { code, result, debug }` path, with existing public contract behavior unchanged.
  - Marked `/_internal/planner/*` as API-owned in `HostRouteOwnershipPolicy` so unknown internal planner routes return API 404s rather than shell HTML fallback.
  - Added a transport-safe internal response projection for planner-facing `graph`, `details`, and `visualization` output so the host route can serialize composed results without graph cycles.
  - Added focused route, ownership, serialization, and fixture-backed integration tests proving planner-only state such as `powerConsumptionMultiplier` is preserved through the internal runtime path while `/v2/solver` remains unchanged.
- What remains incomplete:
  - No guarded non-test caller uses `/_internal/planner/calculate` yet.
  - No Blazor/UI cutover work has started.

## Files / Areas Touched
- `SolverService/SatisfactoryTools.Solver.Api/Services/InternalPlannerCalculationResponse.cs`
- `SolverService/SatisfactoryTools.Solver.Api/Program.cs`
- `SolverService/SatisfactoryTools.Solver.Api/Services/HostRouteOwnershipPolicy.cs`
- `SolverService/SatisfactoryTools.Solver.Api/Contracts/SolverJson.cs`
- `SolverService/SatisfactoryTools.Solver.Api.Tests/HostRouteOwnershipPolicyTests.cs`
- `SolverService/SatisfactoryTools.Solver.Api.Tests/HostRoutingIntegrationTests.cs`
- `SolverService/SatisfactoryTools.Solver.Api.Tests/InternalPlannerCalculationServiceTests.cs`
- `SolverService/SatisfactoryTools.Solver.Api.Tests/SolverApiTests.cs`
- `.sisyphus/plans/dotnet-migration-plan.md`
- `.sisyphus/context/dotnet-migration-resume-guide.md`
- `.sisyphus/context/dotnet-migration-planner-fixtures.md`
- `.sisyphus/context/dotnet-migration-route-parity-matrix.md`
- `.sisyphus/context/dotnet-migration-decision-log.md`
- `.sisyphus/context/dotnet-migration-handoff-2026-04-15-m3-slice6-internal-runtime-route.md`

## Validation
- Commands run:
  - `dotnet test "SolverService/SatisfactoryTools.Solver.Api.Tests/SatisfactoryTools.Solver.Api.Tests.csproj" --filter "FullyQualifiedName~InternalPlannerCalculationServiceTests|FullyQualifiedName~HostRouteOwnershipPolicyTests|FullyQualifiedName~HostRoutingIntegrationTests|FullyQualifiedName~SolverApiTests" --artifacts-path /tmp/satisfactorytools-m3-slice6-targeted-artifacts --logger "console;verbosity=minimal"`
  - `dotnet test "SolverService/SatisfactoryTools.Solver.Api.Tests/SatisfactoryTools.Solver.Api.Tests.csproj" --artifacts-path /tmp/satisfactorytools-m3-slice6-full-artifacts --logger "console;verbosity=minimal"`
  - `yarn build`
- Results:
  - targeted slice-6 tests: passed `86/86`
  - full `dotnet test` with isolated artifact path: passed `145/145`
  - `yarn build`: passed (existing Sass/Bootstrap deprecation warnings remain)

## Review Status
- Post-implementation review found the code/test slice acceptable.
- The only blocking follow-up was continuity documentation: slice 6 needed plan/resume/route-parity/handoff updates before the slice could be considered green under the repo’s own rules.
- Important accepted boundary:
  - `/_internal/planner/calculate` is the first real runtime consumer of the internal calculation seam, but it is intentionally internal-only and not part of the public `/v2/*` compatibility surface.

## Last Green State
- Commit SHA: `492ef2e`
- Why this is green:
  - This is the last committed branch state before the slice-6 working-tree changes; slice-6 validation is green in the current working tree with no public `/v2/*` contract changes applied.

## Open Issues / Blockers
- The new internal route is only runtime-hosted, not yet adopted by a guarded non-test caller outside direct integration tests.
- `SolverJson.InternalPlannerResponseOptions` allows named floating-point literals for the internal route; keep that serializer confined to this internal-only surface.
- If future infrastructure exposes `/_internal/planner/*` publicly, its “internal-only” assumption must be revisited explicitly.

## Decisions Updated
- M3 slice 6 is accepted as adopting the internal calculation seam through a host-internal route rather than through `/v2/solver`.

## Exact Next Slice
- M3 slice 7 - adopt `/_internal/planner/calculate` from the first guarded non-test caller while keeping `/v2/*` unchanged and avoiding Blazor/UI cutover.

## Resume Notes For The Next Session
1. Read `.sisyphus/plans/dotnet-migration-plan.md`
2. Read `.sisyphus/context/dotnet-migration-resume-guide.md`
3. Read `.sisyphus/context/dotnet-migration-planner-fixtures.md`
4. Read `.sisyphus/context/dotnet-migration-route-parity-matrix.md`
5. Read `.sisyphus/context/dotnet-migration-decision-log.md`
6. Read `.sisyphus/context/dotnet-migration-handoff-2026-04-15-m3-slice6-internal-runtime-route.md`
7. Start the first guarded non-test caller adoption of `/_internal/planner/calculate` without widening into public route changes, endpoint contract changes, or Blazor UI work.
