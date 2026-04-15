using SatisfactoryTools.Solver.Api.Contracts;

namespace SatisfactoryTools.Solver.Api.Services;

public sealed class PlannerResultGraph
{
	public const double Delta = 1e-8d;

	private int lastId = 1;

	public List<PlannerGraphNode> Nodes { get; } = [];
	public List<PlannerGraphEdge> Edges { get; } = [];

	public void AddNode(PlannerGraphNode node)
	{
		Nodes.Add(node);
		node.Id = lastId++;
	}

	public void AddEdge(PlannerGraphEdge edge)
	{
		Edges.Add(edge);
		edge.Id = lastId++;
	}

	public void GenerateEdges()
	{
		foreach (var nodeOut in Nodes) {
			foreach (var output in nodeOut.GetOutputs()) {
				foreach (var nodeIn in Nodes) {
					foreach (var input in nodeIn.GetInputs()) {
						if (!StringComparer.Ordinal.Equals(input.Resource.ClassName, output.Resource.ClassName) || input.Amount >= input.MaxAmount) {
							continue;
						}

						var diff = Math.Min(input.MaxAmount - input.Amount, output.Amount);
						output.Amount -= diff;
						input.Amount += diff;

						if (Math.Abs(input.MaxAmount - input.Amount) < Delta) {
							input.Amount = input.MaxAmount;
						}

						if (Math.Abs(output.Amount) < Delta) {
							output.Amount = 0;
						}

						AddEdge(new PlannerGraphEdge(nodeOut, nodeIn, new PlannerItemAmount(output.Resource.ClassName, diff)));
						if (output.Amount == 0) {
							goto NextOutput;
						}
					}
				}
			}

		NextOutput:;
		}
	}
}

public sealed class PlannerGraphEdge
{
	public PlannerGraphEdge(PlannerGraphNode from, PlannerGraphNode to, PlannerItemAmount itemAmount)
	{
		From = from;
		To = to;
		ItemAmount = itemAmount;
		from.ConnectedEdges.Add(this);
		to.ConnectedEdges.Add(this);
	}

	public int Id { get; set; }
	public PlannerGraphNode From { get; }
	public PlannerGraphNode To { get; }
	public PlannerItemAmount ItemAmount { get; }
}

public sealed class PlannerItemAmount(string item, double amount)
{
	public string Item { get; } = item;
	public double Amount { get; set; } = amount;
}

public sealed class PlannerResourceAmount(GameItem resource, double maxAmount, double amount)
{
	public GameItem Resource { get; } = resource;
	public double MaxAmount { get; } = maxAmount;
	public double Amount { get; set; } = amount;
}

public sealed class PlannerRecipeData(
	GameBuilding machine,
	GameRecipe recipe,
	double amount,
	int clockSpeed,
	double powerConsumptionMultiplier = 1d,
	double recipeCostMultiplier = 1d)
{
	public GameBuilding Machine { get; } = machine;
	public GameRecipe Recipe { get; } = recipe;
	public double Amount { get; } = amount;
	public int ClockSpeed { get; } = clockSpeed;
	public double PowerConsumptionMultiplier { get; } = powerConsumptionMultiplier;
	public double RecipeCostMultiplier { get; } = recipeCostMultiplier;
}

public enum PlannerMachineGroupMode
{
	RoundUp,
	UnderclockLast,
	UnderclockEqually,
}

public sealed class PlannerMachineGroup
{
	public PlannerMachineGroup(PlannerRecipeData recipeData, PlannerMachineGroupMode mode = PlannerMachineGroupMode.UnderclockLast)
	{
		RecipeData = recipeData;
		Mode = mode;
		Recalculate();
	}

	public PlannerRecipeData RecipeData { get; }
	public PlannerMachineGroupMode Mode { get; }
	public List<PlannerMachineGroupItem> Machines { get; private set; } = [];
	public PlannerMachineGroupPower Power { get; private set; } = new();

	public int CountMachines()
	{
		var result = 0;
		foreach (var machine in Machines) {
			result += machine.Amount;
		}

		return result;
	}

	private void Recalculate()
	{
		Machines = [];

		switch (Mode) {
			case PlannerMachineGroupMode.RoundUp:
				Machines.Add(GetMachineData((int)Math.Ceiling(RecipeData.Amount * 100d / RecipeData.ClockSpeed), RecipeData.ClockSpeed));
				break;
			case PlannerMachineGroupMode.UnderclockLast:
				var amount = (int)Math.Floor(RecipeData.Amount * 100d / RecipeData.ClockSpeed);
				if (amount > 0) {
					Machines.Add(GetMachineData(amount, RecipeData.ClockSpeed));
				}

				var exact = RecipeData.Amount * 100d / RecipeData.ClockSpeed;
				var rest = exact - Math.Floor(exact);
				if (rest > 0) {
					Machines.Add(GetMachineData(1, PlannerResultMath.Ceil(rest * RecipeData.ClockSpeed, 4)));
				}

				break;
			case PlannerMachineGroupMode.UnderclockEqually:
				var count = (int)Math.Ceiling(RecipeData.Amount * 100d / RecipeData.ClockSpeed);
				var eachExact = RecipeData.Amount * 100d / count;
				var each = PlannerResultMath.Floor(eachExact, 4);
				var boostedMachines = 0;

				if (eachExact - each > PlannerResultGraph.Delta) {
					boostedMachines = (int)Math.Ceiling((RecipeData.Amount * 100d - each * count) / 0.0001d);
				}

				if (boostedMachines > 0) {
					Machines.Add(GetMachineData(boostedMachines, each + 0.0001d));
				}

				if (count - boostedMachines > 0) {
					Machines.Add(GetMachineData(count - boostedMachines, each));
				}

				break;
		}

		Power = SumPower();
	}

	private PlannerMachineGroupItem GetMachineData(int amount, double clockSpeed)
	{
		var result = new PlannerMachineGroupItem {
			Amount = amount,
			ClockSpeed = clockSpeed,
		};
		result.Power.Average = 0;
		result.Power.IsVariable = false;
		result.Power.Max = 0;
		var power = GetPower(result);
		result.Power.Average = power.Average;
		result.Power.IsVariable = power.IsVariable;
		result.Power.Max = power.Max;
		return result;
	}

	private PlannerMachineGroupPower GetPower(PlannerMachineGroupItem machine)
	{
		var exponent = RecipeData.Machine.Metadata.PowerConsumptionExponent;
		var power = machine.Amount * (RecipeData.Machine.Metadata.PowerConsumption * Math.Pow(machine.ClockSpeed / 100d, exponent)) * RecipeData.PowerConsumptionMultiplier;
		var max = 0d;
		var isVariable = false;

		if (RecipeData.Recipe.IsVariablePower) {
			max = machine.Amount * RecipeData.Recipe.MaxPower * Math.Pow(machine.ClockSpeed / 100d, exponent) * RecipeData.PowerConsumptionMultiplier;
			var min = machine.Amount * RecipeData.Recipe.MinPower * Math.Pow(machine.ClockSpeed / 100d, exponent) * RecipeData.PowerConsumptionMultiplier;
			power = (max + min) / 2d;
			isVariable = true;
		}

		if (Mode == PlannerMachineGroupMode.RoundUp) {
			max = power;
			isVariable = true;
			power = power / Math.Ceiling(RecipeData.Amount) * RecipeData.Amount;
		}

		if (max < power) {
			max = power;
		}

		if (Math.Abs(max - power) < PlannerResultGraph.Delta) {
			isVariable = false;
		}

		return new PlannerMachineGroupPower {
			Average = power,
			IsVariable = isVariable,
			Max = max,
		};
	}

	private PlannerMachineGroupPower SumPower()
	{
		var power = new PlannerMachineGroupPower();

		foreach (var machine in Machines) {
			power.Average += machine.Power.Average;
			power.Max += machine.Power.Max;
			if (machine.Power.IsVariable) {
				power.IsVariable = true;
			}
		}

		if (Math.Abs(power.Max - power.Average) < PlannerResultGraph.Delta) {
			power.IsVariable = false;
		}

		return power;
	}
}

public abstract class PlannerGraphNode
{
	public int Id { get; set; }
	public List<PlannerGraphEdge> ConnectedEdges { get; } = [];

	public abstract IReadOnlyList<PlannerResourceAmount> GetInputs();
	public abstract IReadOnlyList<PlannerResourceAmount> GetOutputs();
}

public sealed class PlannerRecipeNode : PlannerGraphNode
{
	private const double IngredientScaleEpsilon = 1e-8d;
	private const string PackagerClassName = "Desc_Packager_C";

	public PlannerRecipeNode(PlannerRecipeData recipeData, GameDataDocument data)
	{
		RecipeData = recipeData;
		var multiplier = GetMultiplier();
		foreach (var ingredient in recipeData.Recipe.Ingredients) {
			Ingredients.Add(new PlannerResourceAmount(
				data.Items[ingredient.Item],
				GetScaledIngredientAmount(ingredient.Amount, data.Items[ingredient.Item].Liquid) * multiplier,
				0));
		}

		foreach (var product in recipeData.Recipe.Products) {
			Products.Add(new PlannerResourceAmount(data.Items[product.Item], product.Amount * multiplier, product.Amount * multiplier));
		}

		MachineData = new PlannerMachineGroup(RecipeData);
	}

	public PlannerRecipeData RecipeData { get; }
	public List<PlannerResourceAmount> Ingredients { get; } = [];
	public List<PlannerResourceAmount> Products { get; } = [];
	public PlannerMachineGroup MachineData { get; }

	public override IReadOnlyList<PlannerResourceAmount> GetInputs() => Ingredients;
	public override IReadOnlyList<PlannerResourceAmount> GetOutputs() => Products;

	private double GetMultiplier()
	{
		return RecipeData.Amount * RecipeData.Machine.Metadata.ManufacturingSpeed * (RecipeData.ClockSpeed / 100d) * (60d / RecipeData.Recipe.Time);
	}

	private double GetScaledIngredientAmount(double amount, bool isLiquid)
	{
		if (RecipeData.Machine.ClassName == PackagerClassName) {
			return amount;
		}

		var scaledAmount = amount * RecipeData.RecipeCostMultiplier;
		if (isLiquid) {
			return scaledAmount;
		}

		if (scaledAmount <= IngredientScaleEpsilon) {
			return 0;
		}

		return Math.Max(1d, Math.Floor(scaledAmount + 0.5d + IngredientScaleEpsilon));
	}
}

public sealed class PlannerMinerNode(PlannerItemAmount itemAmount, GameDataDocument data) : PlannerGraphNode
{
	public PlannerItemAmount ItemAmount { get; } = itemAmount;
	public GameItem Resource { get; } = data.Items[itemAmount.Item];
	public List<PlannerResourceAmount> Outputs { get; } = [new(data.Items[itemAmount.Item], itemAmount.Amount, itemAmount.Amount)];

	public override IReadOnlyList<PlannerResourceAmount> GetInputs() => [];
	public override IReadOnlyList<PlannerResourceAmount> GetOutputs() => Outputs;
}

public sealed class PlannerInputNode(PlannerItemAmount itemAmount, GameDataDocument data) : PlannerGraphNode
{
	public PlannerItemAmount ItemAmount { get; } = itemAmount;
	public GameItem Resource { get; } = data.Items[itemAmount.Item];
	public List<PlannerResourceAmount> Outputs { get; } = [new(data.Items[itemAmount.Item], itemAmount.Amount, itemAmount.Amount)];

	public override IReadOnlyList<PlannerResourceAmount> GetInputs() => [];
	public override IReadOnlyList<PlannerResourceAmount> GetOutputs() => Outputs;
}

public sealed class PlannerProductNode(PlannerItemAmount itemAmount, GameDataDocument data) : PlannerGraphNode
{
	public PlannerItemAmount ItemAmount { get; } = itemAmount;
	public GameItem Resource { get; } = data.Items[itemAmount.Item];
	public List<PlannerResourceAmount> Inputs { get; } = [new(data.Items[itemAmount.Item], itemAmount.Amount, 0)];

	public override IReadOnlyList<PlannerResourceAmount> GetInputs() => Inputs;
	public override IReadOnlyList<PlannerResourceAmount> GetOutputs() => [];
}

public sealed class PlannerByproductNode(PlannerItemAmount itemAmount, GameDataDocument data) : PlannerGraphNode
{
	public PlannerItemAmount ItemAmount { get; } = itemAmount;
	public GameItem Resource { get; } = data.Items[itemAmount.Item];
	public List<PlannerResourceAmount> Inputs { get; } = [new(data.Items[itemAmount.Item], itemAmount.Amount, 0)];

	public override IReadOnlyList<PlannerResourceAmount> GetInputs() => Inputs;
	public override IReadOnlyList<PlannerResourceAmount> GetOutputs() => [];
}

public sealed class PlannerSinkNode(PlannerItemAmount itemAmount, GameDataDocument data) : PlannerGraphNode
{
	public PlannerItemAmount ItemAmount { get; } = itemAmount;
	public GameItem Resource { get; } = data.Items[itemAmount.Item];
	public List<PlannerResourceAmount> Inputs { get; } = [new(data.Items[itemAmount.Item], itemAmount.Amount, 0)];

	public override IReadOnlyList<PlannerResourceAmount> GetInputs() => Inputs;
	public override IReadOnlyList<PlannerResourceAmount> GetOutputs() => [];
}

public static class PlannerResultMath
{
	private const double JavaScriptNumberEpsilon = 2.220446049250313e-16d;

	public static double Round(double value, int decimals = 3)
	{
		var factor = Math.Pow(10d, decimals);
		return Math.Round((value + JavaScriptNumberEpsilon) * factor, MidpointRounding.AwayFromZero) / factor;
	}

	public static double Ceil(double value, int decimals = 3)
	{
		var factor = Math.Pow(10d, decimals);
		return Math.Ceiling((value + JavaScriptNumberEpsilon) * factor) / factor;
	}

	public static double Floor(double value, int decimals = 3)
	{
		var factor = Math.Pow(10d, decimals);
		return Math.Floor((value + JavaScriptNumberEpsilon) * factor) / factor;
	}
}
