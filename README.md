# SatisfactoryTools
Satisfactory Tools for planning and building the perfect base.

## Requirements
- node.js 20+
- yarn 1.22 (`npx yarn@1.22.22 ...` also works if Yarn is not installed globally)
- PHP 7.1+

## Installation
- `git clone git@github.com:coyote-linux/SatisfactoryTools.git`
- `yarn install`
- `yarn build`
- Set up a virtual host pointing to `/www` directory (using e.g. Apache or ngnix)
- Proxy same-origin `/v2/solver` requests to the local ASP.NET solver service, or inject `SOLVER_URL` in `www/index.php` to point at your deployed solver endpoint.
- Proxy same-origin `/v2/share` requests to the local ASP.NET service as well, or expose that endpoint directly from the same origin.

The current stack is verified in CI on Node 24 and was smoke-tested locally on Node 22.

## Contributing
Any pull requests are welcome, though some rules must be followed:
- try to follow current coding style (there's `tslint` and `.editorconfig`, those should help you with that)
- one PR per feature
- all PRs must target `dev` branch

## Development
Run `yarn start` to start the automated build process. It will watch over the code and rebuild it on change.

For local same-origin planner testing, `docker compose up` now starts:

- a Node builder that produces `www/assets/app.js`,
- the local ASP.NET solver/share service on the internal `solver:8080`, and
- a PHP/Apache web container on `http://localhost:8080/` that reverse-proxies same-origin `/v2/solver` and `/v2/share/*` to that solver container.

This means the browser can use the frontend's default same-origin `/v2/*` paths during local testing, including planner share creation/loading, without editing `www/index.php`.

The web container only waits for `www/assets/app.js` to exist before Apache starts. If you want to guarantee that Compose is serving a freshly rebuilt frontend bundle rather than a previously generated local file, rerun `yarn build` first or remove `www/assets/app.js` before `docker compose up`.

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
