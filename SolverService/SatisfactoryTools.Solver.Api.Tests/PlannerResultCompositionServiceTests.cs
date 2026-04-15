using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using SatisfactoryTools.Solver.Api.Contracts;
using SatisfactoryTools.Solver.Api.Services;

namespace SatisfactoryTools.Solver.Api.Tests;

public sealed class PlannerResultCompositionServiceTests
{
	private const double Tolerance = 0.001d;

	private readonly GameDataCatalog gameDataCatalog;
	private readonly PlannerCompatibilityService compatibilityService;
	private readonly PlannerResultCompositionService compositionService;

	public PlannerResultCompositionServiceTests()
	{
		var repoRoot = Environment.GetEnvironmentVariable("PWD")
			?? throw new InvalidOperationException("PWD must point at the repository root for planner composition tests.");
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["DataRoot"] = Path.Combine(repoRoot, "data"),
			})
			.Build();
		var environment = new FakeHostEnvironment(repoRoot);
		gameDataCatalog = new GameDataCatalog(environment, configuration);
		compatibilityService = new PlannerCompatibilityService(gameDataCatalog);
		compositionService = new PlannerResultCompositionService(
			compatibilityService,
			new ProductionPlannerSolver(gameDataCatalog),
			gameDataCatalog,
			new PlannerResultDomainFactory());
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
	[MemberData(nameof(PlannerFixtureIdsWithCanonicalSolverParity))]
	public void ExecuteDerivesTheSameStrippedSolverRequestAsCompatibilityParity(string fixtureId)
	{
		var fixture = PlannerFixtureSupport.LoadPlannerFixture(fixtureId);

		var outcome = compositionService.Execute(fixture.PlannerState, fixture.UiState.ShowDebugOutput);

		Assert.NotNull(fixture.SolverRequest);
		AssertEquivalent(fixture.SolverRequest!, outcome.SolverRequest);
	}

	[Fact]
	public void ExecuteReturnsBothRawSolverOutputAndComposedDomainForFixtureBackedHappyPath()
	{
		var fixture = PlannerFixtureSupport.LoadPlannerFixture("F001");

		var outcome = compositionService.Execute(fixture.PlannerState, fixture.UiState.ShowDebugOutput);

		AssertClose(60d, Assert.Contains("Desc_OreIron_C#Mine", outcome.SolverExecution.Result));
		AssertClose(40d, Assert.Contains("Desc_IronPlate_C#Product", outcome.SolverExecution.Result));
		AssertClose(40d, Assert.Contains("Desc_IronPlate_C", outcome.ResultDomain.Details.Output));
		AssertClose(16d, outcome.ResultDomain.Details.Power.Total.Average);
		Assert.Equal(2, outcome.ResultDomain.Details.Power.ByBuilding["Desc_SmelterMk1_C"].Amount);
		Assert.Equal(2, outcome.ResultDomain.Details.Power.ByBuilding["Desc_ConstructorMk1_C"].Amount);
	}

	[Fact]
	public void ExecutePreservesPlannerOnlyPowerMultiplierWhenComposing()
	{
		var fixture = PlannerFixtureSupport.LoadPlannerFixture("F003");
		var plannerState = ClonePlannerState(fixture.PlannerState, powerConsumptionMultiplier: 1.2d);

		var outcome = compositionService.Execute(plannerState, fixture.UiState.ShowDebugOutput);

		Assert.Null(outcome.SolverRequest.PowerConsumptionMultiplier);
		Assert.Equal(1.2d, outcome.ResultDomain.Graph.Nodes.OfType<PlannerRecipeNode>().Select((node) => node.RecipeData.PowerConsumptionMultiplier).Distinct().Single());
		AssertClose(19.2d, outcome.ResultDomain.Details.Power.Total.Average);
		AssertClose(19.2d, outcome.ResultDomain.Details.Power.Total.Max);
	}

	[Fact]
	public void ExecutePreserves12RecipeMultiplierAcrossRawAndComposedOutputs()
	{
		var fixture = PlannerFixtureSupport.LoadPlannerFixture("F005");

		var outcome = compositionService.Execute(fixture.PlannerState, fixture.UiState.ShowDebugOutput);

		Assert.Equal(2d, outcome.SolverRequest.RecipeCostMultiplier);
		AssertClose(240d, Assert.Contains("Desc_OreIron_C#Mine", outcome.SolverExecution.Result));
		AssertClose(40d, Assert.Contains("Desc_IronPlate_C", outcome.ResultDomain.Details.Output));
		AssertClose(24d, outcome.ResultDomain.Details.Power.Total.Average);
		Assert.Equal(2d, outcome.ResultDomain.Graph.Nodes.OfType<PlannerRecipeNode>().Select((node) => node.RecipeData.RecipeCostMultiplier).Distinct().Single());
	}

	private static PlannerState ClonePlannerState(PlannerState state, double? powerConsumptionMultiplier = null)
	{
		return new PlannerState
		{
			Metadata = new PlannerMetadata
			{
				Name = state.Metadata.Name,
				Icon = state.Metadata.Icon,
				SchemaVersion = state.Metadata.SchemaVersion,
				GameVersion = state.Metadata.GameVersion,
			},
			Request = new PlannerRequest
			{
				ResourceMax = new Dictionary<string, double>(state.Request.ResourceMax, StringComparer.Ordinal),
				ResourceWeight = new Dictionary<string, double>(state.Request.ResourceWeight, StringComparer.Ordinal),
				BlockedResources = [.. state.Request.BlockedResources],
				BlockedRecipes = [.. state.Request.BlockedRecipes],
				BlockedMachines = [.. state.Request.BlockedMachines],
				AllowedAlternateRecipes = [.. state.Request.AllowedAlternateRecipes],
				RecipeCostMultiplier = state.Request.RecipeCostMultiplier,
				PowerConsumptionMultiplier = powerConsumptionMultiplier ?? state.Request.PowerConsumptionMultiplier,
				SinkableResources = [.. state.Request.SinkableResources],
				Production = state.Request.Production.Select((item) => new SolverProductionItem
				{
					Item = item.Item,
					Type = item.Type,
					Amount = item.Amount,
					Ratio = item.Ratio,
				}).ToList(),
				Input = state.Request.Input.Select((item) => new SolverInputItem
				{
					Item = item.Item,
					Amount = item.Amount,
				}).ToList(),
			},
		};
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

	private static void AssertClose(double expected, double actual)
	{
		Assert.InRange(actual, expected - Tolerance, expected + Tolerance);
	}

	private sealed class FakeHostEnvironment(string contentRootPath) : IHostEnvironment
	{
		public string EnvironmentName { get; set; } = Environments.Development;
		public string ApplicationName { get; set; } = nameof(SatisfactoryTools);
		public string ContentRootPath { get; set; } = contentRootPath;
		public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(contentRootPath);
	}
}
