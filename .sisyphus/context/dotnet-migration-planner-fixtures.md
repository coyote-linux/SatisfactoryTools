# .NET Migration Planner Fixtures

## Purpose

This document tracks the minimum representative planner cases that must be locked before the planner domain is ported.

## Minimum Fixture Set

| Fixture ID | Version | Scenario | Why It Matters | Status |
|---|---|---|---|---|
| F001 | 1.1 | simple product solve | baseline request/result path | Planned |
| F002 | 1.1-ficsmas | seasonal dataset solve | verifies version-specific dataset behavior | Planned |
| F003 | 1.2 | basic planner solve | baseline current production path | Planned |
| F004 | 1.2 | share round-trip | preserves `/v2/share` and route link semantics | Planned |
| F005 | 1.2 | recipe multiplier case | preserves current 1.2 multiplier behavior | Planned |
| F006 | 1.2 | Packager exception case | preserves Packager `1.0x` behavior | Planned |
| F007 | 1.2 | no-result debug case | preserves structured debug output behavior | Planned |
| F008 | 1.2 | manual-input recipe case | preserves Biomass-style manual input handling | Planned |

## Usage Rules

1. Every fixture should identify input payload, expected key outputs, and expected route/version context.
2. Do not start M3 without enough fixture coverage to expose parity drift in planner-domain code.
