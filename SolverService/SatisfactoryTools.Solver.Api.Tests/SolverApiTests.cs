using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace SatisfactoryTools.Solver.Api.Tests;

public class SolverApiTests : IClassFixture<WebApplicationFactory<Program>>
{
	private readonly WebApplicationFactory<Program> factory;
	private readonly HttpClient client;

	public SolverApiTests(WebApplicationFactory<Program> factory)
	{
		this.factory = factory;
		client = factory.CreateClient();
	}

	[Fact]
	public async Task HealthEndpointReturnsActiveStatus()
	{
		var response = await client.GetAsync("/v2/");
		response.EnsureSuccessStatusCode();

		var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
		Assert.NotNull(payload);
		Assert.Equal("200", payload!["code"].ToString());
		Assert.Equal("True", payload["active"].ToString());
	}

	[Fact]
	public async Task MissingGameVersionReturnsCompatibilityError()
	{
		var response = await client.PostAsJsonAsync("/v2/solver", new
		{
			resourceMax = new Dictionary<string, double>(),
			resourceWeight = new Dictionary<string, double>(),
			blockedResources = Array.Empty<string>(),
			blockedRecipes = Array.Empty<string>(),
			allowedAlternateRecipes = Array.Empty<string>(),
			sinkableResources = Array.Empty<string>(),
			production = new[] { new { item = "Desc_IronPlate_C", type = "perMinute", amount = 20d, ratio = 100d } },
			input = Array.Empty<object>(),
		});

		Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
		var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
		Assert.NotNull(payload);
		Assert.Contains("gameVersion", payload!["error"].ToString());
	}

	[Fact]
	public async Task IronPlateSolveReturnsResultEnvelope()
	{
		var response = await client.PostAsJsonAsync("/v2/solver", new
		{
			gameVersion = "1.1.0",
			resourceMax = new Dictionary<string, double>
			{
				["Desc_OreIron_C"] = 120,
				["Desc_Water_C"] = 0,
			},
			resourceWeight = new Dictionary<string, double>
			{
				["Desc_OreIron_C"] = 1,
				["Desc_Water_C"] = 0,
			},
			blockedResources = Array.Empty<string>(),
			blockedRecipes = Array.Empty<string>(),
			allowedAlternateRecipes = Array.Empty<string>(),
			sinkableResources = Array.Empty<string>(),
			production = new[]
			{
				new { item = "Desc_IronPlate_C", type = "perMinute", amount = 40d, ratio = 100d },
			},
			input = Array.Empty<object>(),
		});

		response.EnsureSuccessStatusCode();
		var payload = await response.Content.ReadFromJsonAsync<SolverEnvelope>();
		Assert.NotNull(payload);
		Assert.Equal(200, payload!.Code);
		Assert.NotNull(payload.Result);
		Assert.Contains("Desc_OreIron_C#Mine", payload.Result!.Keys);
		Assert.Contains("Desc_IronPlate_C#Product", payload.Result.Keys);
		Assert.Contains(payload.Result.Keys, (key) => key.Contains("@100#", StringComparison.Ordinal));
	}

	[Fact]
	public async Task RecipeCostMultiplierIncreasesRequiredRawResources()
	{
		var baseRequest = new
		{
			gameVersion = "1.2.0",
			resourceMax = new Dictionary<string, double>
			{
				["Desc_OreIron_C"] = 1000,
				["Desc_Water_C"] = 0,
			},
			resourceWeight = new Dictionary<string, double>
			{
				["Desc_OreIron_C"] = 1,
				["Desc_Water_C"] = 0,
			},
			blockedResources = Array.Empty<string>(),
			blockedRecipes = Array.Empty<string>(),
			allowedAlternateRecipes = Array.Empty<string>(),
			sinkableResources = Array.Empty<string>(),
			production = new[]
			{
				new { item = "Desc_IronPlate_C", type = "perMinute", amount = 40d, ratio = 100d },
			},
			input = Array.Empty<object>(),
		};

		var baseResponse = await client.PostAsJsonAsync("/v2/solver", baseRequest);
		baseResponse.EnsureSuccessStatusCode();
		var basePayload = await baseResponse.Content.ReadFromJsonAsync<SolverEnvelope>();
		Assert.NotNull(basePayload?.Result);

		var multipliedResponse = await client.PostAsJsonAsync("/v2/solver", new
		{
			baseRequest.gameVersion,
			baseRequest.resourceMax,
			baseRequest.resourceWeight,
			baseRequest.blockedResources,
			baseRequest.blockedRecipes,
			baseRequest.allowedAlternateRecipes,
			baseRequest.sinkableResources,
			baseRequest.production,
			baseRequest.input,
			recipeCostMultiplier = 2d,
		});

		multipliedResponse.EnsureSuccessStatusCode();
		var multipliedPayload = await multipliedResponse.Content.ReadFromJsonAsync<SolverEnvelope>();
		Assert.NotNull(multipliedPayload?.Result);

		var baseOre = basePayload!.Result!["Desc_OreIron_C#Mine"];
		var multipliedOre = multipliedPayload!.Result!["Desc_OreIron_C#Mine"];

		Assert.Equal(basePayload.Result["Desc_IronPlate_C#Product"], multipliedPayload.Result["Desc_IronPlate_C#Product"]);
		Assert.True(multipliedOre > baseOre);
		Assert.InRange(multipliedOre, 239.999d, 240.001d);
	}

	[Fact]
	public async Task FractionalRecipeCostMultiplierRoundsOneCostRecipeUpToOne()
	{
		var baseRequest = new
		{
			gameVersion = "1.2.0",
			resourceMax = new Dictionary<string, double>
			{
				["Desc_OreIron_C"] = 1000d,
				["Desc_Water_C"] = 0d,
			},
			resourceWeight = new Dictionary<string, double>
			{
				["Desc_OreIron_C"] = 1d,
				["Desc_Water_C"] = 0d,
			},
			blockedResources = Array.Empty<string>(),
			blockedRecipes = Array.Empty<string>(),
			allowedAlternateRecipes = Array.Empty<string>(),
			sinkableResources = Array.Empty<string>(),
			production = new[]
			{
				new { item = "Desc_IronIngot_C", type = "perMinute", amount = 30d, ratio = 100d },
			},
			input = Array.Empty<object>(),
		};

		var baseResponse = await client.PostAsJsonAsync("/v2/solver", baseRequest);
		baseResponse.EnsureSuccessStatusCode();
		var basePayload = await baseResponse.Content.ReadFromJsonAsync<SolverEnvelope>();
		Assert.NotNull(basePayload?.Result);

		var fractionalResponse = await client.PostAsJsonAsync("/v2/solver", new
		{
			baseRequest.gameVersion,
			baseRequest.resourceMax,
			baseRequest.resourceWeight,
			baseRequest.blockedResources,
			baseRequest.blockedRecipes,
			baseRequest.allowedAlternateRecipes,
			baseRequest.sinkableResources,
			baseRequest.production,
			baseRequest.input,
			recipeCostMultiplier = 0.25d,
		});

		fractionalResponse.EnsureSuccessStatusCode();
		var fractionalPayload = await fractionalResponse.Content.ReadFromJsonAsync<SolverEnvelope>();
		Assert.NotNull(fractionalPayload?.Result);

		var baseOre = basePayload!.Result!["Desc_OreIron_C#Mine"];
		var fractionalOre = fractionalPayload!.Result!["Desc_OreIron_C#Mine"];

		Assert.Equal(basePayload.Result["Desc_IronIngot_C#Product"], fractionalPayload.Result["Desc_IronIngot_C#Product"]);
		Assert.InRange(baseOre, 29.999d, 30.001d);
		Assert.InRange(fractionalOre, 29.999d, 30.001d);
	}

	[Fact]
	public async Task FractionalRecipeCostMultiplierRoundsNonIntegerSolidIngredientCountsToNearestWhole()
	{
		var response = await client.PostAsJsonAsync("/v2/solver", new
		{
			gameVersion = "1.2.0",
			resourceMax = new Dictionary<string, double>
			{
				["Desc_OreIron_C"] = 1000d,
				["Desc_Water_C"] = 0d,
			},
			resourceWeight = new Dictionary<string, double>
			{
				["Desc_OreIron_C"] = 1d,
				["Desc_Water_C"] = 0d,
			},
			blockedResources = Array.Empty<string>(),
			blockedRecipes = Array.Empty<string>(),
			allowedAlternateRecipes = Array.Empty<string>(),
			sinkableResources = Array.Empty<string>(),
			production = new[]
			{
				new { item = "Desc_IronIngot_C", type = "perMinute", amount = 30d, ratio = 100d },
			},
			input = Array.Empty<object>(),
			recipeCostMultiplier = 1.25d,
		});

		response.EnsureSuccessStatusCode();
		var payload = await response.Content.ReadFromJsonAsync<SolverEnvelope>();
		Assert.NotNull(payload?.Result);

		Assert.Equal(30d, payload!.Result!["Desc_IronIngot_C#Product"]);
		Assert.InRange(payload.Result["Desc_OreIron_C#Mine"], 29.999d, 30.001d);
	}

	[Fact]
	public async Task FractionalRecipeCostMultiplierRoundsHalfValuesUpForSolidIngredients()
	{
		var response = await client.PostAsJsonAsync("/v2/solver", new
		{
			gameVersion = "1.2.0",
			resourceMax = new Dictionary<string, double>
			{
				["Desc_OreIron_C"] = 1000d,
				["Desc_Water_C"] = 0d,
			},
			resourceWeight = new Dictionary<string, double>
			{
				["Desc_OreIron_C"] = 1d,
				["Desc_Water_C"] = 0d,
			},
			blockedResources = Array.Empty<string>(),
			blockedRecipes = Array.Empty<string>(),
			allowedAlternateRecipes = Array.Empty<string>(),
			sinkableResources = Array.Empty<string>(),
			production = new[]
			{
				new { item = "Desc_IronIngot_C", type = "perMinute", amount = 30d, ratio = 100d },
			},
			input = Array.Empty<object>(),
			recipeCostMultiplier = 1.5d,
		});

		response.EnsureSuccessStatusCode();
		var payload = await response.Content.ReadFromJsonAsync<SolverEnvelope>();
		Assert.NotNull(payload?.Result);

		Assert.Equal(30d, payload!.Result!["Desc_IronIngot_C#Product"]);
		Assert.InRange(payload.Result["Desc_OreIron_C#Mine"], 59.999d, 60.001d);
	}

	[Fact]
	public async Task FractionalRecipeCostMultiplierRoundsTwoPointFiveUpForSolidIngredients()
	{
		var response = await client.PostAsJsonAsync("/v2/solver", new
		{
			gameVersion = "1.2.0",
			resourceMax = new Dictionary<string, double>
			{
				["Desc_OreIron_C"] = 1000d,
				["Desc_Water_C"] = 0d,
			},
			resourceWeight = new Dictionary<string, double>
			{
				["Desc_OreIron_C"] = 1d,
				["Desc_Water_C"] = 0d,
			},
			blockedResources = Array.Empty<string>(),
			blockedRecipes = Array.Empty<string>(),
			allowedAlternateRecipes = Array.Empty<string>(),
			sinkableResources = Array.Empty<string>(),
			production = new[]
			{
				new { item = "Desc_IronIngot_C", type = "perMinute", amount = 30d, ratio = 100d },
			},
			input = Array.Empty<object>(),
			recipeCostMultiplier = 2.5d,
		});

		response.EnsureSuccessStatusCode();
		var payload = await response.Content.ReadFromJsonAsync<SolverEnvelope>();
		Assert.NotNull(payload?.Result);

		Assert.Equal(30d, payload!.Result!["Desc_IronIngot_C#Product"]);
		Assert.InRange(payload.Result["Desc_OreIron_C#Mine"], 89.999d, 90.001d);
	}

	[Fact]
	public async Task FractionalRecipeCostMultiplierKeepsFluidIngredientDecimals()
	{
		var response = await client.PostAsJsonAsync("/v2/solver", new
		{
			gameVersion = "1.2.0",
			resourceMax = new Dictionary<string, double>
			{
				["Desc_Water_C"] = 1000d,
			},
			resourceWeight = new Dictionary<string, double>
			{
				["Desc_Water_C"] = 1d,
			},
			blockedResources = Array.Empty<string>(),
			blockedRecipes = Array.Empty<string>(),
			allowedAlternateRecipes = new[] { "Recipe_Alternate_PolyesterFabric_C" },
			sinkableResources = Array.Empty<string>(),
			production = new[]
			{
				new { item = "Desc_Fabric_C", type = "perMinute", amount = 30d, ratio = 100d },
			},
			input = new[]
			{
				new { item = "Desc_PolymerResin_C", amount = 30d },
			},
			recipeCostMultiplier = 1.25d,
		});

		response.EnsureSuccessStatusCode();
		var payload = await response.Content.ReadFromJsonAsync<SolverEnvelope>();
		Assert.NotNull(payload?.Result);

		Assert.Contains("Recipe_Alternate_PolyesterFabric_C@100#Desc_OilRefinery_C", payload!.Result!.Keys);
		Assert.Equal(30d, payload.Result["Desc_Fabric_C#Product"]);
		Assert.InRange(payload.Result["Desc_PolymerResin_C#Input"], 29.999d, 30.001d);
		Assert.InRange(payload.Result["Desc_Water_C#Mine"], 37.499d, 37.501d);
	}

	[Fact]
	public async Task RecipeCostMultiplierIsRejectedForVersion110()
	{
		var response = await client.PostAsJsonAsync("/v2/solver", new
		{
			gameVersion = "1.1.0",
			resourceMax = new Dictionary<string, double>
			{
				["Desc_OreIron_C"] = 1000,
				["Desc_Water_C"] = 0,
			},
			resourceWeight = new Dictionary<string, double>
			{
				["Desc_OreIron_C"] = 1,
				["Desc_Water_C"] = 0,
			},
			blockedResources = Array.Empty<string>(),
			blockedRecipes = Array.Empty<string>(),
			allowedAlternateRecipes = Array.Empty<string>(),
			sinkableResources = Array.Empty<string>(),
			production = new[]
			{
				new { item = "Desc_IronPlate_C", type = "perMinute", amount = 40d, ratio = 100d },
			},
			input = Array.Empty<object>(),
			recipeCostMultiplier = 2d,
		});

		Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
		var payload = await response.Content.ReadFromJsonAsync<ErrorEnvelope>();
		Assert.NotNull(payload);
		Assert.Equal(500, payload!.Code);
		Assert.Contains("recipeCostMultiplier", payload.Error);
	}

	[Fact]
	public async Task LowerPowerRecipesWinWhenResourceCostsTie()
	{
		var response = await client.PostAsJsonAsync("/v2/solver", new
		{
			gameVersion = "1.0.0",
			resourceMax = new Dictionary<string, double>
			{
				["Desc_Stone_C"] = 0,
				["Desc_Water_C"] = 0,
			},
			resourceWeight = new Dictionary<string, double>
			{
				["Desc_Stone_C"] = 0,
				["Desc_Water_C"] = 0,
			},
			blockedResources = Array.Empty<string>(),
			blockedRecipes = Array.Empty<string>(),
			allowedAlternateRecipes = new[] { "Recipe_Alternate_WetConcrete_C" },
			sinkableResources = Array.Empty<string>(),
			production = new[]
			{
				new { item = "Desc_Cement_C", type = "perMinute", amount = 60d, ratio = 100d },
			},
			input = new[]
			{
				new { item = "Desc_Stone_C", amount = 180d },
				new { item = "Desc_Water_C", amount = 75d },
			},
		});

		response.EnsureSuccessStatusCode();
		var payload = await response.Content.ReadFromJsonAsync<SolverEnvelope>();
		Assert.NotNull(payload?.Result);

		Assert.Contains("Recipe_Concrete_C@100#Desc_ConstructorMk1_C", payload!.Result!.Keys);
		Assert.DoesNotContain("Recipe_Alternate_WetConcrete_C@100#Desc_OilRefinery_C", payload.Result.Keys);
	}

	[Fact]
	public async Task MaxOutputStillUsesAvailableResourcesBeforeResourcePenalty()
	{
		var response = await client.PostAsJsonAsync("/v2/solver", new
		{
			gameVersion = "1.2.0",
			resourceMax = new Dictionary<string, double>
			{
				["Desc_OreIron_C"] = 120d,
				["Desc_Water_C"] = 0d,
			},
			resourceWeight = new Dictionary<string, double>
			{
				["Desc_OreIron_C"] = 999d,
				["Desc_Water_C"] = 0d,
			},
			blockedResources = Array.Empty<string>(),
			blockedRecipes = Array.Empty<string>(),
			allowedAlternateRecipes = Array.Empty<string>(),
			sinkableResources = Array.Empty<string>(),
			production = new[]
			{
				new { item = "Desc_IronPlate_C", type = "max", amount = 0d, ratio = 1d },
			},
			input = Array.Empty<object>(),
		});

		response.EnsureSuccessStatusCode();
		var payload = await response.Content.ReadFromJsonAsync<SolverEnvelope>();
		Assert.NotNull(payload?.Result);

		Assert.InRange(payload!.Result!["Desc_OreIron_C#Mine"], 119.999d, 120.001d);
		Assert.InRange(payload.Result["Desc_IronPlate_C#Product"], 79.999d, 80.001d);
	}

	[Fact]
	public async Task DirectLimestoneMiningBeatsSamConversionWhenStoneIsCheaper()
	{
		var response = await client.PostAsJsonAsync("/v2/solver", new
		{
			gameVersion = "1.2.0",
			resourceMax = new Dictionary<string, double>
			{
				["Desc_Stone_C"] = 120d,
				["Desc_Sulfur_C"] = 20d,
				["Desc_SAM_C"] = 40d,
				["Desc_Water_C"] = 0d,
			},
			resourceWeight = new Dictionary<string, double>
			{
				["Desc_Stone_C"] = 1.3175965665236051d,
				["Desc_Sulfur_C"] = 8.527777777777779d,
				["Desc_SAM_C"] = 9.029411764705882d,
				["Desc_Water_C"] = 0d,
			},
			blockedResources = Array.Empty<string>(),
			blockedRecipes = Array.Empty<string>(),
			allowedAlternateRecipes = Array.Empty<string>(),
			sinkableResources = Array.Empty<string>(),
			production = new[]
			{
				new { item = "Desc_Stone_C", type = "perMinute", amount = 120d, ratio = 100d },
			},
			input = Array.Empty<object>(),
		});

		response.EnsureSuccessStatusCode();
		var payload = await response.Content.ReadFromJsonAsync<SolverEnvelope>();
		Assert.NotNull(payload?.Result);

		Assert.InRange(payload!.Result!["Desc_Stone_C#Mine"], 119.999d, 120.001d);
		Assert.DoesNotContain("Recipe_Limestone_Sulfur_C@100#Desc_Converter_C", payload.Result.Keys);
		Assert.DoesNotContain("Recipe_IngotSAM_C@100#Desc_ConstructorMk1_C", payload.Result.Keys);
	}

	[Fact]
	public async Task ShareEndpointsRoundTripPlannerPayload()
	{
		var shareRoot = Path.Combine(Path.GetTempPath(), "satisfactorytools-share-tests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(shareRoot);

		try {
			using var shareClient = factory.WithWebHostBuilder((builder) =>
			{
				builder.ConfigureAppConfiguration((_, configuration) =>
				{
					configuration.AddInMemoryCollection(new Dictionary<string, string?>
					{
						["ShareStore:Root"] = shareRoot,
					});
				});
			}).CreateClient();

			var createResponse = await shareClient.PostAsJsonAsync("/v2/share/?version=1.2", new
			{
				metadata = new
				{
					name = "Shared Motor Plan",
					icon = "Desc_Motor_C",
					schemaVersion = 1,
					gameVersion = "1.2",
				},
				request = new
				{
					resourceMax = new Dictionary<string, double>(),
					resourceWeight = new Dictionary<string, double>(),
					blockedResources = Array.Empty<string>(),
					blockedRecipes = Array.Empty<string>(),
					allowedAlternateRecipes = Array.Empty<string>(),
					sinkableResources = Array.Empty<string>(),
					production = new[] { new { item = "Desc_Motor_C", type = "perMinute", amount = 10d, ratio = 100d } },
					input = Array.Empty<object>(),
				},
			});

			createResponse.EnsureSuccessStatusCode();
			var createPayload = await createResponse.Content.ReadFromJsonAsync<ShareCreateEnvelope>();
			Assert.NotNull(createPayload);
			Assert.Equal(200, createPayload!.Code);
			Assert.StartsWith("/1.2/production?share=", createPayload.Link, StringComparison.Ordinal);

			var shareId = createPayload.Link[(createPayload.Link.LastIndexOf('=') + 1)..];
			Assert.NotEmpty(shareId);

			var loadResponse = await shareClient.GetAsync("/v2/share/" + shareId);
			loadResponse.EnsureSuccessStatusCode();
			var loadPayload = await loadResponse.Content.ReadFromJsonAsync<ShareLoadEnvelope>();
			Assert.NotNull(loadPayload);
			Assert.Equal(200, loadPayload!.Code);
			Assert.Equal("1.2", loadPayload.Data.GetProperty("metadata").GetProperty("gameVersion").GetString());
			Assert.Equal("Shared Motor Plan", loadPayload.Data.GetProperty("metadata").GetProperty("name").GetString());
		} finally {
			Directory.Delete(shareRoot, true);
		}
	}

	private sealed class SolverEnvelope
	{
		public int Code { get; init; }
		public Dictionary<string, double>? Result { get; init; }
	}

	private sealed class ShareCreateEnvelope
	{
		public int Code { get; init; }
		public string Link { get; init; } = string.Empty;
	}

	private sealed class ErrorEnvelope
	{
		public int Code { get; init; }
		public string Error { get; init; } = string.Empty;
	}

	private sealed class ShareLoadEnvelope
	{
		public int Code { get; init; }
		public JsonElement Data { get; init; }
	}
}
