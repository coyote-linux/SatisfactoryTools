<!DOCTYPE html>
<html lang="en" ng-app="app">
<head>
	<meta charset="utf-8">
	<meta name="viewport" content="width=device-width, initial-scale=1, shrink-to-fit=no">
	<title ng-bind="$root.versionLabel ? '[' + $root.versionLabel + '] Satisfactory Tools' : 'Satisfactory Tools'">Satisfactory Tools</title>

	<link rel="apple-touch-icon" sizes="180x180" href="/assets/images/icons/apple-touch-icon.png">
	<link rel="icon" type="image/png" sizes="32x32" href="/assets/images/icons/favicon-32x32.png">
	<link rel="icon" type="image/png" sizes="16x16" href="/assets/images/icons/favicon-16x16.png">
	<link rel="manifest" href="/assets/images/icons/site.webmanifest">
	<link rel="mask-icon" href="/assets/images/icons/safari-pinned-tab.svg" color="#da532c">
	<link rel="shortcut icon" href="/assets/images/icons/favicon.ico">
	<meta name="msapplication-TileColor" content="#da532c">
	<meta name="msapplication-config" content="/assets/images/icons/browserconfig.xml">
	<!-- Primary Meta Tags -->
	<meta name="theme-color" content="#ff8c00">
	<meta name="title" content="Satisfactory Tools" />
	<meta name="type" content="website" />
	<meta name="image" content="https://ficsit.spugnort.com/assets/images/icons/android-chrome-512x512.png" />
	<meta name="description" content="A modified fork of Satisfactory Tools for planning and building the perfect base. Calculate your production or consumption, browse items, buildings, and schematics, and track updates from the public fork at ficsit.spugnort.com." />
	<!-- Open Graph / Facebook -->
	<meta property="og:type" content="website">
	<meta property="og:url" content="https://ficsit.spugnort.com/">
	<meta property="og:title" content="Satisfactory Tools">
	<meta property="og:description" content="A modified fork of Satisfactory Tools for planning and building the perfect base. Calculate your production or consumption, browse items, buildings, and schematics, and track updates from the public fork at ficsit.spugnort.com.">
	<meta property="og:image" content="https://ficsit.spugnort.com/assets/images/icons/android-chrome-512x512.png">
	<!-- Twitter -->
	<meta property="twitter:card" content="summary">
	<meta property="twitter:url" content="https://ficsit.spugnort.com/">
	<meta property="twitter:title" content="Satisfactory Tools">
	<meta property="twitter:description" content="A modified fork of Satisfactory Tools for planning and building the perfect base. Calculate your production or consumption, browse items, buildings, and schematics, and track updates from the public fork at ficsit.spugnort.com.">
	<meta property="twitter:image" content="https://ficsit.spugnort.com/assets/images/icons/android-chrome-512x512.png">

	<style>
		[ng\:cloak], [ng-cloak], [data-ng-cloak], [x-ng-cloak], .ng-cloak, .x-ng-cloak {
			display: none !important;
		}

		/* for faster loading */
		body {
			margin: 0;
			font-family: system-ui, -apple-system, sans-serif;
			font-size: 1rem;
			font-weight: 400;
			line-height: 1.5;
			color: #e0e0e0;
			text-align: left;
			background-color: #0d0d0d;
		}

		.fullscreen-loader {
			position: fixed;
			top: 0;
			left: 0;
			right: 0;
			bottom: 0;
			display: flex;
			background-color: #0d0d0d;

			align-items: center;
			justify-content: center;
			flex-direction: column;
			z-index: 10000;
		}

		@keyframes loading {
			0% {
				opacity: 1;
				font-size: 1em;
			}
			99% {
				opacity: 0;
				font-size: 30em;
			}
			100% {
				z-index: -10;
				opacity: 0;
				font-size: 20em;
			}
		}

		.fullscreen-loader.hidden {
			animation: loading 0.7s ease-in-out;
			animation-fill-mode: forwards;
		}

		.fullscreen-loader .logo {
			margin-bottom: 20px;
		}

		.fullscreen-loader .loader {
			font-size: 50px;
			color: #ff8c00;
		}

		.fullscreen-loader .loader-text, .fullscreen-loader .loader-text span {
			margin-top: 20px;
			font-size: 32px;
			color: #ff8c00;
		}
	</style>
	<link rel="stylesheet" href="/assets/css/fontawesome.min.css">
	<base href="/">
</head>
<body ng-class="{'april-ui': $root.aprilMode}">

<div id="toasts" class="toasts"></div>

<app></app>

<div class="fullscreen-loader" ng-class="{hidden: true}">
	<div class="logo">
		<img src="/assets/images/logo/satisfactorySmall.png" height="60">
		<img src="/assets/images/logo/tools.png" height="60">
	</div>
	<div>
		<span class="loader fas fa-spin fa-sync-alt"></span>
	</div>
	<div class="loader-text">
		<span ng-bind="'Entering'">Loading</span> the A.W.E.S.O.M.E.!
	</div>
</div>

	<script>
		window.SATISFACTORY_TOOLS_CONFIG = {
			solverUrl: <?= json_encode(getenv('SOLVER_URL') ?: '/v2/solver') ?>,
			useInternalPlannerCalculate: <?= json_encode(filter_var(getenv('USE_INTERNAL_PLANNER_CALCULATE') ?: false, FILTER_VALIDATE_BOOLEAN)) ?>,
			internalPlannerCalculateUrl: <?= json_encode(getenv('INTERNAL_PLANNER_CALCULATE_URL') ?: '/_internal/planner/calculate') ?>
		};
	</script>
	<script src="/assets/app.js?v=<?= filemtime(__DIR__ . '/assets/app.js') ?>" async></script>
</body>
</html>
