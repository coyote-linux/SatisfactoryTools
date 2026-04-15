export interface IVisEdge
{
	from: number;
	to: number;
	label?: string;
	title?: string;
	arrows?: string;
	id?: number;
	smooth?: {
		enabled: boolean;
		type?: string;
		roundness?: number;
	};
	color?: {
		color: string,
		highlight: string,
	};
	font?: {
		color: string,
	};
}
