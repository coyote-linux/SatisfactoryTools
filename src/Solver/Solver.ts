import axios from 'axios';
import {IProductionToolResponse} from '@src/Tools/Production/IProductionToolResponse';
import {IProductionDataApiRequest} from '@src/Tools/Production/IProductionData';

declare global
{
	interface Window {
		SATISFACTORY_TOOLS_CONFIG?: {
			solverUrl?: string;
		};
	}
}

export class Solver
{

	public static solveProduction(productionRequest: IProductionDataApiRequest, callback: (response: IProductionToolResponse) => void): void
	{
		axios({
			method: 'post',
			url: window.SATISFACTORY_TOOLS_CONFIG?.solverUrl || '/v2/solver',
			data: productionRequest,
		}).then((response) => {
			if ('result' in response.data) {
				callback(response.data.result);
			}
		}).catch(() => {
			callback({});
		});
	}

}
