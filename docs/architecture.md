# Application Architecture

## Overview

SatisfactoryTools is a browser-based AngularJS application bundled with webpack and served from the `www/` directory. The application is primarily a static frontend, and the ASP.NET Core host now uses `www/index.php` as the shell template source so it can preserve the existing cache-busted asset URL and runtime-config behavior without requiring PHP at runtime.

At runtime the application loads a single generated bundle, `www/assets/app.js`, then bootstraps the AngularJS module defined in `src/app.ts`. Route, component, directive, and dataset wiring is centralized in `src/Module/AppModule.ts`.

## Request and boot flow

1. The ASP.NET Core host resolves the active frontend root and renders `www/index.php` as the entry document template.
2. That shell keeps `<base href="/">`, renders the current markup, injects `window.SATISFACTORY_TOOLS_CONFIG`, and loads `/assets/app.js`.
3. webpack builds that bundle from `src/app.ts` into `www/assets/app.js`.
4. `src/app.ts` loads styles and third-party UI libraries, then creates the AngularJS `app` module.
5. `src/Module/AppModule.ts` registers routes, controllers, directives, services, filters, and components.
6. Route transitions select the active game dataset and render the matching controller/component templates.

## Key directories

- `src/` - TypeScript application source.
- `src/Module/` - AngularJS application wiring, controllers, services, directives, and components.
- `src/Data/` - runtime dataset selection and data access helpers.
- `src/Tools/Production/` - production planner request/result models and UI-side production logic.
- `src/Solver/` - frontend solver client and browser-side solver URL selection.
- `SolverService/` - local C# replacement solver service and tests.
- `templates/` - HTML templates loaded directly into the bundle.
- `styles/` - Sass and CSS entry styles.
- `data/` - generated runtime datasets and parsing inputs.
- `bin/` - offline data-processing scripts (`parseDocs`, `parsePak`, `generateImages`).
- `www/` - deployable web root.

## Frontend architecture

### Bundle entry

- `src/app.ts` is the webpack entrypoint.
- It loads the CSS/Sass bundles, AngularJS, and the legacy UI dependencies used by the app.
- It creates the root AngularJS module with dependencies such as `ui.router`, `ui.bootstrap`, `ui.select`, `ngSanitize`, and `ngAnimate`.

### Application wiring

- `src/Module/AppModule.ts` is the central runtime wiring file.
- It enables HTML5 routing through `$locationProvider.html5Mode(...)`.
- It registers ui-router states for the home page, codex views, item/building/schematic detail pages, and the production planner.
- It also registers reusable directives and components such as breadcrumbs, filters, visualization, and scroll behavior.

### Templates

- Templates live under `templates/`.
- They are bundled as source strings and referenced from TypeScript using `require('@templates/...')`.
- Route templates, directive templates, and component templates are all resolved at build time.

## Dataset and version handling

The app supports multiple Satisfactory data versions.

- `src/Module/AppModule.ts` inspects the URL path and determines which game version is active.
- `src/Data/DataProvider.ts` switches the runtime dataset between:
  - `data/data.json`
  - `data/data1.0.json`
  - `data/data1.0-ficsmas.json`

These JSON files are imported into the webpack bundle at build time, so a normal deployment does not need to serve them separately from the web root.

## Production planner architecture

- The planner entry controller is `src/Module/Controllers/ProductionController.ts`.
- Planner state and transformations flow through `src/Tools/Production/*`.
- `src/Solver/Solver.ts` is the browser-side client that reads the configured solver URL and posts planner requests to `/v2/solver`.
- `SolverService/SatisfactoryTools.Solver.Api` is the new local replacement solver service. It uses OR-Tools and consumes the same `data/data1.0*.json` assets as the frontend.
- Shared planner imports also rely on the remote API namespace (`/v2/share/...`).

The planner currently expects both solving and sharing to live under the same-origin `/v2/*` namespace.

- `src/Solver/Solver.ts` defaults browser solve requests to `/v2/solver` unless `www/index.php` injects an override.
- `ProductionTab.ts` posts share creates to `/v2/share/?version=...`.
- `ProductionController.ts` loads shared plans from `/v2/share/{shareId}`.

For deployment, the frontend can run without the local solver service only if the served origin still exposes compatible `/v2/solver` and `/v2/share/...` endpoints.

## Build and asset pipeline

### Standard application build

- `yarn build` / `yarn buildCI` runs webpack in production mode.
- Output is written to `www/assets/app.js`.
- `yarn start` runs webpack in watch mode only; it does not serve HTTP traffic.
- `dotnet test SolverService/SatisfactoryTools.Solver.Api.Tests/SatisfactoryTools.Solver.Api.Tests.csproj` validates the local C# solver contract and sample solve behavior.
- `dotnet run --project SolverService/SatisfactoryTools.Solver.Api/SatisfactoryTools.Solver.Api.csproj --urls=http://0.0.0.0:8080` starts the unified local host for both the shell and `/v2/*`.

### Offline content generation

- `yarn parseDocs` reads `data/Docs.json` and regenerates `data/data.json`, `data/diff.txt`, and `data/imageMapping.json`.
- `yarn generateImages` depends on `yarn parseDocs` and writes generated item images into `www/assets/images/items`.
- `yarn parsePak` is an offline pak parser utility.

These scripts are content-maintenance tools, not part of the minimum runtime needed to serve the application.

## Deployment requirements

### Minimum runtime requirements

- Node.js 20+ for building the bundle.
- Yarn 1.22.x (or `npx yarn@1.22.22 ...`).
- .NET SDK 10 for the unified host and replacement solver service.

### Required server behavior

- The document root must be `www/`.
- The application expects to be served from the site root because `www/index.php` sets `<base href="/">` and uses absolute `/assets/...` URLs.
- Unknown non-file routes must fall back to the rendered shell HTML while `/v2/*` keeps API ownership.
- Static assets under `www/assets/` must be served directly.

### Current verified environment

- CI builds on Node 24 (`.circleci/config.yml`).
- Local modern-stack smoke testing has been done on Node 22.

### External dependencies at runtime

- Public deployments should expose same-origin `/v2/solver` and `/v2/share/...` directly from the ASP.NET host.
- The host preserves `www/index.php` runtime config injection semantics, including the `SOLVER_URL` override and the default same-origin `/v2/*` model.
- The guarded planner path now defaults to same-origin `/_internal/planner/calculate`; explicit rollback remains available through `Planner:UseInternalCalculate=false` on the ASP.NET host or `USE_INTERNAL_PLANNER_CALCULATE=false` if the raw PHP shell template is still rendered directly.

For deployments behind a reverse proxy or TLS terminator, the ASP.NET host must receive the public scheme and host values for planner requests. The internal planner access policy compares the browser `Origin` header against `request.Scheme` plus `request.Host`, so mismatched forwarded values will cause same-origin planner requests to fail.

In local development, the recommended path is to let the ASP.NET host serve both the shell and the compatibility endpoints.

If the deployment target blocks outbound network access, both solving and sharing can still work as long as `/v2/solver` and `/v2/share/...` are handled locally.

## Recommended local testing model

For local testing, use one process to build the bundle and one ASP.NET host process. The provided Docker Compose setup is intended for this exact workflow.

## Docker Compose testing workflow

The repository includes a simple `docker-compose.yml` for local testing.

- `builder` uses Node 24 and runs `yarn install` plus `yarn build` against the repository root.
- `web` uses the .NET 10 SDK image and runs the unified ASP.NET Core host from `SolverService/`.
- `web` serves the existing `www/` asset tree, renders the `www/index.php` shell template, and owns same-origin `/v2/*`.
- `web` stores compose-local share payloads under `/tmp/satisfactorytools-share-store` so share creation/loading works during a compose session.
- `web` now inherits the guarded planner default-on behavior unless `PLANNER_USE_INTERNAL_CALCULATE=false` is supplied to Compose as an explicit rollback override.
- `web` waits for `www/assets/app.js` before starting so the Angular shell has its generated bundle available.

Start the app with:

```bash
docker compose up
```

Then open `http://localhost:8080/`.

The web container waits for `www/assets/app.js` to exist before starting ASP.NET, so a fresh clone can be started with one command.
