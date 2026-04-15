using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using SatisfactoryTools.Solver.Api.Contracts;
using SatisfactoryTools.Solver.Api.Services;

namespace SatisfactoryTools.Solver.Api.Tests;

public sealed class PlannerResultVisualizationFactoryTests
{
	private readonly GameDataCatalog gameDataCatalog;
	private readonly ProductionPlannerSolver solver;
	private readonly PlannerResultDomainFactory domainFactory;
	private readonly PlannerResultVisualizationFactory visualizationFactory;

	public PlannerResultVisualizationFactoryTests()
	{
		var repoRoot = Environment.GetEnvironmentVariable("PWD")
			?? throw new InvalidOperationException("PWD must point at the repository root for planner visualization tests.");
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["DataRoot"] = Path.Combine(repoRoot, "data"),
			})
			.Build();
		var environment = new FakeHostEnvironment(repoRoot);
		gameDataCatalog = new GameDataCatalog(environment, configuration);
		solver = new ProductionPlannerSolver(gameDataCatalog);
		domainFactory = new PlannerResultDomainFactory();
		visualizationFactory = new PlannerResultVisualizationFactory();
	}

	public static IEnumerable<object[]> PlannerFixtureIdsWithResultVisualizationExpectation()
	{
		return PlannerFixtureSupport.PlannerFixtureIdsWithResultVisualizationExpectation();
	}

	[Fact]
	public void Create_BuildsSpecialVisualNodesWithExpectedLabelsAndStyles()
	{
		var data = CreateMiniGameData();
		var request = new SolverRequest();
		var graph = domainFactory.CreateGraph(request, new Dictionary<string, double>
		{
			["Desc_TestOre_C#Mine"] = 60d,
			["Desc_TestInput_C#Input"] = 20d,
			["Desc_TestProduct_C#Product"] = 15d,
			["Desc_TestByproduct_C#Byproduct"] = 5d,
			["Desc_TestProduct_C#Sink"] = 3d,
		}, data);

		var visualization = visualizationFactory.Create(graph, data);

		Assert.Equal(5, visualization.Nodes.Count);

		var miner = Assert.Single(visualization.Nodes, (node) => node.Label == "<b>Test Ore</b>\n60 / min");
		Assert.Null(miner.Title);
		Assert.Equal("rgba(78, 93, 108, 1)", miner.Color?.Background);

		var input = Assert.Single(visualization.Nodes, (node) => node.Label == "<b>Input: Test Input</b>\n20 / min");
		Assert.Null(input.Title);
		Assert.Equal("rgba(175, 109, 14, 1)", input.Color?.Background);

		var product = Assert.Single(visualization.Nodes, (node) => node.Label == "<b>Test Product</b>\n15 / min");
		Assert.Null(product.Title);

		var byproduct = Assert.Single(visualization.Nodes, (node) => node.Label == "<b>Byproduct: Test Byproduct</b>\n5 / min");
		Assert.Null(byproduct.Title);

		var sink = Assert.Single(visualization.Nodes, (node) => node.Label == "Sink: <b>Test Product</b>\n3 / min\n30 points / min");
		Assert.Null(sink.Title);
		Assert.Equal("rgba(217, 83, 79, 1)", sink.Color?.Background);
	}

	[Fact]
	public void Create_BuildsRecipeTooltipAndMultiplierLabelParity()
	{
		var data = CreateMiniGameData();
		var request = new SolverRequest {
			RecipeCostMultiplier = 2d,
			PowerConsumptionMultiplier = 3d,
		};
		var graph = domainFactory.CreateGraph(request, new Dictionary<string, double>
		{
			["Recipe_TestAlternate_C@100#Desc_TestMachine_C"] = 1d,
		}, data);

		var visualization = visualizationFactory.Create(graph, data);
		var recipe = Assert.Single(visualization.Nodes);

		Assert.Equal("<b>Alternate: Test Product</b>\n1x Test Machine · Cost x2", recipe.Label);
		Assert.Equal(
			"1x Test Machine at <b>100%</b> clock speed<br>Recipe cost multiplier: <b>x2</b><br><br>Needed power: 30 MW<br><br><b>IN:</b> 2 / min - Test Ore<br><b>IN:</b> 2 / min - Test Input<br><b>OUT:</b> 1 / min - Test Product<br><b>OUT:</b> 0.5 / min - Test Byproduct",
			recipe.Title);
	}

	[Fact]
	public void Create_DoesNotShowRecipeCostSuffixForPackagerCase()
	{
		var data = CreateMiniGameData();
		var request = new SolverRequest {
			RecipeCostMultiplier = 2d,
		};
		var graph = domainFactory.CreateGraph(request, new Dictionary<string, double>
		{
			["Recipe_TestPackagedWater_C@100#Desc_Packager_C"] = 1d,
		}, data);

		var visualization = visualizationFactory.Create(graph, data);
		var recipe = Assert.Single(visualization.Nodes);

		Assert.Equal("<b>Packaged Test Water</b>\n1x Packager", recipe.Label);
		Assert.DoesNotContain("Cost x", recipe.Label, StringComparison.Ordinal);
		Assert.DoesNotContain("Recipe cost multiplier", recipe.Title, StringComparison.Ordinal);
		Assert.Equal(
			"1x Packager at <b>100%</b> clock speed<br>Needed power: 10 MW<br><br><b>IN:</b> 60 / min - Test Water<br><b>IN:</b> 60 / min - Test Canister<br><b>OUT:</b> 60 / min - Packaged Test Water",
			recipe.Title);
	}

	[Fact]
	public void Create_EncodesDynamicTextFragmentsInsideVisualizationMarkup()
	{
		var data = CreateHostileMiniGameData();
		var request = new SolverRequest();
		var graph = domainFactory.CreateGraph(request, new Dictionary<string, double>
		{
			["Desc_TestOre_C#Mine"] = 1d,
			["Recipe_TestUnsafe_C@100#Desc_TestMachine_C"] = 1d,
			["Desc_TestProduct_C#Product"] = 1d,
		}, data);

		var visualization = visualizationFactory.Create(graph, data);
		var recipe = Assert.Single(visualization.Nodes, (node) => node.Title is not null);
		var product = Assert.Single(visualization.Nodes, (node) => node.Label.Contains("Product &lt;script&gt;bad()&lt;/script&gt;", StringComparison.Ordinal));
		var productEdge = Assert.Single(visualization.Edges, (edge) => edge.Label.Contains("Product &lt;script&gt;bad()&lt;/script&gt;", StringComparison.Ordinal));

		Assert.Contains("Unsafe &lt;script&gt;alert(1)&lt;/script&gt;", recipe.Label, StringComparison.Ordinal);
		Assert.Contains("Machine &lt;svg/onload=1&gt;", recipe.Label, StringComparison.Ordinal);
		Assert.DoesNotContain("<script>alert(1)</script>", recipe.Label, StringComparison.Ordinal);
		Assert.DoesNotContain("<svg/onload=1>", recipe.Label, StringComparison.Ordinal);

		Assert.NotNull(recipe.Title);
		Assert.Contains("Machine &lt;svg/onload=1&gt;", recipe.Title, StringComparison.Ordinal);
		Assert.Contains("Ore &lt;img src=x onerror=1&gt;", recipe.Title, StringComparison.Ordinal);
		Assert.Contains("Product &lt;script&gt;bad()&lt;/script&gt;", recipe.Title, StringComparison.Ordinal);
		Assert.DoesNotContain("<img src=x onerror=1>", recipe.Title, StringComparison.Ordinal);
		Assert.DoesNotContain("<script>bad()</script>", recipe.Title, StringComparison.Ordinal);

		Assert.Contains("Product &lt;script&gt;bad()&lt;/script&gt;", product.Label, StringComparison.Ordinal);
		Assert.DoesNotContain("<script>bad()</script>", product.Label, StringComparison.Ordinal);
		Assert.Contains("Product &lt;script&gt;bad()&lt;/script&gt;\n1 / min", productEdge.Label, StringComparison.Ordinal);
		Assert.DoesNotContain("<script>bad()</script>", productEdge.Label, StringComparison.Ordinal);
	}

	[Fact]
	public void Create_BuildsFormattedVisualEdgesWithReverseEdgeSmoothing()
	{
		var data = CreateMiniGameData();
		var graph = new PlannerResultGraph();
		var left = new PlannerInputNode(new PlannerItemAmount("Desc_TestInput_C", 20d), data);
		var right = new PlannerProductNode(new PlannerItemAmount("Desc_TestProduct_C", 15d), data);

		graph.AddNode(left);
		graph.AddNode(right);
		graph.AddEdge(new PlannerGraphEdge(left, right, new PlannerItemAmount("Desc_TestProduct_C", 15d)));
		graph.AddEdge(new PlannerGraphEdge(right, left, new PlannerItemAmount("Desc_TestInput_C", 5d)));

		var visualization = visualizationFactory.Create(graph, data);

		Assert.Collection(visualization.Edges.OrderBy((edge) => edge.Id),
			edge => {
				Assert.Equal("Test Product\n15 / min", edge.Label);
				Assert.True(edge.Smooth.Enabled);
				Assert.Equal("curvedCW", edge.Smooth.Type);
				Assert.Equal(0.2d, edge.Smooth.Roundness);
			},
			edge => {
				Assert.Equal("Test Input\n5 / min", edge.Label);
				Assert.True(edge.Smooth.Enabled);
				Assert.Equal("curvedCW", edge.Smooth.Type);
				Assert.Equal(0.2d, edge.Smooth.Roundness);
			});
	}

	[Fact]
	public void Create_BuildsStableElkGraphPayload()
	{
		var data = CreateMiniGameData();
		var request = new SolverRequest();
		var graph = domainFactory.CreateGraph(request, new Dictionary<string, double>
		{
			["Desc_TestOre_C#Mine"] = 60d,
			["Recipe_TestAlternate_C@100#Desc_TestMachine_C"] = 1d,
			["Desc_TestProduct_C#Product"] = 1d,
		}, data);

		var visualization = visualizationFactory.Create(graph, data);

		Assert.Equal("root", visualization.ElkGraph.Id);
		Assert.Equal("org.eclipse.elk.layered", visualization.ElkGraph.LayoutOptions.Algorithm);
		Assert.True(visualization.ElkGraph.LayoutOptions.FavorStraightEdges);
		Assert.Equal("40", visualization.ElkGraph.LayoutOptions.NodeNodeSpacing);
		Assert.All(visualization.ElkGraph.Children, (child) => {
			Assert.Equal(250, child.Width);
			Assert.Equal(100, child.Height);
		});
		Assert.Equal(visualization.Nodes.Count, visualization.ElkGraph.Children.Count);
		Assert.Equal(visualization.Edges.Count, visualization.ElkGraph.Edges.Count);
	}

	[Theory]
	[MemberData(nameof(PlannerFixtureIdsWithResultVisualizationExpectation))]
	public void PlannerFixturesMatchTargetedVisualizationParity(string fixtureId)
	{
		var fixture = PlannerFixtureSupport.LoadPlannerFixture(fixtureId);
		var expectation = Assert.IsType<PlannerFixtureSupport.PlannerFixtureResultVisualizationExpectation>(fixture.ResultVisualizationExpectation);
		var request = Assert.IsType<SolverRequest>(fixture.SolverRequest);
		var data = gameDataCatalog.Get(request.GameVersion!);
		var solve = solver.Solve(request);
		var result = domainFactory.Create(request, solve.Result, data);

		Assert.Equal(expectation.NodeCount, result.Visualization.Nodes.Count);
		Assert.Equal(expectation.EdgeCount, result.Visualization.Edges.Count);

		var nodesById = result.Visualization.Nodes.ToDictionary((node) => node.Id);
		var graphNodesByKey = result.Graph.Nodes.ToDictionary(GetNodeKey, StringComparer.Ordinal);

		foreach (var (nodeKey, expectedLabel) in expectation.NodeLabels) {
			var graphNode = Assert.Contains(nodeKey, graphNodesByKey);
			var visualNode = Assert.Contains(graphNode.Id, nodesById);
			Assert.Equal(expectedLabel, visualNode.Label);
		}

		var visualEdgesById = result.Visualization.Edges.ToDictionary((edge) => edge.Id);
		foreach (var edgeExpectation in expectation.Edges) {
			var graphEdge = result.Graph.Edges.Single((edge) =>
				StringComparer.Ordinal.Equals(GetNodeKey(edge.From), edgeExpectation.From)
				&& StringComparer.Ordinal.Equals(GetNodeKey(edge.To), edgeExpectation.To));
			var visualEdge = Assert.Contains(graphEdge.Id, visualEdgesById);
			Assert.Equal(edgeExpectation.Label, visualEdge.Label);
			Assert.Equal(edgeExpectation.SmoothEnabled, visualEdge.Smooth.Enabled);
		}

		Assert.Equal("root", result.Visualization.ElkGraph.Id);
		Assert.Equal(result.Visualization.Nodes.Count, result.Visualization.ElkGraph.Children.Count);
		Assert.Equal(result.Visualization.Edges.Count, result.Visualization.ElkGraph.Edges.Count);
	}

	private static string GetNodeKey(PlannerGraphNode node)
	{
		return node switch
		{
			PlannerRecipeNode recipeNode => recipeNode.RecipeData.Recipe.ClassName + "@" + recipeNode.RecipeData.ClockSpeed + "#" + recipeNode.RecipeData.Machine.ClassName,
			PlannerMinerNode minerNode => minerNode.ItemAmount.Item + "#Mine",
			PlannerInputNode inputNode => inputNode.ItemAmount.Item + "#Input",
			PlannerProductNode productNode => productNode.ItemAmount.Item + "#Product",
			PlannerByproductNode byproductNode => byproductNode.ItemAmount.Item + "#Byproduct",
			PlannerSinkNode sinkNode => sinkNode.ItemAmount.Item + "#Sink",
			_ => throw new InvalidOperationException($"Unsupported node type '{node.GetType().Name}'."),
		};
	}

	private static GameDataDocument CreateMiniGameData()
	{
		return new GameDataDocument
		{
			Items = new Dictionary<string, GameItem>(StringComparer.Ordinal)
			{
				["Desc_TestOre_C"] = new() { ClassName = "Desc_TestOre_C", Name = "Test Ore" },
				["Desc_TestInput_C"] = new() { ClassName = "Desc_TestInput_C", Name = "Test Input" },
				["Desc_TestProduct_C"] = new() { ClassName = "Desc_TestProduct_C", Name = "Test Product", SinkPoints = 10d },
				["Desc_TestByproduct_C"] = new() { ClassName = "Desc_TestByproduct_C", Name = "Test Byproduct" },
				["Desc_TestWater_C"] = new() { ClassName = "Desc_TestWater_C", Name = "Test Water", Liquid = true },
				["Desc_TestCanister_C"] = new() { ClassName = "Desc_TestCanister_C", Name = "Test Canister" },
				["Desc_TestPackagedWater_C"] = new() { ClassName = "Desc_TestPackagedWater_C", Name = "Packaged Test Water" },
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
				["Recipe_TestPackagedWater_C"] = new()
				{
					ClassName = "Recipe_TestPackagedWater_C",
					Name = "Packaged Test Water",
					InMachine = true,
					Time = 60d,
					Ingredients = [
						new GameItemAmount { Item = "Desc_TestWater_C", Amount = 60d },
						new GameItemAmount { Item = "Desc_TestCanister_C", Amount = 60d },
					],
					Products = [
						new GameItemAmount { Item = "Desc_TestPackagedWater_C", Amount = 60d },
					],
					ProducedIn = ["Desc_Packager_C"],
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
						PowerConsumptionExponent = 1d,
					},
				},
				["Desc_Packager_C"] = new()
				{
					ClassName = "Desc_Packager_C",
					Name = "Packager",
					Metadata = new GameBuildingMetadata
					{
						ManufacturingSpeed = 1d,
						PowerConsumption = 10d,
						PowerConsumptionExponent = 1d,
					},
				},
			},
			Resources = new Dictionary<string, GameResource>(StringComparer.Ordinal)
			{
				["Desc_TestOre_C"] = new() { Item = "Desc_TestOre_C" },
				["Desc_TestWater_C"] = new() { Item = "Desc_TestWater_C" },
			},
		};
	}

	private static GameDataDocument CreateHostileMiniGameData()
	{
		return new GameDataDocument
		{
			Items = new Dictionary<string, GameItem>(StringComparer.Ordinal)
			{
				["Desc_TestOre_C"] = new() { ClassName = "Desc_TestOre_C", Name = "Ore <img src=x onerror=1>" },
				["Desc_TestProduct_C"] = new() { ClassName = "Desc_TestProduct_C", Name = "Product <script>bad()</script>" },
			},
			Recipes = new Dictionary<string, GameRecipe>(StringComparer.Ordinal)
			{
				["Recipe_TestUnsafe_C"] = new()
				{
					ClassName = "Recipe_TestUnsafe_C",
					Name = "Unsafe <script>alert(1)</script>",
					InMachine = true,
					Time = 60d,
					Ingredients = [
						new GameItemAmount { Item = "Desc_TestOre_C", Amount = 1d },
					],
					Products = [
						new GameItemAmount { Item = "Desc_TestProduct_C", Amount = 1d },
					],
					ProducedIn = ["Desc_TestMachine_C"],
				},
			},
			Buildings = new Dictionary<string, GameBuilding>(StringComparer.Ordinal)
			{
				["Desc_TestMachine_C"] = new()
				{
					ClassName = "Desc_TestMachine_C",
					Name = "Machine <svg/onload=1>",
					Metadata = new GameBuildingMetadata
					{
						ManufacturingSpeed = 1d,
						PowerConsumption = 10d,
						PowerConsumptionExponent = 1d,
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
