using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using SatisfactoryTools.Solver.Api.Contracts;
using SatisfactoryTools.Solver.Api.Services;

namespace SatisfactoryTools.Solver.Api.Tests;

public sealed class PlannerResultDomainFactoryTests
{
	private const double Tolerance = 0.001d;

	private readonly GameDataCatalog gameDataCatalog;
	private readonly ProductionPlannerSolver solver;
	private readonly PlannerResultDomainFactory factory;

	public PlannerResultDomainFactoryTests()
	{
		var repoRoot = Environment.GetEnvironmentVariable("PWD")
			?? throw new InvalidOperationException("PWD must point at the repository root for planner result-domain tests.");
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["DataRoot"] = Path.Combine(repoRoot, "data"),
			})
			.Build();
		var environment = new FakeHostEnvironment(repoRoot);
		gameDataCatalog = new GameDataCatalog(environment, configuration);
		solver = new ProductionPlannerSolver(gameDataCatalog);
		factory = new PlannerResultDomainFactory();
	}

	public static IEnumerable<object[]> PlannerFixtureIdsWithResultDomainExpectation()
	{
		return PlannerFixtureSupport.PlannerFixtureIdsWithResultDomainExpectation();
	}

	[Fact]
	public void CreateGraphParsesSpecialKeysAndRecipeKeys()
	{
		var data = CreateMiniGameData();
		var request = new SolverRequest
		{
			RecipeCostMultiplier = 2d,
			PowerConsumptionMultiplier = 3d,
		};
		var response = new Dictionary<string, double>
		{
			["Desc_TestOre_C#Mine"] = 60d,
			["Desc_TestInput_C#Input"] = 20d,
			["Desc_TestProduct_C#Product"] = 15d,
			["Desc_TestByproduct_C#Byproduct"] = 5d,
			["Desc_TestProduct_C#Sink"] = 3d,
			["Recipe_TestAlternate_C@250#Desc_TestMachine_C"] = 2d,
			["Desc_Unknown_C#Product"] = 99d,
			["Recipe_TestAlternate_C#Desc_TestMachine_C"] = 99d,
		};

		var graph = factory.CreateGraph(request, response, data);

		Assert.Collection(graph.Nodes,
			node => Assert.IsType<PlannerMinerNode>(node),
			node => Assert.IsType<PlannerInputNode>(node),
			node => Assert.IsType<PlannerProductNode>(node),
			node => Assert.IsType<PlannerByproductNode>(node),
			node => Assert.IsType<PlannerSinkNode>(node),
			node => {
				var recipeNode = Assert.IsType<PlannerRecipeNode>(node);
				Assert.Equal("Recipe_TestAlternate_C", recipeNode.RecipeData.Recipe.ClassName);
				Assert.Equal("Desc_TestMachine_C", recipeNode.RecipeData.Machine.ClassName);
				Assert.Equal(250, recipeNode.RecipeData.ClockSpeed);
				Assert.Equal(2d, recipeNode.RecipeData.Amount);
				Assert.Equal(3d, recipeNode.RecipeData.PowerConsumptionMultiplier);
				Assert.Equal(2d, recipeNode.RecipeData.RecipeCostMultiplier);
			});

	}

	[Fact]
	public void GenerateEdgesGreedilyStopsWhenOutputFallsWithinEpsilon()
	{
		var data = CreateMiniGameData();
		var graph = new PlannerResultGraph();
		var source = new PlannerMinerNode(new PlannerItemAmount("Desc_TestOre_C", 1d), data);
		var firstConsumer = new PlannerProductNode(new PlannerItemAmount("Desc_TestOre_C", 0.999999999995d), data);
		var secondConsumer = new PlannerByproductNode(new PlannerItemAmount("Desc_TestOre_C", 1d), data);

		graph.AddNode(source);
		graph.AddNode(firstConsumer);
		graph.AddNode(secondConsumer);
		graph.GenerateEdges();

		Assert.Single(graph.Edges);
		Assert.InRange(graph.Edges[0].ItemAmount.Amount, 0.999999999994d, 1d);
		Assert.InRange(firstConsumer.Inputs[0].Amount, 0.999999999994d, 0.9999999999951d);
		Assert.Equal(0d, source.Outputs[0].Amount);
		Assert.Equal(0d, secondConsumer.Inputs[0].Amount);
	}

	[Fact]
	public void PlannerResultMathMatchesTypeScriptBoundaryRounding()
	{
		Assert.Equal(1.01d, PlannerResultMath.Round(1.005d, 2));
		Assert.Equal(0.1234d, PlannerResultMath.Ceil(0.12329999999999998d, 4));
		Assert.Equal(0.1234d, PlannerResultMath.Floor(0.12339999999999998d, 4));
	}

	[Fact]
	public void UnderclockLastUsesTypeScriptCeilForResidualMachineClock()
	{
		var data = CreateMiniGameData();
		var recipe = new PlannerRecipeData(
			data.Buildings["Desc_TestMachine_C"],
			data.Recipes["Recipe_TestAlternate_C"],
			1.0012339999999998d,
			100);

		var machineGroup = new PlannerMachineGroup(recipe, PlannerMachineGroupMode.UnderclockLast);

		Assert.Equal(2, machineGroup.CountMachines());
		Assert.Equal(100d, machineGroup.Machines[0].ClockSpeed);
		Assert.Equal(0.1234d, machineGroup.Machines[1].ClockSpeed);
	}

	[Fact]
	public void CreateCapturesAlternateRecipesInDetails()
	{
		var data = CreateMiniGameData();
		var request = new SolverRequest();
		var response = new Dictionary<string, double>
		{
			["Recipe_TestAlternate_C@100#Desc_TestMachine_C"] = 1d,
			["Desc_TestProduct_C#Product"] = 10d,
		};

		var result = factory.Create(request, response, data);

		var alternate = Assert.Single(result.Details.AlternatesNeeded);
		Assert.Equal("Recipe_TestAlternate_C", alternate.ClassName);
		Assert.Equal("Alternate: Test Product", alternate.Name);
	}

	[Theory]
	[MemberData(nameof(PlannerFixtureIdsWithResultDomainExpectation))]
	public void PlannerFixturesMatchTargetedResultDomainParity(string fixtureId)
	{
		var fixture = PlannerFixtureSupport.LoadPlannerFixture(fixtureId);
		var expectation = Assert.IsType<PlannerFixtureSupport.PlannerFixtureResultDomainExpectation>(fixture.ResultDomainExpectation);
		var request = Assert.IsType<SolverRequest>(fixture.SolverRequest);
		var data = gameDataCatalog.Get(request.GameVersion!);
		var solve = solver.Solve(request);
		var result = factory.Create(request, solve.Result, data);

		if (expectation.Graph is not null) {
			if (expectation.Graph.NodeCount > 0) {
				Assert.Equal(expectation.Graph.NodeCount, result.Graph.Nodes.Count);
			}

			if (expectation.Graph.EdgeCount > 0) {
				Assert.Equal(expectation.Graph.EdgeCount, result.Graph.Edges.Count);
			}

			var recipeKeys = result.Graph.Nodes
				.OfType<PlannerRecipeNode>()
				.Select((node) => node.RecipeData.Recipe.ClassName + "@" + node.RecipeData.ClockSpeed + "#" + node.RecipeData.Machine.ClassName)
				.OrderBy((entry) => entry, StringComparer.Ordinal)
				.ToArray();
			Assert.Equal(expectation.Graph.RecipeKeys.OrderBy((entry) => entry, StringComparer.Ordinal), recipeKeys);
		}

		if (expectation.Flags is not null) {
			Assert.Equal(expectation.Flags.HasInput, result.Details.HasInput);
			Assert.Equal(expectation.Flags.HasOutput, result.Details.HasOutput);
			Assert.Equal(expectation.Flags.HasByproducts, result.Details.HasByproducts);
		}

		if (expectation.Buildings is not null) {
			Assert.Equal(expectation.Buildings.Amount, result.Details.Buildings.Amount);
			foreach (var (buildingClass, amount) in expectation.Buildings.BuildingAmounts) {
				Assert.Equal(amount, Assert.Contains(buildingClass, result.Details.Buildings.Buildings).Amount);
			}

			AssertNumericSubset(expectation.Buildings.Resources, result.Details.Buildings.Resources);
		}

		foreach (var (itemClass, itemExpectation) in expectation.Items) {
			var actual = Assert.Contains(itemClass, result.Details.Items);
			AssertClose(itemExpectation.Produced, actual.Produced);
			AssertClose(itemExpectation.Consumed, actual.Consumed);
			AssertClose(itemExpectation.Diff, actual.Diff);
		}

		foreach (var (itemClass, inputExpectation) in expectation.Input) {
			var actual = Assert.Contains(itemClass, result.Details.Input);
			AssertClose(inputExpectation.Max, actual.Max);
			AssertClose(inputExpectation.Used, actual.Used);
			AssertClose(inputExpectation.UsedPercentage, actual.UsedPercentage);
			AssertClose(inputExpectation.ProducedExtra, actual.ProducedExtra);
		}

		foreach (var (resourceClass, resourceExpectation) in expectation.RawResources) {
			var actual = Assert.Contains(resourceClass, result.Details.RawResources);
			Assert.Equal(resourceExpectation.Enabled, actual.Enabled);
			AssertClose(resourceExpectation.Max, actual.Max);
			AssertClose(resourceExpectation.Used, actual.Used);
			AssertClose(resourceExpectation.UsedPercentage, actual.UsedPercentage);
		}

		AssertNumericSubset(expectation.Output, result.Details.Output);
		AssertNumericSubset(expectation.Byproducts, result.Details.Byproducts);
		Assert.Equal(expectation.AlternatesNeeded, result.Details.AlternatesNeeded.Select((recipe) => recipe.ClassName).ToArray());

		if (expectation.Power is not null) {
			AssertClose(expectation.Power.TotalAverage, result.Details.Power.Total.Average);
			AssertClose(expectation.Power.TotalMax, result.Details.Power.Total.Max);
			Assert.Equal(expectation.Power.IsVariable, result.Details.Power.Total.IsVariable);

			foreach (var (buildingClass, amount) in expectation.Power.BuildingMachineCounts) {
				Assert.Equal(amount, Assert.Contains(buildingClass, result.Details.Power.ByBuilding).Amount);
			}

			foreach (var (buildingClass, average) in expectation.Power.BuildingAverage) {
				AssertClose(average, Assert.Contains(buildingClass, result.Details.Power.ByBuilding).Power.Average);
			}
		}
	}

	private static void AssertNumericSubset(IReadOnlyDictionary<string, double> expected, IReadOnlyDictionary<string, double> actual)
	{
		foreach (var (key, value) in expected) {
			AssertClose(value, Assert.Contains(key, actual));
		}
	}

	private static void AssertClose(double expected, double actual)
	{
		Assert.InRange(actual, expected - Tolerance, expected + Tolerance);
	}

	private static GameDataDocument CreateMiniGameData()
	{
		return new GameDataDocument
		{
			Items = new Dictionary<string, GameItem>(StringComparer.Ordinal)
			{
				["Desc_TestOre_C"] = new() { ClassName = "Desc_TestOre_C", Name = "Test Ore" },
				["Desc_TestInput_C"] = new() { ClassName = "Desc_TestInput_C", Name = "Test Input" },
				["Desc_TestProduct_C"] = new() { ClassName = "Desc_TestProduct_C", Name = "Test Product" },
				["Desc_TestByproduct_C"] = new() { ClassName = "Desc_TestByproduct_C", Name = "Test Byproduct" },
			},
			Recipes = new Dictionary<string, GameRecipe>(StringComparer.Ordinal)
			{
				["Recipe_TestAlternate_C"] = new()
				{
					ClassName = "Recipe_TestAlternate_C",
					Name = "Alternate: Test Product",
					Alternate = true,
					InMachine = true,
					Time = 60d,
					Ingredients = [
						new GameItemAmount { Item = "Desc_TestOre_C", Amount = 1d },
						new GameItemAmount { Item = "Desc_TestInput_C", Amount = 1d },
					],
					Products = [
						new GameItemAmount { Item = "Desc_TestProduct_C", Amount = 1d },
						new GameItemAmount { Item = "Desc_TestByproduct_C", Amount = 0.5d },
					],
					ProducedIn = ["Desc_TestMachine_C"],
				},
			},
			Buildings = new Dictionary<string, GameBuilding>(StringComparer.Ordinal)
			{
				["Desc_TestMachine_C"] = new()
				{
					ClassName = "Desc_TestMachine_C",
					Name = "Test Machine",
					Metadata = new GameBuildingMetadata
					{
						ManufacturingSpeed = 1d,
						PowerConsumption = 10d,
						PowerConsumptionExponent = 1.321929d,
					},
				},
			},
			Resources = new Dictionary<string, GameResource>(StringComparer.Ordinal)
			{
				["Desc_TestOre_C"] = new() { Item = "Desc_TestOre_C" },
			},
		};
	}

	private sealed class FakeHostEnvironment(string contentRootPath) : IHostEnvironment
	{
		public string EnvironmentName { get; set; } = Environments.Development;
		public string ApplicationName { get; set; } = nameof(SatisfactoryTools);
		public string ContentRootPath { get; set; } = contentRootPath;
		public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(contentRootPath);
	}
}
