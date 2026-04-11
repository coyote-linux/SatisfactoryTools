import '@src/../styles/bootstrap.scss';
import '@src/../node_modules/ui-select/dist/select.css';
import '@src/../styles/style.scss';

import 'jquery/dist/jquery.min';
import 'jquery-ui-dist/jquery-ui.min';
import 'popper.js/dist/umd/popper.min';
import 'bootstrap/dist/js/bootstrap.min';
import * as angular from 'angular';
import 'angular-sanitize/angular-sanitize.min';
import 'angular-animate/angular-animate.min';
import 'angular-ui-router/release/angular-ui-router.min';
import 'ui-bootstrap4/dist/ui-bootstrap';
import 'ui-bootstrap4/dist/ui-bootstrap-tpls';
import 'ui-select/dist/select.min';
import 'angular-ui-sortable/dist/sortable.min';
import 'angular-breadcrumb/dist/angular-breadcrumb.min';

import cytoscape from 'cytoscape';
import nodeHtmlLabel from 'cytoscape-node-html-label';
//import cytoscapeElk from 'cytoscape-elk';
import {AppModule} from '@src/Module/AppModule';

nodeHtmlLabel(cytoscape as any);
//cytoscape.use(cytoscapeElk as any);

new AppModule(angular.module('app', ['ui.sortable', 'ui.select', 'ui.router', 'ui.bootstrap', 'ngSanitize', 'ngAnimate', 'ncy-angular-breadcrumb'])).register();
