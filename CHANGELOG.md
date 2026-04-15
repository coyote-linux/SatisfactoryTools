# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased]

### Added
- Added `docs/architecture.md` to document the application boot flow, deployment model, and runtime requirements.
- Added `docker-compose.yml` with a Node 24 builder container and a `php:8.2-apache` web container for local testing.
- Added this changelog to track the ongoing stack-modernization work and the remaining path to Satisfactory v1.2 support.
- Added a new C# replacement solver service under `SolverService/` with a compatible `/v2/solver` HTTP API and initial OR-Tools-based planning engine.
- Added 1.2-only production-planner controls for recipe cost and power multipliers.
- Added a local file-backed share store and same-origin `/v2/share` endpoints to the C# service so planner sharing can run without the upstream hosted API.
- Added an optional production-planner debug panel so infeasible solve requests can show structured solver diagnostics directly in the UI.
- Added a resumable `.NET` migration planning package under `.sisyphus/` with milestone sequencing, route/version parity notes, validation gates, risk tracking, and handoff templates.
- Added file-backed planner parity fixtures `F001` through `F008` under `SolverService/SatisfactoryTools.Solver.Api.Tests/Fixtures/Planner/` to lock representative `1.1`, `1.1-ficsmas`, `1.2`, share, multiplier, Packager, and debug scenarios in executable tests.
- Added a C# planner compatibility/request-shaping layer that mirrors the Angular planner's version normalization, storage-key mapping, legacy schema upgrades, resource defaulting, and canonical solver-request derivation under direct fixture-backed tests.
- Added a C# non-visual planner result-domain layer that parses raw solver keys into graph nodes/edges and canonical planner aggregations under targeted fixture-backed parity tests.

### Changed
- Modernized the frontend build pipeline for current Node versions by replacing legacy webpack loader usage with built-in HTML asset handling and standard module imports.
- Updated the webpack Sass pipeline to use the modern Sass API with explicit load paths for package resolution.
- Updated project metadata and setup guidance to declare the modern Node/Yarn workflow used by CI and local builds.
- Expanded repository documentation to cover architecture, deployment behavior, and the local Docker-based testing flow.
- Updated the frontend solver client to use a configurable solver URL so Docker/local environments can target the new C# service instead of the hosted production solver.
- Updated Docker Compose to run the local C# solver alongside the web app and builder.
- Updated the local C# solver to accept and apply a `recipeCostMultiplier` for 1.2 production plans, with Packager recipes intentionally staying at `1.0x`.
- Updated the default solver path to same-origin `/v2/solver` so deployed forks can proxy browser solve requests through their own host.
- Updated the site metadata, homepage copy, and shared footer/community links to describe this fork while keeping visible attribution to the original project and author.
- Updated the deployment docs to explain the same-origin solver and share proxy requirements for self-hosted forks.
- Updated planner sharing to create and load saved plans through the local same-origin `/v2/share` API.
- Refined the 1.2 production-planner solver to maximize requested outputs first, then minimize weighted resource extraction, then break ties with recipe power cost.
- Refreshed the site shell and production-planner styling with the darker amber fork theme.
- Updated the solver API test harness to load fixture-backed planner parity artifacts and assert current route/storage, solve, and share compatibility behavior from the captured baseline.
- Updated local Docker Compose testing to proxy same-origin `/v2/solver` and `/v2/share/*` through the Apache web container to the local ASP.NET compatibility service.
- Updated the ASP.NET host to serve the Angular shell and `www/` asset tree directly, replacing PHP/Apache runtime duties for the active local testing path.
- Updated the ASP.NET host to use an explicit route-ownership policy so `/v2/*`, static/file-like paths, and legacy Angular shell fallback remain separated and directly testable during the strangler migration.

### Removed
- Removed `script-loader` and `angular-templatecache-loader` from the build dependency chain.
- Removed the inherited Matomo analytics snippet from `www/index.php`.

### Fixed
- Aligned lockfiles after the dependency cleanup so the repo no longer references removed legacy webpack loaders.
- Documented the current runtime version boundary correctly: the planner now exposes `1.1`, `1.1-ficsmas`, and `1.2`, with `1.2` using the same recipe dataset as `1.1`.
- Restored self-hosted production-planner solving for common cases without depending on the private upstream solver codebase.
- Recipe cost multiplier changes now affect actual solve behavior instead of stopping at frontend state only, except for Packager recipes which intentionally remain at `1.0x`.
- Corrected production-graph ingredient flow labels so recipe cost multiplier changes now propagate into visualization edges and Items-tab consumption totals.
- Corrected Items-tab producer totals so externally supplied inputs are counted in item production breakdowns.
- Fixed the deployment configuration for `ficsit.spugnort.com` so the fork now serves correctly over Apache with the local solver proxied behind the same origin.
- Removed the remaining runtime dependency on `api.satisfactorytools.com` for planner share creation and loading.
- Corrected planner recipe weighting so rare or costly resource conversions are no longer favored over cheaper direct resources in equivalent solutions.
- Corrected 1.2 recipe cost multiplier handling so solid inputs round to the nearest whole recipe cost with a minimum of 1, fluid inputs keep decimal recipe costs, and Packager recipes remain fixed at `1.0x`.
- Corrected imported planner tabs so missing 1.2 raw-resource defaults such as SAM are restored instead of silently loading with unusable zero caps.
- Fixed planner no-result diagnostics to explain disabled alternates, missing manual inputs, and Packager/Turbofuel dead ends without surfacing misleading package or unpackage loops first.
- Fixed manual-only recipes such as Biomass by allowing non-resource ingredients like Leaves, Wood, Mycelia, and Alien Protein to be supplied through planner inputs.
- Fixed the solver debug panel contrast so diagnostic payloads remain readable against the dark planner theme.
- Restored the header logo width so the brand mark keeps its intended proportions instead of rendering squished.
- Restored unified ASP.NET shell parity for bare version-root routes such as `/1.2`, missing asset `404` handling, and empty `SOLVER_URL` fallback behavior.
