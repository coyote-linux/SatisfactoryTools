using SatisfactoryTools.Solver.Api.Contracts;

namespace SatisfactoryTools.Solver.Api.Services;

public sealed class PlannerCompatibilityService(GameDataCatalog gameDataCatalog)
{
	private const string DefaultRouteVersion = "1.1";
	private const string Version11 = "1.1";
	private const string Version11Ficsmas = "1.1-ficsmas";
	private const string Version12 = "1.2";
	private const string Version10Alias = "1.0";
	private const string Version10FicsmasAlias = "1.0-ficsmas";
	private const string SolverVersion10 = "1.0.0";
	private const string SolverVersion10Ficsmas = "1.0.0-ficsmas";
	private const string SolverVersion11 = "1.1.0";
	private const string SolverVersion12 = "1.2.0";
	private const string LegacyOilLimit = "Desc_LiquidOil_C";

	public string NormalizeRouteVersion(string? plannerFacingVersion)
	{
		return plannerFacingVersion switch
		{
			Version10Alias => Version11,
			Version10FicsmasAlias => Version11Ficsmas,
			Version11 or Version11Ficsmas or Version12 => plannerFacingVersion,
			_ => DefaultRouteVersion,
		};
	}

	public string GetStorageKey(string? plannerFacingVersion)
	{
		return NormalizeRouteVersion(plannerFacingVersion) switch
		{
			Version11 => "production1",
			Version11Ficsmas => "production-ficsmas",
			Version12 => "production12",
			_ => "production1",
		};
	}

	public string GetSolverGameVersion(string? plannerFacingVersion)
	{
		return NormalizeRouteVersion(plannerFacingVersion) switch
		{
			Version12 => SolverVersion12,
			Version11 => SolverVersion11,
			Version11Ficsmas => SolverVersion10Ficsmas,
			_ => SolverVersion10,
		};
	}

	public PlannerState NormalizePlannerState(PlannerState state, string? plannerFacingVersion = null)
	{
		var normalizedRouteVersion = NormalizeRouteVersion(plannerFacingVersion ?? state.Metadata.GameVersion);
		return new PlannerState
		{
			Metadata = new PlannerMetadata
			{
				Name = state.Metadata.Name,
				Icon = state.Metadata.Icon,
				SchemaVersion = state.Metadata.SchemaVersion == 0 ? 1 : state.Metadata.SchemaVersion,
				GameVersion = normalizedRouteVersion,
			},
			Request = new PlannerRequest
			{
				ResourceMax = NormalizeResourceMax(state.Request.ResourceMax),
				ResourceWeight = NormalizeResourceWeights(state.Request.ResourceWeight),
				BlockedResources = [.. state.Request.BlockedResources],
				BlockedRecipes = [.. state.Request.BlockedRecipes],
				BlockedMachines = [.. state.Request.BlockedMachines],
				AllowedAlternateRecipes = [.. state.Request.AllowedAlternateRecipes],
				RecipeCostMultiplier = state.Request.RecipeCostMultiplier,
				PowerConsumptionMultiplier = state.Request.PowerConsumptionMultiplier,
				SinkableResources = [.. state.Request.SinkableResources],
				Production = CloneProduction(state.Request.Production),
				Input = CloneInput(state.Request.Input),
			},
		};
	}

	public PlannerState UpgradeLegacyRequest(LegacyPlannerRequest legacyRequest, string? plannerFacingVersion = null)
	{
		var upgraded = UpgradeLegacySchema(legacyRequest);
		return NormalizePlannerState(new PlannerState
		{
			Metadata = new PlannerMetadata
			{
				Name = upgraded.Name,
				Icon = upgraded.Icon,
				SchemaVersion = 1,
				GameVersion = NormalizeRouteVersion(plannerFacingVersion),
			},
			Request = new PlannerRequest
			{
				ResourceMax = new Dictionary<string, double>(upgraded.ResourceMax, StringComparer.Ordinal),
				ResourceWeight = new Dictionary<string, double>(upgraded.ResourceWeight, StringComparer.Ordinal),
				BlockedResources = [.. upgraded.BlockedResources],
				BlockedRecipes = [.. upgraded.BlockedRecipes],
				AllowedAlternateRecipes = [.. upgraded.AllowedAlternateRecipes],
				SinkableResources = [.. upgraded.SinkableResources],
				Production = CloneProduction(upgraded.Production),
				Input = CloneInput(upgraded.Input),
			},
		}, plannerFacingVersion);
	}

	public SolverRequest CreateSolverRequest(PlannerState state, bool showDebugOutput, string? plannerFacingVersion = null)
	{
		var normalizedState = NormalizePlannerState(state, plannerFacingVersion);
		var normalizedRouteVersion = normalizedState.Metadata.GameVersion;
		var blockedMachines = normalizedState.Request.BlockedMachines.ToHashSet(StringComparer.Ordinal);
		var gameData = gameDataCatalog.Get(GetSolverGameVersion(normalizedRouteVersion));

		var allowedAlternateRecipes = normalizedState.Request.AllowedAlternateRecipes
			.Where((recipeClass) => IsRecipeAllowedByMachines(gameData, recipeClass, blockedMachines))
			.Distinct(StringComparer.Ordinal)
			.ToList();

		var blockedRecipes = new HashSet<string>(normalizedState.Request.BlockedRecipes, StringComparer.Ordinal);
		foreach (var recipe in gameData.Recipes.Values.Where((recipe) => !recipe.Alternate && recipe.InMachine)) {
			if (recipe.ProducedIn.Any(blockedMachines.Contains)) {
				blockedRecipes.Add(recipe.ClassName);
			}
		}

		return new SolverRequest
		{
			GameVersion = GetSolverGameVersion(normalizedRouteVersion),
			ResourceMax = new Dictionary<string, double>(normalizedState.Request.ResourceMax, StringComparer.Ordinal),
			ResourceWeight = new Dictionary<string, double>(normalizedState.Request.ResourceWeight, StringComparer.Ordinal),
			BlockedResources = [.. normalizedState.Request.BlockedResources],
			BlockedRecipes = [.. blockedRecipes],
			AllowedAlternateRecipes = allowedAlternateRecipes,
			SinkableResources = [.. normalizedState.Request.SinkableResources],
			Production = CloneProduction(normalizedState.Request.Production),
			Input = CloneInput(normalizedState.Request.Input),
			RecipeCostMultiplier = normalizedRouteVersion == Version12 ? normalizedState.Request.RecipeCostMultiplier : null,
			Debug = showDebugOutput,
		};
	}

	public Dictionary<string, double> NormalizeResourceMax(IReadOnlyDictionary<string, double>? resourceMax)
	{
		if (resourceMax is null) {
			return new Dictionary<string, double>(PlannerResourceDefaults.ResourceAmounts, StringComparer.Ordinal);
		}

		if (DictionaryEquals(resourceMax, PlannerResourceDefaults.ResourceAmountsUpdate8)) {
			return new Dictionary<string, double>(PlannerResourceDefaults.ResourceAmounts, StringComparer.Ordinal);
		}

		var normalized = new Dictionary<string, double>(PlannerResourceDefaults.ResourceAmounts, StringComparer.Ordinal);
		foreach (var (key, value) in resourceMax) {
			normalized[key] = value;
		}

		return normalized;
	}

	public Dictionary<string, double> NormalizeResourceWeights(IReadOnlyDictionary<string, double>? resourceWeight)
	{
		var normalized = new Dictionary<string, double>(PlannerResourceDefaults.ResourceWeights, StringComparer.Ordinal);
		if (resourceWeight is null) {
			return normalized;
		}

		foreach (var (key, value) in resourceWeight) {
			normalized[key] = value;
		}

		return normalized;
	}

	private static LegacyPlannerRequest UpgradeLegacySchema(LegacyPlannerRequest schema)
	{
		var version = schema.Version ?? 0;
		var current = schema;

		for (; version < 2; version++) {
			current = version switch
			{
				0 => new LegacyPlannerRequest
				{
					Version = 1,
					Name = current.Name,
					Icon = current.Icon,
					ResourceMax = new Dictionary<string, double>(current.ResourceMax, StringComparer.Ordinal),
					ResourceWeight = new Dictionary<string, double>(current.ResourceWeight, StringComparer.Ordinal),
					BlockedResources = [.. current.BlockedResources],
					BlockedRecipes = [.. current.BlockedRecipes],
					AllowedAlternateRecipes = [.. current.AllowedAlternateRecipes],
					SinkableResources = [.. current.SinkableResources],
					Production = CloneProduction(current.Production),
					Input = [],
				},
				1 => new LegacyPlannerRequest
				{
					Version = 2,
					Name = current.Name,
					Icon = current.Icon,
					ResourceMax = UpgradeLegacyResourceMax(current.ResourceMax),
					ResourceWeight = new Dictionary<string, double>(PlannerResourceDefaults.ResourceWeights, StringComparer.Ordinal),
					BlockedResources = [.. current.BlockedResources],
					BlockedRecipes = [.. current.BlockedRecipes],
					AllowedAlternateRecipes = [.. current.AllowedAlternateRecipes],
					SinkableResources = [],
					Production = CloneProduction(current.Production),
					Input = CloneInput(current.Input),
				},
				_ => current,
			};
		}

		return current;
	}

	private static Dictionary<string, double> UpgradeLegacyResourceMax(IReadOnlyDictionary<string, double> resourceMax)
	{
		var upgraded = new Dictionary<string, double>(resourceMax, StringComparer.Ordinal);
		if (upgraded.TryGetValue(LegacyOilLimit, out var oilLimit) && oilLimit == 7500d) {
			upgraded[LegacyOilLimit] = PlannerResourceDefaults.ResourceAmounts[LegacyOilLimit];
		}

		return upgraded;
	}

	private static bool DictionaryEquals(IReadOnlyDictionary<string, double> left, IReadOnlyDictionary<string, double> right)
	{
		if (left.Count != right.Count) {
			return false;
		}

		foreach (var (key, value) in left) {
			if (!right.TryGetValue(key, out var rightValue) || rightValue != value) {
				return false;
			}
		}

		return true;
	}

	private static bool IsRecipeAllowedByMachines(GameDataDocument gameData, string recipeClass, HashSet<string> blockedMachines)
	{
		if (!gameData.Recipes.TryGetValue(recipeClass, out var recipe)) {
			return false;
		}

		return recipe.ProducedIn.All((machineClass) => !blockedMachines.Contains(machineClass));
	}

	private static List<SolverProductionItem> CloneProduction(IEnumerable<SolverProductionItem> production)
	{
		return production.Select((item) => new SolverProductionItem
		{
			Item = item.Item,
			Type = item.Type,
			Amount = item.Amount,
			Ratio = item.Ratio,
		}).ToList();
	}

	private static List<SolverInputItem> CloneInput(IEnumerable<SolverInputItem> input)
	{
		return input.Select((item) => new SolverInputItem
		{
			Item = item.Item,
			Amount = item.Amount,
		}).ToList();
	}

	private static class PlannerResourceDefaults
	{
		public static readonly IReadOnlyDictionary<string, double> ResourceAmountsUpdate8 = new Dictionary<string, double>(StringComparer.Ordinal)
		{
			["Desc_OreIron_C"] = 70380d,
			["Desc_OreCopper_C"] = 28860d,
			["Desc_Stone_C"] = 52860d,
			["Desc_Coal_C"] = 30120d,
			["Desc_OreGold_C"] = 11040d,
			["Desc_LiquidOil_C"] = 11700d,
			["Desc_RawQuartz_C"] = 10500d,
			["Desc_Sulfur_C"] = 6840d,
			["Desc_OreBauxite_C"] = 9780d,
			["Desc_OreUranium_C"] = 2100d,
			["Desc_NitrogenGas_C"] = 12000d,
			["Desc_Water_C"] = 9007199254740991d,
		};

		public static readonly IReadOnlyDictionary<string, double> ResourceAmounts = new Dictionary<string, double>(StringComparer.Ordinal)
		{
			["Desc_OreIron_C"] = 92100d,
			["Desc_OreCopper_C"] = 36900d,
			["Desc_Stone_C"] = 69900d,
			["Desc_Coal_C"] = 42300d,
			["Desc_OreGold_C"] = 15000d,
			["Desc_LiquidOil_C"] = 12600d,
			["Desc_RawQuartz_C"] = 13500d,
			["Desc_Sulfur_C"] = 10800d,
			["Desc_OreBauxite_C"] = 12300d,
			["Desc_OreUranium_C"] = 2100d,
			["Desc_NitrogenGas_C"] = 12000d,
			["Desc_SAM_C"] = 10200d,
			["Desc_Water_C"] = 9007199254740991d,
		};

		public static readonly IReadOnlyDictionary<string, double> ResourceWeights = new Dictionary<string, double>(StringComparer.Ordinal)
		{
			["Desc_OreIron_C"] = 1d,
			["Desc_OreCopper_C"] = 2.4959349593495936d,
			["Desc_Stone_C"] = 1.3175965665236051d,
			["Desc_Coal_C"] = 2.1773049645390072d,
			["Desc_OreGold_C"] = 6.140000000000001d,
			["Desc_LiquidOil_C"] = 7.30952380952381d,
			["Desc_RawQuartz_C"] = 6.822222222222222d,
			["Desc_Sulfur_C"] = 8.527777777777779d,
			["Desc_OreBauxite_C"] = 7.487804878048781d,
			["Desc_OreUranium_C"] = 43.85714285714286d,
			["Desc_NitrogenGas_C"] = 7.675000000000001d,
			["Desc_SAM_C"] = 9.029411764705882d,
			["Desc_Water_C"] = 0d,
		};
	}
}
