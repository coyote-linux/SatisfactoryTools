import angular from 'angular';
import jQuery from 'jquery';

declare global
{
	interface Window {
		angular: typeof angular;
		jQuery: typeof jQuery;
		$: typeof jQuery;
	}
}

window.angular = angular;
window.jQuery = jQuery;
window.$ = jQuery;

export {angular};
