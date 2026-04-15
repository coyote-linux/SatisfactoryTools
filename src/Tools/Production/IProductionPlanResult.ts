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

export interface IProductionPlanResult
{
	details: IResultDetails;
	graph?: Graph;
	visualization?: IPlannerVisualization;
}

export interface IInternalPlannerCalculationResponse extends IProductionPlanResult
{
	visualization: IPlannerVisualization;
	debug?: IProductionDataApiDebug;
	error?: string;
}
