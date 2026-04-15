import {IElkGraph} from '@src/Solver/IElkGraph';
import {IProductionDataApiDebug} from '@src/Tools/Production/IProductionData';
import {Graph} from '@src/Tools/Production/Result/Graph';
import {IResultDetails} from '@src/Tools/Production/Result/IResultDetails';
import {IVisEdge} from '@src/Tools/Production/Result/IVisEdge';
import {IVisNode} from '@src/Tools/Production/Result/IVisNode';

export interface IPlannerVisualization
{
	nodes: IVisNode[];
	edges: IVisEdge[];
	elkGraph: IElkGraph;
}

interface IProductionPlanResultBase
{
	details: IResultDetails;
}

export interface ILegacyProductionPlanResult extends IProductionPlanResultBase
{
	graph: Graph;
	visualization?: undefined;
}

export interface IGuardedProductionPlanResult extends IProductionPlanResultBase
{
	graph?: undefined;
	visualization: IPlannerVisualization;
}

export type IProductionPlanResult = ILegacyProductionPlanResult | IGuardedProductionPlanResult;

export interface IInternalPlannerCalculationResponse extends IGuardedProductionPlanResult
{
	debug?: IProductionDataApiDebug;
	error?: string;
}
