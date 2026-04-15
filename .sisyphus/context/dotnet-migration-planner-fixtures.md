# .NET Migration Planner Fixtures

## Purpose

This document tracks the minimum representative planner cases that must be locked before the planner domain is ported.

## Minimum Fixture Set

| Fixture ID | Version | Scenario | Current Coverage | Next Needed Assertion | Status |
|---|---|---|---|---|---|
| F001 | 1.1 | simple product solve | `Fixtures/Planner/F001.json` + fixture-driven `SolverApiTests.cs` + `PlannerResultDomainFactoryTests.cs` + `PlannerResultVisualizationFactoryTests.cs` + `PlannerResultCompositionServiceTests.cs` + `InternalPlannerCalculationServiceTests.cs` | keep as the primary happy-path anchor for planner result composition parity and internal runtime route coverage | Captured |
| F002 | 1.1-ficsmas | seasonal dataset solve | `Fixtures/Planner/F002.json` + fixture-driven `SolverApiTests.cs` | use as the baseline seasonal dataset fixture in later parity suites | Captured |
| F003 | 1.2 | basic planner solve | `Fixtures/Planner/F003.json` + fixture-driven `SolverApiTests.cs` + `PlannerResultCompositionServiceTests.cs` + `InternalPlannerCalculationServiceTests.cs` | preserve as the default non-seasonal `1.2` planner baseline and power-multiplier bridge/internal-route anchor | Captured |
| F004 | 1.2 | share round-trip | `Fixtures/Planner/F004.json` + fixture-driven `SolverApiTests.cs` + `PlannerBrowserRegressionTests.cs` | preserve for guarded share-load compatibility checks, especially first-load shared-tab activation, visualization-backed guarded rendering, and the empty-storage/empty-array edge cases | Captured |
| F005 | 1.2 | recipe multiplier case | `Fixtures/Planner/F005.json` + fixture-driven `SolverApiTests.cs` + `PlannerResultDomainFactoryTests.cs` + `PlannerResultVisualizationFactoryTests.cs` + `PlannerResultCompositionServiceTests.cs` | reuse to lock planner-facing recipe-cost result composition parity after the new bridge layer | Captured |
| F006 | 1.2 | Packager exception case | `Fixtures/Planner/F006.json` + fixture-driven `SolverApiTests.cs` + `PlannerResultDomainFactoryTests.cs` + `PlannerResultVisualizationFactoryTests.cs` | preserve as the Packager `1.0x` exception anchor for planner result composition and view-model parity | Captured |
| F007 | 1.2 | no-result debug case | `Fixtures/Planner/F007.json` + fixture-driven `SolverApiTests.cs` + `InternalPlannerCalculationServiceTests.cs` + `PlannerBrowserRegressionTests.cs` | preserve as the locked `NO_RESULT` + debug anchor for internal planner runtime parity while keeping debug projection scoped to guarded/internal flows | Captured |
| F008 | 1.2 | manual-input recipe case | `Fixtures/Planner/F008.json` + fixture-driven `SolverApiTests.cs` | preserve as the manual-input/debug anchor for M3 parity work | Captured |

## Concrete Seed Definitions

### F001 - 1.1 baseline solve
1. Route context: `/1.1/production`
2. Expected solver payload version: `1.1.0`
3. Expected result status: `RESULT`

### F002 - 1.1-ficsmas seasonal solve
1. Route context: `/1.1-ficsmas/production`
2. Expected solver payload version: `1.0.0-ficsmas`
3. Expected dataset mapping: `data1.0-ficsmas.json`

### F003 - 1.2 baseline solve
1. Route context: `/1.2/production`
2. Expected solver payload version: `1.2.0`
3. Baseline current contract reference: `IronPlateSolveReturnsResultEnvelope`

### F004 - share round-trip
1. Share create route: `POST /v2/share/?version=<activeVersion>`
2. Expected link shape: `/{version}/production?share=<id>`
3. Load route: `GET /v2/share/{shareId}`
4. Current contract reference: `ShareEndpointsRoundTripPlannerPayload`
5. This fixture is the share-flow anchor; it does not also carry a solver execution expectation.

### F005 - 1.2 multiplier behavior
1. Multipliers are only preserved for `1.2`
2. Recipe and power multiplier behaviors should stay unchanged from current contract tests

### F006 - Packager exception
1. Machine class `Desc_Packager_C` remains effectively `1.0x`
2. Graph/result parity must not scale Packager ingredients

### F007 - no-result + debug
1. Expected planner result status: `NO_RESULT`
2. Expected debug behavior: `solverDebug` available when debug is enabled

### F008 - manual-input recipe case
1. This fixture currently locks the debug/diagnostic path for a manual-input-only item when the required input is missing.
2. Biomass-style inputs must continue to be diagnosable; a successful manual-input parity fixture still needs to be added in a later slice if required.

## Usage Rules

1. Every fixture must identify planner route context and storage key, and should include the subset of captured expectations that actually apply to that scenario.
2. In the current captured set, F001/F002/F003/F005/F006/F007/F008 carry solve expectations, while F004 carries the share round-trip expectation.
3. **M3 has now started** with the first planner-domain slice: C# planner compatibility/request-shaping parity tests consume these fixtures directly where applicable.
4. **M3 slice 2 is now complete**: F001, F005, and F006 carry targeted `resultDomainExpectation` blocks consumed by `PlannerResultDomainFactoryTests.cs` to gate the first C# result-domain port.
5. **M3 slice 3 is now complete**: F001, F005, and F006 also carry targeted `resultVisualizationExpectation` blocks consumed by `PlannerResultVisualizationFactoryTests.cs` to gate visual node/edge shaping parity.
6. **M3 slice 4 is now complete**: `PlannerResultCompositionServiceTests.cs` uses F001/F003/F005 plus canonical request-parity coverage to gate the new internal planner result bridge.
7. **M3 slice 5 is now complete**: `InternalPlannerCalculationServiceTests.cs` uses F001/F003 to gate the first host-resolved internal consumer seam over that bridge, while `SolverApiTests.cs` keeps `/v2/solver` locked to the legacy raw envelope.
8. **M3 slice 6 is now complete**: `SolverApiTests.cs` uses F001/F003 to gate the first host-internal runtime route over the internal calculation seam, while host-routing tests keep `/_internal/planner/*` API-owned and off shell fallback.
9. **M3 slice 7 is now complete**: the guarded non-test caller adoption keeps the Angular production planner on a default-off host-injected branch that consumes internal planner `details`/`visualization`, while F007 now also locks internal no-result debug projection parity.
10. **M3 slice 8a is now complete**: the guarded planner path no longer widens legacy graph expectations, the shared-entry empty-array case is hardened, and internal planner route/config/debug exposure is reduced while `/v2/*` remains unchanged.
11. **M3 slice 8b is now complete**: `PlannerBrowserRegressionTests.cs` adds committed real-browser coverage for F004-style guarded share activation, visualization-backed guarded rendering/layout, and F007 guarded no-result debug/internal-route behavior without widening `/v2/*` or enabling guarded mode by default.
12. The next M3 slice is **9**: review guarded default-on readiness, internal-route access restriction, and rollback expectations before widening guarded-path usage.
13. `SolverService/SatisfactoryTools.Solver.Api.Tests/Fixtures/Planner/*.json` is now the authoritative captured artifact set for F001-F008.
