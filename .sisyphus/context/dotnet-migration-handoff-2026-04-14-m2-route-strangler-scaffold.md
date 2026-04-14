# .NET Migration Session Handoff

## Session
- Date: 2026-04-14
- Milestone: M2 - Route-Level Strangler Scaffold
- Atomic slice: M2 - Route-Level Strangler Scaffold

## Summary
- What was completed:
  - Extracted host route-ownership and legacy-shell fallback decisions out of `SolverService/SatisfactoryTools.Solver.Api/Program.cs` into `SolverService/SatisfactoryTools.Solver.Api/Services/HostRouteOwnershipPolicy.cs` so ASP.NET now owns those boundaries explicitly instead of inlining them in middleware.
  - Preserved current external behavior exactly for this slice: `/v2/*` remains API-owned and never shell-falls back, file-like/static misses still return `404` instead of shell HTML, supported bare version roots `/1.0`, `/1.0-ficsmas`, `/1.1`, `/1.1-ficsmas`, and `/1.2` remain shell-fallback eligible, fallback stays GET/HEAD-only, and `/index.php` plus static file serving order remain unchanged.
  - Split host-routing coverage out of `SolverApiTests.cs` into focused files by adding direct policy tests and a dedicated ASP.NET host-routing integration suite while leaving solver/share contract coverage intact in the original large suite.
  - Updated the migration route/resume docs so M2 clearly records that ASP.NET owns route-boundary policy while all current Angular UI routes still remain legacy-shell-owned.
- What remains incomplete:
  - No planner-domain C# porting has started yet.
  - No Blazor planner route or broader UI migration work has started yet.

## Files / Areas Touched
- `SolverService/SatisfactoryTools.Solver.Api/Program.cs`
- `SolverService/SatisfactoryTools.Solver.Api/Services/HostRouteOwnershipPolicy.cs`
- `SolverService/SatisfactoryTools.Solver.Api.Tests/SolverApiTests.cs`
- `SolverService/SatisfactoryTools.Solver.Api.Tests/HostRouteOwnershipPolicyTests.cs`
- `SolverService/SatisfactoryTools.Solver.Api.Tests/HostRoutingIntegrationTests.cs`
- `SolverService/SatisfactoryTools.Solver.Api.Tests/TestApplicationFactoryExtensions.cs`
- `SolverService/SatisfactoryTools.Solver.Api.Tests/FrontendTestSite.cs`
- `.sisyphus/context/dotnet-migration-route-parity-matrix.md`
- `.sisyphus/context/dotnet-migration-resume-guide.md`
- `.sisyphus/context/dotnet-migration-handoff-2026-04-14-m2-route-strangler-scaffold.md`

## Validation
- Commands run:
  - `dotnet test "SolverService/SatisfactoryTools.Solver.Api.Tests/SatisfactoryTools.Solver.Api.Tests.csproj"` *(failed due local permission denial writing to the existing project `obj/` tree)*
  - `dotnet test "SolverService/SatisfactoryTools.Solver.Api.Tests/SatisfactoryTools.Solver.Api.Tests.csproj" --artifacts-path /tmp/satisfactorytools-m2-artifacts --logger "console;verbosity=minimal"`
  - `yarn build`
  - live ASP.NET smoke test via `dotnet run --project SolverService/SatisfactoryTools.Solver.Api/SatisfactoryTools.Solver.Api.csproj --urls=http://127.0.0.1:8091 --artifacts-path /tmp/satisfactorytools-m2-run`
- Results:
  - `dotnet test` with `--artifacts-path`: passed `76/76`
  - `yarn build`: passed (existing Sass/Bootstrap deprecation warnings remain)
  - live ASP.NET smoke: passed for `/`, `/1.2`, `/1.2/production?share=test-share`, `HEAD /1.2/production`, `/v2/`, and non-fallback `404` behavior for `/v2/not-a-route` and `/assets/missing.js`

## Last Green State
- Commit SHA: `d0e26b6`
- Why this is green:
  - This is the last committed M1-complete branch state before the M2 working-tree changes; the slice itself is validated in the current working tree.

## Open Issues / Blockers
- Local default `dotnet test` writes into a permission-restricted existing `SolverService/.../obj/` tree on this machine, so validation currently needs `--artifacts-path` to bypass that environment issue.

## Decisions Updated
- M2 makes the host ownership boundary explicit in code and tests without changing any route shape or planner/UI ownership.

## Exact Next Slice
- M3 - Planner Domain Port Complete

## Resume Notes For The Next Session
1. Read `.sisyphus/plans/dotnet-migration-plan.md`
2. Read `.sisyphus/context/dotnet-migration-resume-guide.md`
3. Read `.sisyphus/context/dotnet-migration-decision-log.md`
4. Read `.sisyphus/context/dotnet-migration-route-parity-matrix.md`
5. Read `.sisyphus/context/dotnet-migration-handoff-2026-04-14-m2-route-strangler-scaffold.md`
6. Start M3 by porting planner-domain/version-normalization logic under parity tests while keeping the new host route-ownership tests green and route contracts unchanged
