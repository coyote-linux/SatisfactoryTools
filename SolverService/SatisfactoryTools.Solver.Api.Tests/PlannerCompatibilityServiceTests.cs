using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using SatisfactoryTools.Solver.Api.Contracts;
using SatisfactoryTools.Solver.Api.Services;

namespace SatisfactoryTools.Solver.Api.Tests;

public sealed class PlannerCompatibilityServiceTests
{
	private readonly GameDataCatalog gameDataCatalog;
	private readonly PlannerCompatibilityService compatibilityService;

	public PlannerCompatibilityServiceTests()
	{
		var repoRoot = Environment.GetEnvironmentVariable("PWD")
			?? throw new InvalidOperationException("PWD must point at the repository root for planner compatibility tests.");
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["DataRoot"] = Path.Combine(repoRoot, "data"),
			})
			.Build();
		var environment = new FakeHostEnvironment(repoRoot);
		gameDataCatalog = new GameDataCatalog(environment, configuration);
		compatibilityService = new PlannerCompatibilityService(gameDataCatalog);
	}

	public static IEnumerable<object[]> PlannerFixtureIds()
	{
		return PlannerFixtureSupport.PlannerFixtureIds();
	}

	public static IEnumerable<object[]> PlannerFixtureIdsWithCanonicalSolverParity()
	{
		yield return ["F001"];
		yield return ["F003"];
		yield return ["F005"];
		yield return ["F006"];
		yield return ["F007"];
		yield return ["F008"];
	}

	[Theory]
	[InlineData(null, "1.1")]
	[InlineData("1.0", "1.1")]
	[InlineData("1.0-ficsmas", "1.1-ficsmas")]
	[InlineData("1.1", "1.1")]
	[InlineData("1.1-ficsmas", "1.1-ficsmas")]
	[InlineData("1.2", "1.2")]
	[InlineData("bogus", "1.1")]
	public void NormalizeRouteVersionMatchesPlannerParity(string? input, string expected)
	{
		Assert.Equal(expected, compatibilityService.NormalizeRouteVersion(input));
	}

	[Theory]
	[InlineData("1.0", "production1")]
	[InlineData("1.1", "production1")]
	[InlineData("1.0-ficsmas", "production-ficsmas")]
	[InlineData("1.1-ficsmas", "production-ficsmas")]
	[InlineData("1.2", "production12")]
	[InlineData("bogus", "production1")]
	public void StorageKeyMappingMatchesPlannerParity(string? input, string expected)
	{
		Assert.Equal(expected, compatibilityService.GetStorageKey(input));
	}

	[Theory]
	[MemberData(nameof(PlannerFixtureIds))]
	public void PlannerFixturesMatchVersionAndStorageParity(string fixtureId)
	{
		var fixture = PlannerFixtureSupport.LoadPlannerFixture(fixtureId);

		Assert.Equal(fixture.RouteVersion, compatibilityService.NormalizeRouteVersion(fixture.RouteVersion));
		Assert.Equal(fixture.StorageKey, compatibilityService.GetStorageKey(fixture.RouteVersion));
		Assert.Equal($"/{fixture.RouteVersion}/production", fixture.RoutePath);
		Assert.Equal(fixture.RouteVersion, compatibilityService.NormalizePlannerState(fixture.PlannerState).Metadata.GameVersion);
	}

	[Theory]
	[MemberData(nameof(PlannerFixtureIdsWithCanonicalSolverParity))]
	public void PlannerFixturesDeriveCanonicalSolverRequestParity(string fixtureId)
	{
		var fixture = PlannerFixtureSupport.LoadPlannerFixture(fixtureId);
		var actual = compatibilityService.CreateSolverRequest(fixture.PlannerState, fixture.UiState.ShowDebugOutput);
		Assert.NotNull(fixture.SolverRequest);
		var expected = fixture.SolverRequest!;

		AssertEquivalent(expected, actual);
	}

	[Fact]
	public void SeasonalFixtureNormalizesEmptyResourceMapsBeforeSolverRequestDerivation()
	{
		var fixture = PlannerFixtureSupport.LoadPlannerFixture("F002");

		var normalizedState = compatibilityService.NormalizePlannerState(fixture.PlannerState);
		var solverRequest = compatibilityService.CreateSolverRequest(fixture.PlannerState, fixture.UiState.ShowDebugOutput);

		Assert.Equal("1.1-ficsmas", normalizedState.Metadata.GameVersion);
		Assert.Equal(13, normalizedState.Request.ResourceMax.Count);
		Assert.Equal(13, normalizedState.Request.ResourceWeight.Count);
		Assert.Equal(12600d, normalizedState.Request.ResourceMax["Desc_LiquidOil_C"]);
		Assert.Equal(0d, normalizedState.Request.ResourceWeight["Desc_Water_C"]);
		Assert.Equal("1.0.0-ficsmas", solverRequest.GameVersion);
		Assert.Null(solverRequest.RecipeCostMultiplier);
		Assert.Null(solverRequest.PowerConsumptionMultiplier);
		Assert.False(solverRequest.Debug);
		Assert.Single(solverRequest.Production);
		Assert.Equal("Desc_Gift_C", solverRequest.Production[0].Item);
	}

	[Fact]
	public void EmptyResourceMapsDefaultToCurrentPlannerValues()
	{
		var state = new PlannerState
		{
			Metadata = new PlannerMetadata { GameVersion = "1.1" },
			Request = new PlannerRequest
			{
				Production = [new SolverProductionItem { Item = "Desc_IronPlate_C", Type = "perMinute", Amount = 10d, Ratio = 100d }],
				ResourceMax = [],
				ResourceWeight = [],
			},
		};

		var normalized = compatibilityService.NormalizePlannerState(state);

		Assert.Equal(13, normalized.Request.ResourceMax.Count);
		Assert.Equal(13, normalized.Request.ResourceWeight.Count);
		Assert.Equal(12600d, normalized.Request.ResourceMax["Desc_LiquidOil_C"]);
		Assert.Equal(9007199254740991d, normalized.Request.ResourceMax["Desc_Water_C"]);
		Assert.Equal(9.029411764705882d, normalized.Request.ResourceWeight["Desc_SAM_C"]);
	}

	[Fact]
	public void Update8ResourceMaxAliasNormalizesToCurrentPlannerValues()
	{
		var normalized = compatibilityService.NormalizeResourceMax(new Dictionary<string, double>
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
		});

		Assert.Equal(13, normalized.Count);
		Assert.Equal(92100d, normalized["Desc_OreIron_C"]);
		Assert.Equal(12600d, normalized["Desc_LiquidOil_C"]);
		Assert.Equal(10200d, normalized["Desc_SAM_C"]);
	}

	[Fact]
	public void LegacySchemaUpgradeMatchesConverterSemantics()
	{
		var upgraded = compatibilityService.UpgradeLegacyRequest(new LegacyPlannerRequest
		{
			Name = "Legacy Plan",
			Icon = "Desc_IronPlate_C",
			ResourceMax = new Dictionary<string, double>
			{
				["Desc_LiquidOil_C"] = 7500d,
			},
			ResourceWeight = new Dictionary<string, double>
			{
				["Desc_OreIron_C"] = 99d,
			},
			BlockedResources = ["Desc_OreIron_C"],
			BlockedRecipes = ["Recipe_IronPlate_C"],
			AllowedAlternateRecipes = ["Recipe_Alternate_CastScrew_C"],
			Production = [new SolverProductionItem { Item = "Desc_IronPlate_C", Type = "perMinute", Amount = 40d, Ratio = 100d }],
		}, "1.0");

		Assert.Equal("Legacy Plan", upgraded.Metadata.Name);
		Assert.Equal("Desc_IronPlate_C", upgraded.Metadata.Icon);
		Assert.Equal("1.1", upgraded.Metadata.GameVersion);
		Assert.Equal(12600d, upgraded.Request.ResourceMax["Desc_LiquidOil_C"]);
		Assert.Equal(13, upgraded.Request.ResourceWeight.Count);
		Assert.Empty(upgraded.Request.Input);
		Assert.Empty(upgraded.Request.SinkableResources);
		Assert.Equal(["Desc_OreIron_C"], upgraded.Request.BlockedResources);
		Assert.Equal(["Recipe_IronPlate_C"], upgraded.Request.BlockedRecipes);
		Assert.Equal(["Recipe_Alternate_CastScrew_C"], upgraded.Request.AllowedAlternateRecipes);
	}

	[Fact]
	public void CanonicalSolverRequestRebuildsBlockedRecipesFromBlockedMachines()
	{
		var gameData = gameDataCatalog.Get("1.2.0");
		var blockedMachine = gameData.Recipes.Values
			.Where((recipe) => recipe.Alternate)
			.SelectMany((recipe) => recipe.ProducedIn)
			.First();
		var blockedAlternate = gameData.Recipes.Values
			.First((recipe) => recipe.Alternate && recipe.ProducedIn.Contains(blockedMachine, StringComparer.Ordinal))
			.ClassName;
		var blockedBaseRecipe = gameData.Recipes.Values
			.First((recipe) => !recipe.Alternate && recipe.InMachine && recipe.ProducedIn.Contains(blockedMachine, StringComparer.Ordinal))
			.ClassName;

		var state = new PlannerState
		{
			Metadata = new PlannerMetadata { GameVersion = "1.2" },
			Request = new PlannerRequest
			{
				BlockedMachines = [blockedMachine],
				BlockedRecipes = [blockedBaseRecipe],
				AllowedAlternateRecipes = [blockedAlternate],
				Production = [new SolverProductionItem { Item = "Desc_PackagedWater_C", Type = "perMinute", Amount = 60d, Ratio = 100d }],
				Input = [new SolverInputItem { Item = "Desc_FluidCanister_C", Amount = 60d }],
			},
		};

		var solverRequest = compatibilityService.CreateSolverRequest(state, showDebugOutput: false);

		Assert.DoesNotContain(blockedAlternate, solverRequest.AllowedAlternateRecipes);
		Assert.Contains(blockedBaseRecipe, solverRequest.BlockedRecipes);
		Assert.Equal(1, solverRequest.BlockedRecipes.Count((recipe) => recipe == blockedBaseRecipe));
		Assert.Null(solverRequest.PowerConsumptionMultiplier);
	}

	[Fact]
	public void CanonicalSolverRequestOmitsNon12RecipeMultiplierAndPreservesDebugFlag()
	{
		var state = new PlannerState
		{
			Metadata = new PlannerMetadata { GameVersion = "1.0-ficsmas" },
			Request = new PlannerRequest
			{
				RecipeCostMultiplier = 2d,
				PowerConsumptionMultiplier = 5d,
				Production = [new SolverProductionItem { Item = "Desc_Gift_C", Type = "perMinute", Amount = 15d, Ratio = 100d }],
			},
		};

		var solverRequest = compatibilityService.CreateSolverRequest(state, showDebugOutput: true);

		Assert.Equal("1.0.0-ficsmas", solverRequest.GameVersion);
		Assert.Null(solverRequest.RecipeCostMultiplier);
		Assert.Null(solverRequest.PowerConsumptionMultiplier);
		Assert.True(solverRequest.Debug);
	}

	private static void AssertEquivalent(SolverRequest expected, SolverRequest actual)
	{
		Assert.Equal(expected.GameVersion, actual.GameVersion);
		Assert.Equal(expected.RecipeCostMultiplier, actual.RecipeCostMultiplier);
		Assert.Null(actual.PowerConsumptionMultiplier);
		Assert.Equal(expected.Debug, actual.Debug);
		AssertContainsSubset(expected.ResourceMax, actual.ResourceMax);
		AssertContainsSubset(expected.ResourceWeight, actual.ResourceWeight);
		Assert.Equal(expected.BlockedResources, actual.BlockedResources);
		Assert.Equal(expected.BlockedRecipes, actual.BlockedRecipes);
		Assert.Equal(expected.AllowedAlternateRecipes, actual.AllowedAlternateRecipes);
		Assert.Equal(expected.SinkableResources, actual.SinkableResources);
		Assert.Equal(expected.Production.Count, actual.Production.Count);
		Assert.Equal(expected.Input.Count, actual.Input.Count);

		for (var index = 0; index < expected.Production.Count; index++) {
			Assert.Equal(expected.Production[index].Item, actual.Production[index].Item);
			Assert.Equal(expected.Production[index].Type, actual.Production[index].Type);
			Assert.Equal(expected.Production[index].Amount, actual.Production[index].Amount);
			Assert.Equal(expected.Production[index].Ratio, actual.Production[index].Ratio);
		}

		for (var index = 0; index < expected.Input.Count; index++) {
			Assert.Equal(expected.Input[index].Item, actual.Input[index].Item);
			Assert.Equal(expected.Input[index].Amount, actual.Input[index].Amount);
		}
	}

	private static void AssertContainsSubset(IReadOnlyDictionary<string, double> expected, IReadOnlyDictionary<string, double> actual)
	{
		foreach (var (key, value) in expected) {
			Assert.True(actual.ContainsKey(key), $"Expected key '{key}' was missing from the derived dictionary.");
			Assert.Equal(value, actual[key]);
		}
	}

	private sealed class FakeHostEnvironment(string contentRootPath) : IHostEnvironment
	{
		public string EnvironmentName { get; set; } = Environments.Development;
		public string ApplicationName { get; set; } = nameof(SatisfactoryTools);
		public string ContentRootPath { get; set; } = contentRootPath;
		public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(contentRootPath);
	}
}
