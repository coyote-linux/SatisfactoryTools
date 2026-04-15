import axios from 'axios';
import {IProductionData, IProductionDataApiDebug} from '@src/Tools/Production/IProductionData';
import {IGuardedProductionPlanResult, IInternalPlannerCalculationResponse} from '@src/Tools/Production/IProductionPlanResult';

export class PlannerCalculationClient
{

	public static calculate(plannerState: IProductionData, showDebugOutput: boolean, callback: (response: IPlannerCalculationClientResponse) => void): void
	{
		axios({
			method: 'post',
			url: '/_internal/planner/calculate',
			params: {
				showDebugOutput: showDebugOutput,
			},
			data: plannerState,
		}).then((response) => {
			const payload = response.data as IInternalPlannerCalculationResponse;
			callback({
				result: PlannerCalculationClient.toGuardedPlanResult(payload),
				debug: payload.debug,
				error: payload.error,
			});
		}).catch((error) => {
			const payload = error.response?.data as {error?: string, debug?: IProductionDataApiDebug}|undefined;
			callback({
				debug: payload?.debug,
				error: payload?.error || error.message || 'Unable to calculate the production plan.',
			});
		});
	}

	private static toGuardedPlanResult(payload: IInternalPlannerCalculationResponse): IGuardedProductionPlanResult
	{
		return {
			details: payload.details,
			visualization: payload.visualization,
		};
	}

}

export interface IPlannerCalculationClientResponse
{

	result?: IGuardedProductionPlanResult;
	debug?: IProductionDataApiDebug;
	error?: string;

}
