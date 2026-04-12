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
- Updated the default solver path to same-origin `/v2/solver` so deployed forks can proxy browser solve requests through their own host.
- Updated the site metadata, homepage copy, and shared footer/community links to describe this fork while keeping visible attribution to the original project and author.
- Updated the deployment docs to explain the same-origin solver proxy requirement and to note that sharing still depends on the upstream hosted share API.

### Removed
- Removed `script-loader` and `angular-templatecache-loader` from the build dependency chain.
- Removed the inherited Matomo analytics snippet from `www/index.php`.

### Fixed
- Aligned lockfiles after the dependency cleanup so the repo no longer references removed legacy webpack loaders.
- Documented the current runtime version boundary correctly: the planner now exposes `1.1`, `1.1-ficsmas`, and `1.2`, with `1.2` using the same recipe dataset as `1.1`.
- Restored self-hosted production-planner solving for common cases without depending on the private upstream solver codebase.
- Recipe cost multiplier changes now affect actual solve behavior instead of stopping at frontend state only.
- Corrected production-graph ingredient flow labels so recipe cost multiplier changes now propagate into visualization edges and Items-tab consumption totals.
- Corrected Items-tab producer totals so externally supplied inputs are counted in item production breakdowns.
- Fixed the deployment configuration for `ficsit.spugnort.com` so the fork now serves correctly over Apache with the local solver proxied behind the same origin.
