import axios from 'axios';
import {IProductionToolResponse} from '@src/Tools/Production/IProductionToolResponse';
import {IProductionDataApiDebug, IProductionDataApiRequest, IProductionDataApiResponseEnvelope} from '@src/Tools/Production/IProductionData';

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

	public static solveProduction(productionRequest: IProductionDataApiRequest, callback: (response: ISolverProductionResponse) => void): void
	{
		axios({
			method: 'post',
			url: window.SATISFACTORY_TOOLS_CONFIG?.solverUrl || '/v2/solver',
			data: productionRequest,
		}).then((response) => {
			const payload = response.data as IProductionDataApiResponseEnvelope;
			callback({
				result: payload.result || {},
				debug: payload.debug,
				error: payload.error,
			});
		}).catch((error) => {
			const payload = error.response?.data as IProductionDataApiResponseEnvelope | undefined;
			callback({
				result: {},
				debug: payload?.debug,
				error: payload?.error || error.message || 'Unable to contact the solver.',
			});
		});
	}

}

export interface ISolverProductionResponse
{

	result: IProductionToolResponse;
	debug?: IProductionDataApiDebug;
	error?: string;

}
