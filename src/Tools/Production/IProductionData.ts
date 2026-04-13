export interface IProductionData
{

	metadata: IProductionDataMetadata;
	request: IProductionDataRequest;

}

export interface IProductionDataMetadata
{

	name: string|null;
	icon: string|null;
	schemaVersion: number;
	gameVersion: string;

}

export interface IProductionDataRequest
{

	resourceMax: {[key: string]: number}; // raw resource limit
	resourceWeight: {[key: string]: number}; // weighted values

	blockedResources: string[]; // whether the raw resource is available for usage or not
	blockedRecipes: string[]; // whether normal recipe can be used
	blockedMachines?: string[]; // which machines are blocked
	allowedAlternateRecipes: string[]; // whether alt is available or not (doesn't guarantee usage)
	recipeCostMultiplier?: number;
	powerConsumptionMultiplier?: number;

	sinkableResources: string[]; // whether or not you can sink a given resource

	production: IProductionDataRequestItem[];
	input: IProductionDataRequestInput[];

}

export interface IProductionDataApiRequest extends IProductionDataRequest
{

	gameVersion: string;
	debug?: boolean;

}

export interface IProductionDataRequestItem
{

	item: string|null; // classname of the item
	type: string; // Constants.PRODUCTION_TYPE
	amount: number; // amount when producing items/min
	ratio: number; // ratio when producing max

}

export interface IProductionDataRequestInput
{

	item: string|null; // classname of the item
	amount: number; // amount of items/min

}

export interface IProductionDataApiResponse
{

	[key: string]: number;

}

export interface IProductionDataApiResponseEnvelope
{

	result?: IProductionDataApiResponse;
	error?: string;
	debug?: IProductionDataApiDebug;

}

export interface IProductionDataApiDebug
{

	status: string;
	phase?: string;
	message: string;
	solverVersion?: string;
	variableCount?: number;
	constraintCount?: number;
	wallTimeMs?: number;
	iterations?: number;
	nodes?: number;
	items?: IProductionDataApiDebugItem[];

}

export interface IProductionDataApiDebugItem
{

	item: string;
	name: string;
	reachable: boolean;
	reasons: string[];

}
