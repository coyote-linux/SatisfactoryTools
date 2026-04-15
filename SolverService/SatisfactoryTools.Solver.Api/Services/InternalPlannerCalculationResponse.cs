namespace SatisfactoryTools.Solver.Api.Services;

internal sealed class InternalPlannerCalculationResponse
{
	public required InternalPlannerGraphResponse Graph { get; init; }
	public required PlannerResultDetails Details { get; init; }
	public required PlannerResultVisualization Visualization { get; init; }

	public static InternalPlannerCalculationResponse FromOutcome(PlannerSolveCompositionOutcome outcome)
	{
		return new InternalPlannerCalculationResponse
		{
			Graph = InternalPlannerGraphResponse.FromGraph(outcome.ResultDomain.Graph),
			Details = outcome.ResultDomain.Details,
			Visualization = outcome.ResultDomain.Visualization,
		};
	}
}

internal sealed class InternalPlannerGraphResponse
{
	public List<InternalPlannerGraphNodeResponse> Nodes { get; init; } = [];
	public List<InternalPlannerGraphEdgeResponse> Edges { get; init; } = [];

	public static InternalPlannerGraphResponse FromGraph(PlannerResultGraph graph)
	{
		return new InternalPlannerGraphResponse
		{
			Nodes = graph.Nodes.Select(InternalPlannerGraphNodeResponse.FromNode).ToList(),
			Edges = graph.Edges.Select(InternalPlannerGraphEdgeResponse.FromEdge).ToList(),
		};
	}
}

internal sealed class InternalPlannerGraphNodeResponse
{
	public required int Id { get; init; }
	public required string Kind { get; init; }
	public string? Item { get; init; }
	public double? Amount { get; init; }
	public string? Recipe { get; init; }
	public string? Machine { get; init; }
	public double? RecipeAmount { get; init; }
	public int? ClockSpeed { get; init; }
	public double? PowerConsumptionMultiplier { get; init; }
	public double? RecipeCostMultiplier { get; init; }
	public List<InternalPlannerResourceAmountResponse> Inputs { get; init; } = [];
	public List<InternalPlannerResourceAmountResponse> Outputs { get; init; } = [];

	public static InternalPlannerGraphNodeResponse FromNode(PlannerGraphNode node)
	{
		return node switch
		{
			PlannerRecipeNode recipeNode => new InternalPlannerGraphNodeResponse
			{
				Id = recipeNode.Id,
				Kind = "recipe",
				Recipe = recipeNode.RecipeData.Recipe.ClassName,
				Machine = recipeNode.RecipeData.Machine.ClassName,
				RecipeAmount = recipeNode.RecipeData.Amount,
				ClockSpeed = recipeNode.RecipeData.ClockSpeed,
				PowerConsumptionMultiplier = recipeNode.RecipeData.PowerConsumptionMultiplier,
				RecipeCostMultiplier = recipeNode.RecipeData.RecipeCostMultiplier,
				Inputs = recipeNode.Ingredients.Select(InternalPlannerResourceAmountResponse.FromResourceAmount).ToList(),
				Outputs = recipeNode.Products.Select(InternalPlannerResourceAmountResponse.FromResourceAmount).ToList(),
			},
			PlannerMinerNode minerNode => FromItemNode(minerNode.Id, "miner", minerNode.ItemAmount, outputs: minerNode.Outputs),
			PlannerInputNode inputNode => FromItemNode(inputNode.Id, "input", inputNode.ItemAmount, outputs: inputNode.Outputs),
			PlannerProductNode productNode => FromItemNode(productNode.Id, "product", productNode.ItemAmount, inputs: productNode.Inputs),
			PlannerByproductNode byproductNode => FromItemNode(byproductNode.Id, "byproduct", byproductNode.ItemAmount, inputs: byproductNode.Inputs),
			PlannerSinkNode sinkNode => FromItemNode(sinkNode.Id, "sink", sinkNode.ItemAmount, inputs: sinkNode.Inputs),
			_ => throw new InvalidOperationException($"Unsupported planner graph node type '{node.GetType().Name}'."),
		};
	}

	private static InternalPlannerGraphNodeResponse FromItemNode(
		int id,
		string kind,
		PlannerItemAmount itemAmount,
		IEnumerable<PlannerResourceAmount>? inputs = null,
		IEnumerable<PlannerResourceAmount>? outputs = null)
	{
		return new InternalPlannerGraphNodeResponse
		{
			Id = id,
			Kind = kind,
			Item = itemAmount.Item,
			Amount = itemAmount.Amount,
			Inputs = inputs?.Select(InternalPlannerResourceAmountResponse.FromResourceAmount).ToList() ?? [],
			Outputs = outputs?.Select(InternalPlannerResourceAmountResponse.FromResourceAmount).ToList() ?? [],
		};
	}
}

internal sealed class InternalPlannerGraphEdgeResponse
{
	public required int Id { get; init; }
	public required int From { get; init; }
	public required int To { get; init; }
	public required string Item { get; init; }
	public required double Amount { get; init; }

	public static InternalPlannerGraphEdgeResponse FromEdge(PlannerGraphEdge edge)
	{
		return new InternalPlannerGraphEdgeResponse
		{
			Id = edge.Id,
			From = edge.From.Id,
			To = edge.To.Id,
			Item = edge.ItemAmount.Item,
			Amount = edge.ItemAmount.Amount,
		};
	}
}

internal sealed class InternalPlannerResourceAmountResponse
{
	public required string Item { get; init; }
	public required double MaxAmount { get; init; }
	public required double Amount { get; init; }

	public static InternalPlannerResourceAmountResponse FromResourceAmount(PlannerResourceAmount resourceAmount)
	{
		return new InternalPlannerResourceAmountResponse
		{
			Item = resourceAmount.Resource.ClassName,
			MaxAmount = resourceAmount.MaxAmount,
			Amount = resourceAmount.Amount,
		};
	}
}
