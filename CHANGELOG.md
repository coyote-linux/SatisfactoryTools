# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased]

### Added
- Added `docs/architecture.md` to document the application boot flow, deployment model, and runtime requirements.
- Added `docker-compose.yml` with a Node 24 builder container and a `php:8.2-apache` web container for local testing.
- Added this changelog to track the ongoing stack-modernization work and the remaining path to Satisfactory v1.2 support.

### Changed
- Modernized the frontend build pipeline for current Node versions by replacing legacy webpack loader usage with built-in HTML asset handling and standard module imports.
- Updated the webpack Sass pipeline to use the modern Sass API with explicit load paths for package resolution.
- Updated project metadata and setup guidance to declare the modern Node/Yarn workflow used by CI and local builds.
- Expanded repository documentation to cover architecture, deployment behavior, and the local Docker-based testing flow.

### Removed
- Removed `script-loader` and `angular-templatecache-loader` from the build dependency chain.

### Fixed
- Aligned lockfiles after the dependency cleanup so the repo no longer references removed legacy webpack loaders.
- Documented the current runtime version boundary: the planner boot flow still recognizes `0.8`, `1.0`, and `1.0-ficsmas`, so Satisfactory v1.2 planner support remains pending.
