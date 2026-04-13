using SatisfactoryTools.Solver.Api.Contracts;
using LinearSolver = Google.OrTools.LinearSolver.Solver;
using Variable = Google.OrTools.LinearSolver.Variable;

namespace SatisfactoryTools.Solver.Api.Services;

public sealed class ProductionPlannerSolver(GameDataCatalog gameDataCatalog)
{
	private const double SolveEpsilon = 1e-8;
	private const double DefaultMultiplier = 1d;
	private const double BuildingTieBreakerWeight = 0.001d;
	private const string ProductionPerMinute = "perMinute";
	private const string ProductionMax = "max";

	public IReadOnlyDictionary<string, double> Solve(SolverRequest request)
	{
		ValidateRequest(request);

		var data = gameDataCatalog.Get(request.GameVersion!);
		var solver = LinearSolver.CreateSolver("GLOP");
		if (solver is null) {
			throw new SolverValidationException("Unable to create the linear solver engine.");
		}

		var resources = data.Resources.Values.Select((resource) => resource.Item).ToHashSet(StringComparer.Ordinal);
		var blockedResources = request.BlockedResources.ToHashSet(StringComparer.Ordinal);
		var sinkableResources = request.SinkableResources.ToHashSet(StringComparer.Ordinal);
		var recipeCostMultiplier = NormalizeMultiplier(request.RecipeCostMultiplier);
		var fixedOutputs = request.Production
			.Where((item) => item.Item is not null && item.Type == ProductionPerMinute && item.Amount > SolveEpsilon)
			.GroupBy((item) => item.Item!, StringComparer.Ordinal)
			.ToDictionary((group) => group.Key, (group) => group.Sum((item) => item.Amount), StringComparer.Ordinal);
		var maxOutputs = request.Production
			.Where((item) => item.Item is not null && item.Type == ProductionMax)
			.GroupBy((item) => item.Item!, StringComparer.Ordinal)
			.ToDictionary((group) => group.Key, (group) => Math.Max(group.Sum((item) => item.Ratio), 1d), StringComparer.Ordinal);
		var inputCaps = request.Input
			.Where((item) => item.Item is not null && item.Amount > SolveEpsilon)
			.GroupBy((item) => item.Item!, StringComparer.Ordinal)
			.ToDictionary((group) => group.Key, (group) => group.Sum((item) => item.Amount), StringComparer.Ordinal);

		var recipes = BuildAllowedRecipes(request, data, recipeCostMultiplier);
		var recipeVariables = recipes.ToDictionary(
			(recipe) => recipe,
			(recipe) => solver.MakeNumVar(0, double.PositiveInfinity, recipe.ResponseKey),
			AllowedRecipeComparer.Instance);

		var mineVariables = resources.ToDictionary(
			item => item,
			item => blockedResources.Contains(item)
				? solver.MakeNumVar(0, 0, item + "#Mine")
				: solver.MakeNumVar(0, request.ResourceMax.GetValueOrDefault(item, 0), item + "#Mine"),
			StringComparer.Ordinal);

		var inputVariables = inputCaps.ToDictionary(
			pair => pair.Key,
			pair => solver.MakeNumVar(0, pair.Value, pair.Key + "#Input"),
			StringComparer.Ordinal);

		var maxOutputVariables = maxOutputs.ToDictionary(
			pair => pair.Key,
			pair => solver.MakeNumVar(0, double.PositiveInfinity, pair.Key + "#Product"),
			StringComparer.Ordinal);
		var allObjectiveVariables = mineVariables.Values
			.Concat(maxOutputVariables.Values)
			.Concat(recipeVariables.Values)
			.Distinct()
			.ToArray();

		var allItems = new HashSet<string>(data.Items.Keys, StringComparer.Ordinal);
		foreach (var item in mineVariables.Keys) {
			allItems.Add(item);
		}
		foreach (var item in fixedOutputs.Keys) {
			allItems.Add(item);
		}
		foreach (var item in maxOutputVariables.Keys) {
			allItems.Add(item);
		}
		foreach (var item in inputVariables.Keys) {
			allItems.Add(item);
		}
		foreach (var recipe in recipes) {
			foreach (var ingredient in recipe.IngredientRates.Keys) {
				allItems.Add(ingredient);
			}
			foreach (var product in recipe.ProductRates.Keys) {
				allItems.Add(product);
			}
		}

		var sinkVariables = new Dictionary<string, Variable>(StringComparer.Ordinal);
		var byproductVariables = new Dictionary<string, Variable>(StringComparer.Ordinal);
		foreach (var item in allItems) {
			if (sinkableResources.Contains(item)) {
				sinkVariables[item] = solver.MakeNumVar(0, double.PositiveInfinity, item + "#Sink");
			} else {
				byproductVariables[item] = solver.MakeNumVar(0, double.PositiveInfinity, item + "#Byproduct");
			}
		}

		foreach (var item in allItems) {
			var fixedOutput = fixedOutputs.GetValueOrDefault(item, 0);
			var balance = solver.MakeConstraint(fixedOutput, fixedOutput, item + "#Balance");

			if (mineVariables.TryGetValue(item, out var mine)) {
				balance.SetCoefficient(mine, 1);
			}
			if (inputVariables.TryGetValue(item, out var input)) {
				balance.SetCoefficient(input, 1);
			}
			if (maxOutputVariables.TryGetValue(item, out var maxOutput)) {
				balance.SetCoefficient(maxOutput, -1);
			}
			if (sinkVariables.TryGetValue(item, out var sink)) {
				balance.SetCoefficient(sink, -1);
			}
			if (byproductVariables.TryGetValue(item, out var byproduct)) {
				balance.SetCoefficient(byproduct, -1);
			}

			foreach (var recipe in recipes) {
				if (recipe.ProductRates.TryGetValue(item, out var productRate)) {
					balance.SetCoefficient(recipeVariables[recipe], productRate);
				}
				if (recipe.IngredientRates.TryGetValue(item, out var ingredientRate)) {
					balance.SetCoefficient(recipeVariables[recipe], balance.GetCoefficient(recipeVariables[recipe]) - ingredientRate);
				}
			}
		}

		var hasMaxOutputs = maxOutputVariables.Count > 0;
		var maximizeObjective = maxOutputVariables.ToDictionary(
			pair => pair.Value,
			pair => maxOutputs[pair.Key],
			ReferenceEqualityComparer.Instance);
		var resourceObjective = mineVariables.ToDictionary(
			pair => pair.Value,
			pair => request.ResourceWeight.GetValueOrDefault(pair.Key, 0),
			ReferenceEqualityComparer.Instance);
		var powerObjective = recipeVariables.ToDictionary(
			pair => pair.Value,
			pair => pair.Key.PowerCost + BuildingTieBreakerWeight,
			ReferenceEqualityComparer.Instance);

		LinearSolver.ResultStatus status;
		if (hasMaxOutputs) {
			status = SolvePhase(solver, maximizeObjective, maximize: true, allObjectiveVariables);
			if (status is not LinearSolver.ResultStatus.OPTIMAL and not LinearSolver.ResultStatus.FEASIBLE) {
				return new SortedDictionary<string, double>(StringComparer.Ordinal);
			}

			BindObjectiveValue(solver, maximizeObjective, CalculateObjectiveValue(maximizeObjective), maximize: true, "MaxOutputObjective");
		}

		status = SolvePhase(solver, resourceObjective, maximize: false, allObjectiveVariables);
		if (status is not LinearSolver.ResultStatus.OPTIMAL and not LinearSolver.ResultStatus.FEASIBLE) {
			return new SortedDictionary<string, double>(StringComparer.Ordinal);
		}

		BindObjectiveValue(solver, resourceObjective, CalculateObjectiveValue(resourceObjective), maximize: false, "ResourceObjective");

		status = SolvePhase(solver, powerObjective, maximize: false, allObjectiveVariables);
		if (status is not LinearSolver.ResultStatus.OPTIMAL and not LinearSolver.ResultStatus.FEASIBLE) {
			return new SortedDictionary<string, double>(StringComparer.Ordinal);
		}

		var result = new SortedDictionary<string, double>(StringComparer.Ordinal);
		foreach (var (item, variable) in mineVariables) {
			TryAdd(result, item + "#Mine", variable.SolutionValue());
		}
		foreach (var (item, variable) in inputVariables) {
			TryAdd(result, item + "#Input", variable.SolutionValue());
		}
		foreach (var (item, amount) in fixedOutputs) {
			TryAdd(result, item + "#Product", amount);
		}
		foreach (var (item, variable) in maxOutputVariables) {
			TryAdd(result, item + "#Product", variable.SolutionValue());
		}
		foreach (var (item, variable) in sinkVariables) {
			TryAdd(result, item + "#Sink", variable.SolutionValue());
		}
		foreach (var (item, variable) in byproductVariables) {
			TryAdd(result, item + "#Byproduct", variable.SolutionValue());
		}
		foreach (var (recipe, variable) in recipeVariables) {
			TryAdd(result, recipe.ResponseKey, variable.SolutionValue());
		}

		return result;
	}

	private static void ValidateRequest(SolverRequest request)
	{
		if (string.IsNullOrWhiteSpace(request.GameVersion)) {
			throw new SolverValidationException("The mandatory item 'gameVersion' is missing.");
		}

		if (request.GameVersion is not ("0.8.0" or "1.0.0" or "1.0.0-ficsmas" or "1.1.0" or "1.2.0")) {
			throw new SolverValidationException("Invalid version");
		}

		if (request.Production.Count == 0) {
			throw new SolverValidationException("The mandatory item 'production' is missing.");
		}

		if (request.RecipeCostMultiplier.HasValue && request.RecipeCostMultiplier.Value <= 0) {
			throw new SolverValidationException("recipeCostMultiplier must be greater than 0.");
		}

		if (request.RecipeCostMultiplier.HasValue && request.GameVersion != "1.2.0") {
			throw new SolverValidationException("recipeCostMultiplier is only supported for gameVersion '1.2.0'.");
		}
	}

	private static List<AllowedRecipe> BuildAllowedRecipes(SolverRequest request, GameDataDocument data, double recipeCostMultiplier)
	{
		var blockedRecipes = request.BlockedRecipes.ToHashSet(StringComparer.Ordinal);
		var allowedAlternates = request.AllowedAlternateRecipes.ToHashSet(StringComparer.Ordinal);
		var recipes = new List<AllowedRecipe>();

		foreach (var recipe in data.Recipes.Values) {
			if (!recipe.InMachine || recipe.ForBuilding) {
				continue;
			}
			if (recipe.Alternate && !allowedAlternates.Contains(recipe.ClassName)) {
				continue;
			}
			if (!recipe.Alternate && blockedRecipes.Contains(recipe.ClassName)) {
				continue;
			}

			var machineClass = recipe.ProducedIn.FirstOrDefault((candidate) =>
				data.Buildings.TryGetValue(candidate, out var building)
				&& building.Metadata.ManufacturingSpeed > SolveEpsilon);

			if (machineClass is null) {
				continue;
			}

			var building = data.Buildings[machineClass];
			var baseRate = building.Metadata.ManufacturingSpeed * (60d / recipe.Time);
			var productRates = recipe.Products.ToDictionary((entry) => entry.Item, (entry) => entry.Amount * baseRate, StringComparer.Ordinal);
			var ingredientRates = recipe.Ingredients.ToDictionary((entry) => entry.Item, (entry) => ScaleIngredientRequirement(entry.Amount, recipeCostMultiplier, data.Items[entry.Item].Liquid) * baseRate, StringComparer.Ordinal);
			var powerCost = GetPowerCost(recipe, building);

			recipes.Add(new AllowedRecipe(recipe, machineClass, ingredientRates, productRates, powerCost));
		}

		return recipes;
	}

	private static LinearSolver.ResultStatus SolvePhase(
		LinearSolver solver,
		IReadOnlyDictionary<Variable, double> coefficients,
		bool maximize,
		IEnumerable<Variable> objectiveVariables)
	{
		var objective = solver.Objective();
		foreach (var variable in objectiveVariables) {
			objective.SetCoefficient(variable, 0);
		}

		foreach (var (variable, coefficient) in coefficients) {
			if (Math.Abs(coefficient) > SolveEpsilon) {
				objective.SetCoefficient(variable, coefficient);
			}
		}

		if (maximize) {
			objective.SetMaximization();
		} else {
			objective.SetMinimization();
		}

		return solver.Solve();
	}

	private static double CalculateObjectiveValue(IReadOnlyDictionary<Variable, double> coefficients)
	{
		var value = 0d;
		foreach (var (variable, coefficient) in coefficients) {
			value += coefficient * variable.SolutionValue();
		}

		return value;
	}

	private static void BindObjectiveValue(
		LinearSolver solver,
		IReadOnlyDictionary<Variable, double> coefficients,
		double value,
		bool maximize,
		string constraintName)
	{
		if (coefficients.Count == 0) {
			return;
		}

		var tolerance = Math.Max(SolveEpsilon, Math.Abs(value) * 1e-6);
		var constraint = maximize
			? solver.MakeConstraint(value - tolerance, double.PositiveInfinity, constraintName)
			: solver.MakeConstraint(double.NegativeInfinity, value + tolerance, constraintName);

		foreach (var (variable, coefficient) in coefficients) {
			constraint.SetCoefficient(variable, coefficient);
		}
	}

	private static double GetPowerCost(GameRecipe recipe, GameBuilding building)
	{
		if (recipe.IsVariablePower) {
			var minPower = Math.Max(recipe.MinPower, 0);
			var maxPower = Math.Max(recipe.MaxPower, 0);
			if (maxPower > SolveEpsilon) {
				return minPower > SolveEpsilon ? (minPower + maxPower) / 2d : maxPower;
			}
			if (minPower > SolveEpsilon) {
				return minPower;
			}
		}

		return Math.Max(building.Metadata.PowerConsumption, 0);
	}

	private static double ScaleIngredientRequirement(double amount, double recipeCostMultiplier, bool isLiquid)
	{
		var scaledAmount = amount * recipeCostMultiplier;
		if (isLiquid) {
			return scaledAmount;
		}
		if (scaledAmount <= SolveEpsilon) {
			return 0;
		}

		return Math.Max(1d, Math.Floor(scaledAmount + 0.5d + SolveEpsilon));
	}

	private static void TryAdd(IDictionary<string, double> result, string key, double value)
	{
		if (double.IsNaN(value) || double.IsInfinity(value)) {
			return;
		}

		var normalized = Math.Abs(value) < SolveEpsilon ? 0 : Math.Round(value, 6);
		if (normalized <= 0) {
			return;
		}

		result[key] = normalized;
	}

	private static double NormalizeMultiplier(double? value)
	{
		if (!value.HasValue || value.Value <= 0) {
			return DefaultMultiplier;
		}

		return value.Value;
	}

	private sealed record AllowedRecipe(
		GameRecipe Recipe,
		string MachineClass,
		IReadOnlyDictionary<string, double> IngredientRates,
		IReadOnlyDictionary<string, double> ProductRates,
		double PowerCost)
	{
		public string ResponseKey => Recipe.ClassName + "@100#" + MachineClass;
	}

	private sealed class ReferenceEqualityComparer : IEqualityComparer<Variable>
	{
		public static readonly ReferenceEqualityComparer Instance = new();

		public bool Equals(Variable? x, Variable? y)
		{
			return ReferenceEquals(x, y);
		}

		public int GetHashCode(Variable obj)
		{
			return obj.GetHashCode();
		}
	}

	private sealed class AllowedRecipeComparer : IEqualityComparer<AllowedRecipe>
	{
		public static readonly AllowedRecipeComparer Instance = new();

		public bool Equals(AllowedRecipe? x, AllowedRecipe? y)
		{
			if (ReferenceEquals(x, y)) {
				return true;
			}
			if (x is null || y is null) {
				return false;
			}

			return x.Recipe.ClassName == y.Recipe.ClassName && x.MachineClass == y.MachineClass;
		}

		public int GetHashCode(AllowedRecipe obj)
		{
			return HashCode.Combine(obj.Recipe.ClassName, obj.MachineClass);
		}
	}
}
