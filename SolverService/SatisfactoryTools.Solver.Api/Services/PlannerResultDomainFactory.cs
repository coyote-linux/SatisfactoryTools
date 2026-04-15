using SatisfactoryTools.Solver.Api.Contracts;

namespace SatisfactoryTools.Solver.Api.Services;

public sealed class PlannerResultDomainFactory
{
	private readonly PlannerResultVisualizationFactory visualizationFactory = new();

	public PlannerResultDomain Create(SolverRequest request, IReadOnlyDictionary<string, double> response, GameDataDocument data)
	{
		var graph = CreateGraph(request, response, data);
		return new PlannerResultDomain {
			Graph = graph,
			Details = CreateDetails(request, graph, data),
			Visualization = visualizationFactory.Create(graph, data),
		};
	}

	public PlannerResultGraph CreateGraph(SolverRequest request, IReadOnlyDictionary<string, double> response, GameDataDocument data)
	{
		var graph = new PlannerResultGraph();

		foreach (var (recipeData, amount) in response) {
			var parts = recipeData.Split('#');
			if (parts.Length != 2) {
				continue;
			}

			var machineData = parts[0];
			var machineClass = parts[1];

			switch (machineClass) {
				case "Mine":
					graph.AddNode(new PlannerMinerNode(new PlannerItemAmount(machineData, amount), data));
					break;
				case "Sink":
					if (data.Items.ContainsKey(machineData)) {
						graph.AddNode(new PlannerSinkNode(new PlannerItemAmount(machineData, amount), data));
					}
					break;
				case "Product":
					if (data.Items.ContainsKey(machineData)) {
						graph.AddNode(new PlannerProductNode(new PlannerItemAmount(machineData, amount), data));
					}
					break;
				case "Byproduct":
					if (data.Items.ContainsKey(machineData)) {
						graph.AddNode(new PlannerByproductNode(new PlannerItemAmount(machineData, amount), data));
					}
					break;
				case "Input":
					if (data.Items.ContainsKey(machineData)) {
						graph.AddNode(new PlannerInputNode(new PlannerItemAmount(machineData, amount), data));
					}
					break;
				default:
					var recipeParts = machineData.Split('@');
					if (recipeParts.Length != 2 || !int.TryParse(recipeParts[1], out var clockSpeed)) {
						break;
					}

					if (!data.Buildings.TryGetValue(machineClass, out var machine) || !data.Recipes.TryGetValue(recipeParts[0], out var recipe)) {
						break;
					}

					graph.AddNode(new PlannerRecipeNode(new PlannerRecipeData(
						machine,
						recipe,
						amount,
						clockSpeed,
						request.PowerConsumptionMultiplier ?? 1d,
						request.RecipeCostMultiplier ?? 1d), data));
					break;
			}
		}

		graph.GenerateEdges();
		return graph;
	}

	private static PlannerResultDetails CreateDetails(SolverRequest request, PlannerResultGraph graph, GameDataDocument data)
	{
		var details = new PlannerResultDetails();
		CalculateBuildings(graph, data, details);
		CalculateItems(graph, data, details);
		CalculateInput(request, graph, data, details);
		CalculateRawResources(request, graph, data, details);
		CalculateProducts(graph, details);
		FindAlternateRecipes(graph, data, details);
		CalculatePower(graph, details);
		return details;
	}

	private static void CalculateBuildings(PlannerResultGraph graph, GameDataDocument data, PlannerResultDetails details)
	{
		var buildings = new PlannerBuildingsResultDetails();

		foreach (var node in graph.Nodes) {
			if (node is not PlannerRecipeNode recipeNode) {
				continue;
			}

			var className = recipeNode.RecipeData.Machine.ClassName;
			var amount = (int)Math.Ceiling(recipeNode.RecipeData.Amount);
			if (!buildings.Buildings.TryGetValue(className, out var buildingDetails)) {
				buildingDetails = new PlannerBuildingResultDetails();
				buildings.Buildings[className] = buildingDetails;
			}

			buildingDetails.Amount += amount;
			buildingDetails.Recipes[recipeNode.RecipeData.Recipe.ClassName] = new PlannerBuildingRecipeResultDetails {
				Amount = amount,
				Resources = CalculateBuildingCost(className, amount, data),
			};
		}

		foreach (var (_, building) in buildings.Buildings) {
			building.Resources = SumBuildingCost(building.Recipes.Values.Select((recipe) => recipe.Resources));
		}

		buildings.Resources = SumBuildingCost(buildings.Buildings.Values.Select((building) => building.Resources));
		buildings.Amount = buildings.Buildings.Values.Sum((building) => building.Amount);

		details.Buildings.Amount = buildings.Amount;
		details.Buildings.Resources = buildings.Resources;
		details.Buildings.Buildings.Clear();
		foreach (var entry in buildings.Buildings.OrderBy((entry) => data.Buildings[entry.Key].Name, StringComparer.Ordinal)) {
			details.Buildings.Buildings.Add(entry.Key, entry.Value);
		}
	}

	private static void CalculateItems(PlannerResultGraph graph, GameDataDocument data, PlannerResultDetails details)
	{
		var items = new Dictionary<string, PlannerItemResultDetails>(StringComparer.Ordinal);

		foreach (var node in graph.Nodes) {
			foreach (var edge in node.ConnectedEdges) {
				AddItem(items, node, edge);
			}
		}

		foreach (var (_, item) in items) {
			item.Diff = PlannerResultMath.Round(item.Produced - item.Consumed);

			foreach (var producer in item.Producers.Values) {
				producer.ItemPercentage = PlannerResultMath.Round(producer.ItemAmount / item.Produced * 100d);
			}

			foreach (var consumer in item.Consumers.Values) {
				consumer.ItemPercentage = PlannerResultMath.Round(consumer.ItemAmount / item.Consumed * 100d);
			}
		}

		details.Items.Clear();
		foreach (var entry in items.OrderBy((entry) => data.Resources.ContainsKey(entry.Key) ? 0 : 1)
			.ThenBy((entry) => data.Items[entry.Key].Name, StringComparer.Ordinal)) {
			details.Items.Add(entry.Key, entry.Value);
		}
	}

	private static void CalculateInput(SolverRequest request, PlannerResultGraph graph, GameDataDocument data, PlannerResultDetails details)
	{
		var inputs = new Dictionary<string, PlannerInputResultDetails>(StringComparer.Ordinal);

		foreach (var input in request.Input) {
			if (input.Item is null || input.Amount <= 0 || !data.Items.ContainsKey(input.Item)) {
				continue;
			}

			if (!inputs.TryGetValue(input.Item, out var inputDetails)) {
				inputDetails = new PlannerInputResultDetails();
				inputs[input.Item] = inputDetails;
			}

			inputDetails.Max += input.Amount;
		}

		foreach (var node in graph.Nodes) {
			switch (node) {
				case PlannerInputNode inputNode when inputs.TryGetValue(inputNode.ItemAmount.Item, out var inputDetails):
					inputDetails.Used += inputNode.ItemAmount.Amount;
					break;
				case PlannerRecipeNode recipeNode:
					foreach (var product in recipeNode.GetOutputs()) {
						if (inputs.TryGetValue(product.Resource.ClassName, out var recipeInputDetails)) {
							recipeInputDetails.ProducedExtra += product.MaxAmount;
						}
					}
					break;
				case PlannerMinerNode minerNode when inputs.TryGetValue(minerNode.ItemAmount.Item, out var minerInputDetails):
					minerInputDetails.ProducedExtra += minerNode.ItemAmount.Amount;
					break;
			}
		}

		details.Input.Clear();
		foreach (var entry in inputs) {
			entry.Value.Used = PlannerResultMath.Round(entry.Value.Used);
			entry.Value.Max = PlannerResultMath.Round(entry.Value.Max);
			entry.Value.UsedPercentage = PlannerResultMath.Round(entry.Value.Used / entry.Value.Max * 100d);
			details.Input.Add(entry.Key, entry.Value);
		}

		details.HasInput = inputs.Count > 0;
	}

	private static void CalculateRawResources(SolverRequest request, PlannerResultGraph graph, GameDataDocument data, PlannerResultDetails details)
	{
		var resources = new Dictionary<string, PlannerRawResourceResultDetails>(StringComparer.Ordinal);

		foreach (var resource in data.Resources.Keys.OrderBy((resource) => data.Items[resource].Name, StringComparer.Ordinal)) {
			resources[resource] = new PlannerRawResourceResultDetails {
				Enabled = !request.BlockedResources.Contains(resource, StringComparer.Ordinal),
				Max = request.ResourceMax.GetValueOrDefault(resource, 0d),
			};
		}

		foreach (var node in graph.Nodes) {
			if (node is PlannerMinerNode minerNode && resources.TryGetValue(minerNode.ItemAmount.Item, out var resourceDetails)) {
				resourceDetails.Used += minerNode.ItemAmount.Amount;
			}
		}

		details.RawResources.Clear();
		foreach (var entry in resources) {
			entry.Value.Used = PlannerResultMath.Round(entry.Value.Used);
			entry.Value.UsedPercentage = PlannerResultMath.Round(entry.Value.Used / entry.Value.Max * 100d);
			details.RawResources.Add(entry.Key, entry.Value);
		}
	}

	private static void CalculateProducts(PlannerResultGraph graph, PlannerResultDetails details)
	{
		foreach (var node in graph.Nodes) {
			switch (node) {
				case PlannerProductNode productNode:
					AddProduct(details.Output, productNode.ItemAmount.Item, productNode.ItemAmount.Amount);
					details.HasOutput = true;
					break;
				case PlannerByproductNode byproductNode:
					AddProduct(details.Byproducts, byproductNode.ItemAmount.Item, byproductNode.ItemAmount.Amount);
					details.HasByproducts = true;
					details.HasOutput = true;
					break;
			}
		}
	}

	private static void CalculatePower(PlannerResultGraph graph, PlannerResultDetails details)
	{
		var byRecipe = new Dictionary<string, PlannerRecipePowerDetails>(StringComparer.Ordinal);
		var byBuilding = new Dictionary<string, PlannerMachinePowerDetails>(StringComparer.Ordinal);

		foreach (var node in graph.Nodes) {
			if (node is not PlannerRecipeNode recipeNode) {
				continue;
			}

			var machineClass = recipeNode.RecipeData.Machine.ClassName;
			var recipeClass = recipeNode.RecipeData.Recipe.ClassName;
			byRecipe[recipeClass] = new PlannerRecipePowerDetails {
				Machine = machineClass,
				Machines = recipeNode.MachineData.Machines.Select(CloneMachine).ToList(),
				Power = ClonePower(recipeNode.MachineData.Power),
			};

			if (!byBuilding.TryGetValue(machineClass, out var machineDetails)) {
				machineDetails = new PlannerMachinePowerDetails();
				byBuilding[machineClass] = machineDetails;
			}

			machineDetails.Recipes[recipeClass] = new PlannerBuildingPowerRecipeDetails {
				ClockSpeed = recipeNode.RecipeData.ClockSpeed,
				Amount = recipeNode.MachineData.CountMachines(),
				Power = ClonePower(recipeNode.MachineData.Power),
			};
			machineDetails.Amount += recipeNode.MachineData.CountMachines();
			machineDetails.Power.Average += recipeNode.MachineData.Power.Average;
			machineDetails.Power.Max += recipeNode.MachineData.Power.Max;
			if (recipeNode.MachineData.Power.IsVariable) {
				machineDetails.Power.IsVariable = true;
			}
		}

		var total = new PlannerMachineGroupPower();
		foreach (var (_, machineDetails) in byBuilding) {
			total.Average += machineDetails.Power.Average;
			total.Max += machineDetails.Power.Max;
			if (machineDetails.Power.IsVariable) {
				total.IsVariable = true;
			}

			machineDetails.Power.Average = PlannerResultMath.Round(machineDetails.Power.Average);
			machineDetails.Power.Max = PlannerResultMath.Round(machineDetails.Power.Max);
		}

		if (Math.Abs(total.Max - total.Average) < PlannerResultGraph.Delta) {
			total.IsVariable = false;
		}

		details.Power.ByBuilding.Clear();
		foreach (var entry in byBuilding) {
			details.Power.ByBuilding.Add(entry.Key, entry.Value);
		}

		details.Power.ByRecipe.Clear();
		foreach (var entry in byRecipe) {
			details.Power.ByRecipe.Add(entry.Key, entry.Value);
		}

		details.Power.Total.Average = PlannerResultMath.Round(total.Average);
		details.Power.Total.Max = PlannerResultMath.Round(total.Max);
		details.Power.Total.IsVariable = total.IsVariable;
	}

	private static void FindAlternateRecipes(PlannerResultGraph graph, GameDataDocument data, PlannerResultDetails details)
	{
		var recipes = new HashSet<string>(StringComparer.Ordinal);
		foreach (var node in graph.Nodes) {
			if (node is PlannerRecipeNode recipeNode && recipeNode.RecipeData.Recipe.Alternate) {
				recipes.Add(recipeNode.RecipeData.Recipe.ClassName);
			}
		}

		details.AlternatesNeeded.Clear();
		details.AlternatesNeeded.AddRange(recipes
			.OrderBy((recipeClass) => data.Recipes[recipeClass].Name, StringComparer.Ordinal)
			.Select((recipeClass) => data.Recipes[recipeClass]));
	}

	private static void AddProduct(Dictionary<string, double> products, string product, double amount)
	{
		products[product] = products.TryGetValue(product, out var current) ? current + amount : amount;
	}

	private static void AddItem(Dictionary<string, PlannerItemResultDetails> items, PlannerGraphNode node, PlannerGraphEdge edge)
	{
		var className = edge.ItemAmount.Item;
		var amount = PlannerResultMath.Round(edge.ItemAmount.Amount);
		bool? outgoing = null;

		if (!ReferenceEquals(edge.From, edge.To)) {
			if (ReferenceEquals(edge.From, node)) {
				outgoing = true;
			} else if (ReferenceEquals(edge.To, node)) {
				outgoing = false;
			}
		}

		if (!items.TryGetValue(className, out var itemDetails)) {
			itemDetails = new PlannerItemResultDetails();
			items[className] = itemDetails;
		}

		if (outgoing == true) {
			if (edge.To is PlannerRecipeNode consumingRecipe) {
				AddRecipeAmount(itemDetails.Consumers, consumingRecipe.RecipeData.Recipe.ClassName, amount);
				itemDetails.Consumed += amount;
			}
		} else if (outgoing == false) {
			switch (edge.From) {
				case PlannerRecipeNode producingRecipe:
					AddRecipeAmount(itemDetails.Producers, producingRecipe.RecipeData.Recipe.ClassName, amount);
					itemDetails.Produced += amount;
					break;
				case PlannerMinerNode minerNode:
					AddRecipeAmount(itemDetails.Producers, minerNode.ItemAmount.Item, amount, "miner");
					itemDetails.Produced += amount;
					break;
				case PlannerInputNode inputNode:
					AddRecipeAmount(itemDetails.Producers, inputNode.ItemAmount.Item, amount, "input");
					itemDetails.Produced += amount;
					break;
			}
		} else {
			itemDetails.Produced += amount;
			itemDetails.Consumed += amount;
		}

		itemDetails.Diff = PlannerResultMath.Round(itemDetails.Produced - itemDetails.Consumed);
	}

	private static void AddRecipeAmount(Dictionary<string, PlannerItemBuildingAmountResultDetails> data, string className, double amount, string type = "recipe")
	{
		if (!data.TryGetValue(className, out var details)) {
			details = new PlannerItemBuildingAmountResultDetails { Type = type };
			data[className] = details;
		}

		details.ItemAmount += amount;
	}

	private static Dictionary<string, double> CalculateBuildingCost(string buildingClass, int amount, GameDataDocument data)
	{
		var cost = new Dictionary<string, double>(StringComparer.Ordinal);
		foreach (var recipe in data.Recipes.Values) {
			if (recipe.Products.Count > 0 && StringComparer.Ordinal.Equals(recipe.Products[0].Item, buildingClass)) {
				foreach (var ingredient in recipe.Ingredients) {
					cost[ingredient.Item] = amount * ingredient.Amount;
				}
			}
		}

		return cost;
	}

	private static Dictionary<string, double> SumBuildingCost(IEnumerable<Dictionary<string, double>> costs)
	{
		var cost = new Dictionary<string, double>(StringComparer.Ordinal);
		foreach (var entry in costs) {
			foreach (var (key, value) in entry) {
				cost[key] = cost.TryGetValue(key, out var current) ? current + value : value;
			}
		}

		return cost;
	}

	private static PlannerMachineGroupPower ClonePower(PlannerMachineGroupPower power)
	{
		return new PlannerMachineGroupPower {
			Average = PlannerResultMath.Round(power.Average),
			IsVariable = power.IsVariable,
			Max = PlannerResultMath.Round(power.Max),
		};
	}

	private static PlannerMachineGroupItem CloneMachine(PlannerMachineGroupItem machine)
	{
		return new PlannerMachineGroupItem {
			Amount = machine.Amount,
			ClockSpeed = machine.ClockSpeed,
			Power = {
				Average = PlannerResultMath.Round(machine.Power.Average),
				IsVariable = machine.Power.IsVariable,
				Max = PlannerResultMath.Round(machine.Power.Max),
			},
		};
	}
}
