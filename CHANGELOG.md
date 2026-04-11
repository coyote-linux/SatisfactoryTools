# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased]

### Added
- Added `docs/architecture.md` to document the application boot flow, deployment model, and runtime requirements.
- Added `docker-compose.yml` with a Node 24 builder container and a `php:8.2-apache` web container for local testing.
- Added this changelog to track the ongoing stack-modernization work and the remaining path to Satisfactory v1.2 support.
- Added a new C# replacement solver service under `SolverService/` with a compatible `/v2/solver` HTTP API and initial OR-Tools-based planning engine.
- Added 1.2-only production-planner controls for recipe cost and power multipliers.

### Changed
- Modernized the frontend build pipeline for current Node versions by replacing legacy webpack loader usage with built-in HTML asset handling and standard module imports.
- Updated the webpack Sass pipeline to use the modern Sass API with explicit load paths for package resolution.
- Updated project metadata and setup guidance to declare the modern Node/Yarn workflow used by CI and local builds.
- Expanded repository documentation to cover architecture, deployment behavior, and the local Docker-based testing flow.
- Updated the frontend solver client to use a configurable solver URL so Docker/local environments can target the new C# service instead of the hosted production solver.
- Updated Docker Compose to run the local C# solver alongside the web app and builder.
- Updated the local C# solver to accept and apply a `recipeCostMultiplier` for 1.2 production plans.

### Removed
- Removed `script-loader` and `angular-templatecache-loader` from the build dependency chain.

### Fixed
- Aligned lockfiles after the dependency cleanup so the repo no longer references removed legacy webpack loaders.
- Documented the current runtime version boundary: the planner boot flow still recognizes `0.8`, `1.0`, and `1.0-ficsmas`, so Satisfactory v1.2 planner support remains pending.
- Restored self-hosted production-planner solving for common cases without depending on the private upstream solver codebase.
- Recipe cost multiplier changes now affect actual solve behavior instead of stopping at frontend state only.
- Corrected production-graph ingredient flow labels so recipe cost multiplier changes now propagate into visualization edges and Items-tab consumption totals.
- Corrected Items-tab producer totals so externally supplied inputs are counted in item production breakdowns.
