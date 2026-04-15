using System.Net;
using System.Globalization;
using SatisfactoryTools.Solver.Api.Contracts;

namespace SatisfactoryTools.Solver.Api.Services;

public sealed class PlannerResultVisualizationFactory
{
	private const string Transparent = "rgba(0, 0, 0, 0)";
	private const string LightText = "rgba(238, 238, 238, 1)";
	private const string EdgeColor = "rgba(105, 125, 145, 1)";
	private const string EdgeHighlightColor = "rgba(134, 151, 167, 1)";
	private const string PackagerClassName = "Desc_Packager_C";
	private const double IngredientScaleEpsilon = 1e-8d;

	public PlannerResultVisualization Create(PlannerResultGraph graph, GameDataDocument data)
	{
		return new PlannerResultVisualization {
			Nodes = graph.Nodes.Select(CreateNode).ToList(),
			Edges = graph.Edges.Select((edge) => CreateEdge(edge, data)).ToList(),
			ElkGraph = CreateElkGraph(graph),
		};
	}

	private static PlannerVisualizationNode CreateNode(PlannerGraphNode node)
	{
		return node switch
		{
			PlannerRecipeNode recipeNode => new PlannerVisualizationNode {
				Id = recipeNode.Id,
				Label = CreateRecipeLabel(recipeNode),
				Title = CreateRecipeTooltip(recipeNode),
				Color = CreateNodeColor("rgba(223, 105, 26, 1)", "rgba(231, 122, 49, 1)"),
				Font = CreateFont(),
			},
			PlannerMinerNode minerNode => new PlannerVisualizationNode {
				Id = minerNode.Id,
				Label = $"<b>{Encode(minerNode.Resource.Name)}</b>\n{PlannerResultMath.FormatNumber(minerNode.ItemAmount.Amount)} / min",
				Color = CreateNodeColor("rgba(78, 93, 108, 1)", "rgba(105, 125, 145, 1)"),
				Font = CreateFont(),
			},
			PlannerInputNode inputNode => new PlannerVisualizationNode {
				Id = inputNode.Id,
				Label = $"{FormatText($"Input: {Encode(inputNode.Resource.Name)}")}\n{PlannerResultMath.FormatNumber(inputNode.ItemAmount.Amount)} / min",
				Color = CreateNodeColor("rgba(175, 109, 14, 1)", "rgba(234, 146, 18, 1)"),
				Font = CreateFont(),
			},
			PlannerProductNode productNode => new PlannerVisualizationNode {
				Id = productNode.Id,
				Label = $"{FormatText(Encode(productNode.Resource.Name))}\n{PlannerResultMath.FormatNumber(productNode.ItemAmount.Amount)} / min",
				Color = CreateNodeColor("rgba(80, 160, 80, 1)", "rgba(111, 182, 111, 1)"),
				Font = CreateFont(),
			},
			PlannerByproductNode byproductNode => new PlannerVisualizationNode {
				Id = byproductNode.Id,
				Label = $"{FormatText($"Byproduct: {Encode(byproductNode.Resource.Name)}")}\n{PlannerResultMath.FormatNumber(byproductNode.ItemAmount.Amount)} / min",
				Color = CreateNodeColor("rgba(27, 112, 137, 1)", "rgba(38, 159, 194, 1)"),
				Font = CreateFont(),
			},
			PlannerSinkNode sinkNode => new PlannerVisualizationNode {
				Id = sinkNode.Id,
				Label = $"Sink: {FormatText(Encode(sinkNode.Resource.Name))}\n{PlannerResultMath.FormatNumber(sinkNode.ItemAmount.Amount)} / min\n{PlannerResultMath.FormatNumber(sinkNode.ItemAmount.Amount * sinkNode.Resource.SinkPoints)} points / min",
				Color = CreateNodeColor("rgba(217, 83, 79, 1)", "rgba(224, 117, 114, 1)"),
				Font = CreateFont(),
			},
			_ => throw new InvalidOperationException($"Unsupported planner graph node type '{node.GetType().Name}'."),
		};
	}

	private static PlannerVisualizationEdge CreateEdge(PlannerGraphEdge edge, GameDataDocument data)
	{
		return new PlannerVisualizationEdge {
			Id = edge.Id,
			From = edge.From.Id,
			To = edge.To.Id,
			Label = Encode(data.Items[edge.ItemAmount.Item].Name) + "\n" + PlannerResultMath.FormatNumber(edge.ItemAmount.Amount) + " / min",
			Color = new PlannerVisualizationEdgeColor {
				Color = EdgeColor,
				Highlight = EdgeHighlightColor,
			},
			Font = CreateFont(),
			Smooth = edge.To.HasOutputTo(edge.From)
				? new PlannerVisualizationSmooth {
					Enabled = true,
					Type = "curvedCW",
					Roundness = 0.2d,
				}
				: new PlannerVisualizationSmooth {
					Enabled = false,
				},
		};
	}

	private static PlannerElkGraph CreateElkGraph(PlannerResultGraph graph)
	{
		return new PlannerElkGraph {
			Id = "root",
			LayoutOptions = new PlannerElkLayoutOptions {
				Algorithm = "org.eclipse.elk.layered",
				FavorStraightEdges = true,
				NodeNodeSpacing = "40",
			},
			Children = graph.Nodes.Select((node) => new PlannerElkNode {
				Id = node.Id.ToString(CultureInfo.InvariantCulture),
				Width = 250,
				Height = 100,
			}).ToList(),
			Edges = graph.Edges.Select((edge) => new PlannerElkEdge {
				Id = string.Empty,
				Source = edge.From.Id.ToString(CultureInfo.InvariantCulture),
				Target = edge.To.Id.ToString(CultureInfo.InvariantCulture),
			}).ToList(),
		};
	}

	private static string CreateRecipeLabel(PlannerRecipeNode node)
	{
		var machineTitle = PlannerResultMath.FormatNumber(node.RecipeData.Amount) + "x " + Encode(node.RecipeData.Machine.Name);
		if (!HasRecipeCostAdjustment(node.RecipeData)) {
			return FormatText(Encode(node.RecipeData.Recipe.Name)) + "\n" + machineTitle;
		}

		return FormatText(Encode(node.RecipeData.Recipe.Name)) + "\n" + machineTitle + " · Cost x" + PlannerResultMath.FormatNumber(node.RecipeData.RecipeCostMultiplier);
	}

	private static string CreateRecipeTooltip(PlannerRecipeNode node)
	{
		var title = new List<string>();
		foreach (var machine in node.MachineData.Machines) {
			title.Add(machine.Amount + "x " + Encode(node.RecipeData.Machine.Name) + " at <b>" + FormatClockSpeed(machine.ClockSpeed) + "%</b> clock speed");
		}

		if (HasRecipeCostAdjustment(node.RecipeData)) {
			title.Add("Recipe cost multiplier: <b>x" + PlannerResultMath.FormatNumber(node.RecipeData.RecipeCostMultiplier) + "</b>");
			title.Add(string.Empty);
		}

		title.Add("Needed power: " + PlannerResultMath.FormatNumber(PlannerResultMath.Round(node.MachineData.Power.Average)) + " MW");
		title.Add(string.Empty);

		foreach (var ingredient in node.Ingredients) {
			title.Add("<b>IN:</b> " + PlannerResultMath.FormatNumber(ingredient.MaxAmount) + " / min - " + Encode(ingredient.Resource.Name));
		}

		foreach (var product in node.Products) {
			title.Add("<b>OUT:</b> " + PlannerResultMath.FormatNumber(product.MaxAmount) + " / min - " + Encode(product.Resource.Name));
		}

		return string.Join("<br>", title);
	}

	private static bool HasRecipeCostAdjustment(PlannerRecipeData recipeData)
	{
		if (recipeData.Machine.ClassName == PackagerClassName) {
			return false;
		}

		return Math.Abs(recipeData.RecipeCostMultiplier - 1d) > IngredientScaleEpsilon;
	}

	private static string FormatText(string text, bool bold = true)
	{
		var parts = text.Split(' ').ToList();
		if (parts.Count >= 4) {
			parts.Insert((int)Math.Ceiling(parts.Count / 2d), bold ? "</b>\n<b>" : "\n");
		}

		var joined = string.Join(" ", parts);
		return bold ? $"<b>{joined}</b>" : joined;
	}

	private static string FormatClockSpeed(double value)
	{
		return value.ToString("0.####", CultureInfo.InvariantCulture);
	}

	private static PlannerVisualizationNodeColor CreateNodeColor(string background, string highlightBackground)
	{
		return new PlannerVisualizationNodeColor {
			Border = Transparent,
			Background = background,
			Highlight = new PlannerVisualizationHighlightColor {
				Border = LightText,
				Background = highlightBackground,
			},
		};
	}

	private static PlannerVisualizationFont CreateFont()
	{
		return new PlannerVisualizationFont {
			Color = LightText,
		};
	}

	private static string Encode(string text)
	{
		return WebUtility.HtmlEncode(text);
	}
}
