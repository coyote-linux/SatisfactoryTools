using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SatisfactoryTools.Solver.Api.Tests;

public class SolverApiTests : IClassFixture<WebApplicationFactory<Program>>
{
	private readonly HttpClient client;

	public SolverApiTests(WebApplicationFactory<Program> factory)
	{
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
			gameVersion = "1.0.0",
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
			gameVersion = "1.0.0",
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
		Assert.Equal(240d, multipliedOre, 6);
	}

	private sealed class SolverEnvelope
	{
		public int Code { get; init; }
		public Dictionary<string, double>? Result { get; init; }
	}
}
