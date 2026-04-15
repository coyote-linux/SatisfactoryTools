# SatisfactoryTools
Satisfactory Tools for planning and building the perfect base.

## Requirements
- node.js 20+
- yarn 1.22 (`npx yarn@1.22.22 ...` also works if Yarn is not installed globally)
- .NET SDK 10

## Installation
- `git clone git@github.com:coyote-linux/SatisfactoryTools.git`
- `yarn install`
- `yarn build`
- Run `dotnet run --project SolverService/SatisfactoryTools.Solver.Api/SatisfactoryTools.Solver.Api.csproj`
- Open the reported ASP.NET URL (or set `--urls=http://0.0.0.0:8080` for a fixed local port)

The ASP.NET host now serves the existing `www/` shell and asset tree directly, including same-origin `/v2/solver` and `/v2/share/*` endpoints. The guarded internal planner route now has an explicit same-origin request gate, remains outside the public `/v2/*` compatibility surface, and is enabled by default through `useInternalPlannerCalculate`; set `Planner__UseInternalCalculate=false` to roll the ASP.NET host back to the legacy `/v2/solver` planner path, or `USE_INTERNAL_PLANNER_CALCULATE=false` if you still render the raw `www/index.php` shell template directly. If you deploy behind a reverse proxy or TLS terminator, make sure the ASP.NET host sees the public scheme and host so the same-origin access check continues to pass. If you need a non-default solver endpoint, set the `SOLVER_URL` environment variable before starting the ASP.NET host.

The current stack is verified in CI on Node 24 and was smoke-tested locally on Node 22.

## Contributing
Any pull requests are welcome, though some rules must be followed:
- try to follow current coding style (there's `tslint` and `.editorconfig`, those should help you with that)
- one PR per feature
- all PRs must target `dev` branch

## Development
Run `yarn start` to start the automated build process. It will watch over the code and rebuild it on change.

### Verification

- Frontend build: `yarn build`
- Core .NET suite: `dotnet test "SolverService/SatisfactoryTools.Solver.Api.Tests/SatisfactoryTools.Solver.Api.Tests.csproj"`
- Guarded browser regression suite:
  1. run `yarn build` so `www/assets/app.js` is fresh,
  2. run `dotnet build "SolverService/SatisfactoryTools.Solver.Api.Tests/SatisfactoryTools.Solver.Api.Tests.csproj" --artifacts-path /tmp/satisfactorytools-browser-test-artifacts`,
  3. install Playwright Chromium from the built test output with the generated Playwright script or `dotnet exec ... Microsoft.Playwright.dll install chromium`,
  4. run `dotnet test "SolverService/SatisfactoryTools.Solver.Api.Tests/SatisfactoryTools.Solver.Api.Tests.csproj" --filter "FullyQualifiedName~PlannerBrowserRegressionTests" --artifacts-path /tmp/satisfactorytools-browser-test-artifacts --logger "console;verbosity=minimal"`.

CircleCI currently enforces the frontend path with `yarn install` and `yarn buildCI`. The .NET and Playwright-based browser regression suites are available in-repo for local and review validation even though they are not yet wired into CircleCI.

For local same-origin planner testing, `docker compose up` now starts:

- a Node builder that produces `www/assets/app.js`,
- a unified ASP.NET host on `http://localhost:8080/` that serves the Angular shell, static assets, and same-origin `/v2/*` endpoints.

This means the browser can use the frontend's default same-origin `/v2/*` paths during local testing, including planner share creation/loading, without editing frontend TypeScript or requiring PHP/Apache at runtime.

Compose now inherits the unified host's guarded planner default-on behavior. If you want the compose-local host to stay on the legacy planner path instead, set `PLANNER_USE_INTERNAL_CALCULATE=false` before `docker compose up`.

The web container only waits for `www/assets/app.js` to exist before ASP.NET starts. If you want to guarantee that Compose is serving a freshly rebuilt frontend bundle rather than a previously generated local file, rerun `yarn build` first or remove `www/assets/app.js` before `docker compose up`.

## Updating data
Get the latest Docs.json from your game installation and place it into `data` folder.
Then run `yarn parseDocs`command and the `data.json` file would get updated automatically.
It will also generate `diff.txt` file in the same folder, marking differences between the two files in a player-readable format (useful for generating changelogs), as well as `imageMapping.json`, which will be useful if you want to update icons as well (see below).

## Updating icons
First you need to extract the images out of the game pack. You need `umodel` (UE Viewer) program. Run these commands (replacing paths where necessary):

```shell script
.\umodel.exe -path="C:\Program Files\Epic Games\SatisfactoryExperimental\FactoryGame\Content\Paks" -out=".\out256" -png -export *_256.uasset -game=ue4.22
.\umodel.exe -path="C:\Program Files\Epic Games\SatisfactoryExperimental\FactoryGame\Content\Paks" -out=".\out256" -png -export *_256_New.uasset -game=ue4.22
```

After the export is done, copy the resulting `out256` folder to `data/icons`. Then run `yarn generateImages`, which will automatically generate the images in correct sizes and places. `yarn parseDocs` has to be run before this command, if it wasn't run in the previous step.
