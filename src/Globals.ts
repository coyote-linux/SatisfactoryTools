import angular from 'angular';
import jQuery from 'jquery';

declare global
{
	interface Window {
		angular: typeof angular;
		jQuery: typeof jQuery;
		$: typeof jQuery;
		SATISFACTORY_TOOLS_CONFIG?: {
			solverUrl?: string;
			useInternalPlannerCalculate?: boolean;
			internalPlannerCalculateUrl?: string;
		};
	}
}

window.angular = angular;
window.jQuery = jQuery;
window.$ = jQuery;

export {angular};
