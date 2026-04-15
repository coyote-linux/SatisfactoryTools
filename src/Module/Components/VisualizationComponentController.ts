import {DataSet, Edge, Network} from 'vis-network';
import {IController, IScope, ITimeoutService} from 'angular';
import ELK from 'elkjs/lib/elk.bundled';
import cytoscape from 'cytoscape';
import {IVisNode} from '@src/Tools/Production/Result/IVisNode';
import {IVisEdge} from '@src/Tools/Production/Result/IVisEdge';
import {IElkGraph} from '@src/Solver/IElkGraph';
import {Strings} from '@src/Utils/Strings';
import model from '@src/Data/Model';
import {IProductionPlanResult} from '@src/Tools/Production/IProductionPlanResult';

export class VisualizationComponentController implements IController
{

	public result: IProductionPlanResult;

	public static $inject = ['$element', '$scope', '$timeout'];

	private unregisterWatcherCallback: () => void;
	private network: Network;
	private fitted: boolean = false;

	public constructor(private readonly $element: any, private readonly $scope: IScope, private readonly $timeout: ITimeoutService) {}


	public $onInit(): void
	{
		this.unregisterWatcherCallback = this.$scope.$watch(() => {
			return this.result;
		}, (newValue) => {
			this.updateData(newValue);
		});
	}

	public $onDestroy(): void
	{
		this.unregisterWatcherCallback();
	}

	public useCytoscape(result: IProductionPlanResult): void
	{
		if (!result.graph) {
			return;
		}

		const options: cytoscape.CytoscapeOptions = {
			container: this.$element[0],
		};
		options.layout = {
			name: 'elk',
			fit: true,
			padding: 200,
			nodeDimensionIncludeLabels: true,
			elk: {
				algorithm: 'layered',
				edgeRouting: 'POLYLINE',
				'spacing.nodeNode': 200,
			},
		} as any;

		const elements: cytoscape.ElementDefinition[] = [];
		for (const node of result.graph.nodes) {
			elements.push({
				data: {
					id: node.id.toString(),
					label: node.getTitle(),
				},
				position: {
					x: 1,
					y: 1,
				},
			});
		}

		for (const edge of result.graph.edges) {
			elements.push({
				data: {
					id: edge.id.toString(),
					source: edge.from.id.toString(),
					target: edge.to.id.toString(),
					label: edge.itemAmount.item,
				},
			});
		}

		options.elements = elements;
		options.style = [
			{
				selector: 'node[label]',
				style: {
					width: 'label',
					height: 'label',
					shape: 'round-rectangle',
					'font-size': '12px',
					label: 'data(label)',
					'text-valign': 'center',
					'text-halign': 'center',
				},
			},
			{
				selector: 'edge[label]',
				style: {
					label: 'data(label)',
					width: 3,
					'curve-style': 'segments',
				},
			},
		];

		const cy = cytoscape(options as any);
	}

	public useVis(result: IProductionPlanResult): void
	{
		const nodes = new DataSet<IVisNode>();
		const edges = new DataSet<IVisEdge>();
		let elkGraph: IElkGraph;

		if (result.visualization) {
			for (const node of result.visualization.nodes) {
				nodes.add(node);
			}

			for (const edge of result.visualization.edges) {
				edges.add(edge);
			}

			elkGraph = result.visualization.elkGraph;
		} else if (result.graph) {
			for (const node of result.graph.nodes) {
				nodes.add(node.getVisNode());
			}

			for (const edge of result.graph.edges) {
				const smooth: NonNullable<IVisEdge['smooth']> = {
					enabled: false,
				};

				if (edge.to.hasOutputTo(edge.from)) {
					smooth.enabled = true;
					smooth.type = 'curvedCW';
					smooth.roundness = 0.2;
				}

				edges.add({
					id: edge.id,
					from: edge.from.id,
					to: edge.to.id,
					label: model.getItem(edge.itemAmount.item).prototype.name + '\n' + Strings.formatNumber(edge.itemAmount.amount) + ' / min',
					color: {
						color: 'rgba(105, 125, 145, 1)',
						highlight: 'rgba(134, 151, 167, 1)',
					},
					font: {
						color: 'rgba(238, 238, 238, 1)',
					},
					smooth: smooth,
				});
			}

			elkGraph = {
				id: 'root',
				layoutOptions: {
					'elk.algorithm': 'org.eclipse.elk.layered',
					'org.eclipse.elk.layered.nodePlacement.favorStraightEdges': true as unknown as string,
					'org.eclipse.elk.spacing.nodeNode': 40 + '',
				},
				children: [],
				edges: [],
			};

			nodes.forEach((node) => {
				elkGraph.children.push({
					id: node.id.toString(),
					width: 250,
					height: 100,
				});
			});
			edges.forEach((edge) => {
				elkGraph.edges.push({
					id: '',
					source: edge.from.toString(),
					target: edge.to.toString(),
				});
			});
		} else {
			return;
		}

		this.network = this.drawVisualisation(nodes, edges);

		this.$timeout(0).then(() => {
			this.$timeout(0).then(() => {
				const elk = new ELK();
				elk.layout(elkGraph).then((data) => {
					nodes.forEach((node) => {
						const id = node.id;
						if (data.children) {
							for (const item of data.children) {
								if (parseInt(item.id, 10) === id) {
									nodes.update({
										id: id,
										x: item.x,
										y: item.y,
									});
									return;
								}
							}
						}
					});

					if (!this.fitted) {
						this.fitted = true;
						this.network.fit();
					}
				});
			});
		});
	}

	public updateData(result: IProductionPlanResult|undefined): void
	{
		if (!result) {
			return;
		}

		this.fitted = false;
		this.useVis(result);
	}

	private drawVisualisation(nodes: DataSet<IVisNode>, edges: DataSet<IVisEdge>): Network
	{
		const visEdges = new DataSet<Edge>();
		visEdges.add(edges.get().map((edge) => {
			if (edge.smooth?.enabled) {
				return {
					...edge,
					smooth: {
						enabled: true,
						type: edge.smooth.type || 'curvedCW',
						roundness: edge.smooth.roundness ?? 0.2,
					},
				};
			}

			return {
				...edge,
				smooth: false,
			};
		}));

		return new Network(this.$element[0], {
			nodes: nodes,
			edges: visEdges,
		}, {
			edges: {
				labelHighlightBold: false,
				font: {
					size: 14,
					multi: 'html',
					strokeColor: 'rgba(0, 0, 0, 0.2)',
				},
				arrows: 'to',
				smooth: false,
			},
			nodes: {
				labelHighlightBold: false,
				font: {
					// align: 'left',
					size: 14,
					multi: 'html',
				},
				margin: {
					top: 10,
					left: 10,
					right: 10,
					bottom: 10,
				},
				shape: 'box',
				widthConstraint: {
					minimum: 50,
					maximum: 250,
				},
				// widthConstraint: 225,
			},
			physics: {
				enabled: false,
			},
			layout: {
				improvedLayout: false,
				hierarchical: false,
			},
			interaction: {
				tooltipDelay: 0,
			},
		});
	}

}
