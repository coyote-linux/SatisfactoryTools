# Application Architecture

## Overview

SatisfactoryTools is a browser-based AngularJS application bundled with webpack and served from the `www/` directory. The application is primarily a static frontend, but it is delivered through `www/index.php` so the server can provide cache-busted asset URLs and front-controller routing.

At runtime the application loads a single generated bundle, `www/assets/app.js`, then bootstraps the AngularJS module defined in `src/app.ts`. Route, component, directive, and dataset wiring is centralized in `src/Module/AppModule.ts`.

## Request and boot flow

1. A web server serves `www/index.php` as the entry document.
2. `www/index.php` sets `<base href="/">`, renders shell markup, and loads `/assets/app.js`.
3. webpack builds that bundle from `src/app.ts` into `www/assets/app.js`.
4. `src/app.ts` loads styles and third-party UI libraries, then creates the AngularJS `app` module.
5. `src/Module/AppModule.ts` registers routes, controllers, directives, services, filters, and components.
6. Route transitions select the active game dataset and render the matching controller/component templates.

## Key directories

- `src/` - TypeScript application source.
- `src/Module/` - AngularJS application wiring, controllers, services, directives, and components.
- `src/Data/` - runtime dataset selection and data access helpers.
- `src/Tools/Production/` - production planner request/result models and UI-side production logic.
- `src/Solver/` - remote solver client.
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
- Solving is not performed in this repository. `src/Solver/Solver.ts` posts planner requests to the remote API at `https://api.satisfactorytools.com/v2/solver`.
- Shared planner imports also rely on the remote API namespace (`/v2/share/...`).

For deployment, the frontend can run without a local solver service, but production-planner solving and share loading require outbound access to the hosted API.

## Build and asset pipeline

### Standard application build

- `yarn build` / `yarn buildCI` runs webpack in production mode.
- Output is written to `www/assets/app.js`.
- `yarn start` runs webpack in watch mode only; it does not serve HTTP traffic.

### Offline content generation

- `yarn parseDocs` reads `data/Docs.json` and regenerates `data/data.json`, `data/diff.txt`, and `data/imageMapping.json`.
- `yarn generateImages` depends on `yarn parseDocs` and writes generated item images into `www/assets/images/items`.
- `yarn parsePak` is an offline pak parser utility.

These scripts are content-maintenance tools, not part of the minimum runtime needed to serve the application.

## Deployment requirements

### Minimum runtime requirements

- Node.js 20+ for building the bundle.
- Yarn 1.22.x (or `npx yarn@1.22.22 ...`).
- A PHP-capable web server because the entry document is `www/index.php`.
- Apache rewrite support or equivalent front-controller fallback for HTML5 routes.

### Required server behavior

- The document root must be `www/`.
- The application expects to be served from the site root because `www/index.php` sets `<base href="/">` and uses absolute `/assets/...` URLs.
- Unknown non-file routes must fall back to `index.php`. The existing Apache rules live in `www/.htaccess`.
- Static assets under `www/assets/` must be served directly.

### Current verified environment

- CI builds on Node 24 (`.circleci/config.yml`).
- Local modern-stack smoke testing has been done on Node 22.

### External dependencies at runtime

- Remote solver API: `https://api.satisfactorytools.com/v2/solver`
- Remote share API: `https://api.satisfactorytools.com/v2/share/...`
- Matomo analytics script referenced from `www/index.php`

If the deployment target blocks outbound network access, the production planner will load but solver-backed features will not work.

## Recommended local testing model

For local testing, use one process to build the bundle and one PHP/Apache web server rooted at `www/`. The provided Docker Compose setup is intended for this exact workflow.

## Docker Compose testing workflow

The repository includes a simple `docker-compose.yml` for local testing.

- `builder` uses Node 24 and runs `yarn install` plus `yarn build` against the repository root.
- `web` uses the official `php:8.2-apache` image and serves `www/` directly from `/var/www/html`.
- Apache reads the existing `www/.htaccess`, so HTML5 routes continue to fall back to `index.php`.

Start the app with:

```bash
docker compose up
```

Then open `http://localhost:8080/`.

The web container waits for `www/assets/app.js` to exist before starting Apache, so a fresh clone can be started with one command.
