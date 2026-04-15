using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SatisfactoryTools.Solver.Api.Contracts;
using SatisfactoryTools.Solver.Api.Services;

namespace SatisfactoryTools.Solver.Api.Tests;

public sealed class InternalPlannerCalculationServiceTests : IClassFixture<WebApplicationFactory<Program>>
{
	private const double Tolerance = 0.001d;

	private readonly IServiceProvider services;
	private readonly InternalPlannerCalculationService calculationService;

	public InternalPlannerCalculationServiceTests(WebApplicationFactory<Program> factory)
	{
		services = factory.Services;
		calculationService = services.GetRequiredService<InternalPlannerCalculationService>();
	}

	[Fact]
	public void ResolvesFromTheAppServiceProvider()
	{
		Assert.NotNull(services.GetService<PlannerResultDomainFactory>());
		Assert.NotNull(services.GetService<PlannerResultCompositionService>());
		Assert.Same(calculationService, services.GetRequiredService<InternalPlannerCalculationService>());
	}

	[Fact]
	public void CalculateReturnsRawSolverOutputAndPlannerFacingComposedOutput()
	{
		var fixture = PlannerFixtureSupport.LoadPlannerFixture("F001");

		var outcome = calculationService.Calculate(fixture.PlannerState, fixture.UiState.ShowDebugOutput);

		AssertClose(60d, Assert.Contains("Desc_OreIron_C#Mine", outcome.SolverExecution.Result));
		AssertClose(40d, Assert.Contains("Desc_IronPlate_C#Product", outcome.SolverExecution.Result));
		AssertClose(40d, Assert.Contains("Desc_IronPlate_C", outcome.ResultDomain.Details.Output));
		Assert.Equal(2, outcome.ResultDomain.Graph.Nodes.OfType<PlannerRecipeNode>().Count());
		Assert.NotEmpty(outcome.ResultDomain.Visualization.Nodes);
		Assert.NotEmpty(outcome.ResultDomain.Visualization.Edges);
	}

	[Fact]
	public void CalculateKeepsPlannerOnlyPowerMultiplierOutOfSolverRequestWhileApplyingItToComposedResults()
	{
		var fixture = PlannerFixtureSupport.LoadPlannerFixture("F003");
		var plannerState = ClonePlannerState(fixture.PlannerState, powerConsumptionMultiplier: 1.2d);

		var outcome = calculationService.Calculate(plannerState, fixture.UiState.ShowDebugOutput);

		Assert.Null(outcome.SolverRequest.PowerConsumptionMultiplier);
		AssertClose(60d, Assert.Contains("Desc_OreIron_C#Mine", outcome.SolverExecution.Result));
		Assert.Equal(1.2d, outcome.ResultDomain.Graph.Nodes.OfType<PlannerRecipeNode>().Select((node) => node.RecipeData.PowerConsumptionMultiplier).Distinct().Single());
		AssertClose(19.2d, outcome.ResultDomain.Details.Power.Total.Average);
		AssertClose(19.2d, outcome.ResultDomain.Details.Power.Total.Max);
	}

	[Fact]
	public void InternalPlannerRouteResponseProjectionSerializesWithoutCycles()
	{
		var fixture = PlannerFixtureSupport.LoadPlannerFixture("F001");
		var outcome = calculationService.Calculate(fixture.PlannerState, fixture.UiState.ShowDebugOutput);
		var response = InternalPlannerCalculationResponse.FromOutcome(outcome);

		var json = JsonSerializer.Serialize(response, SolverJson.InternalPlannerResponseOptions);

		Assert.Contains("\"graph\"", json, StringComparison.Ordinal);
		Assert.Contains("\"details\"", json, StringComparison.Ordinal);
		Assert.Contains("\"visualization\"", json, StringComparison.Ordinal);
	}

	[Fact]
	public void InternalPlannerRouteResponseProjectionIncludesDebugWhenRequested()
	{
		var fixture = PlannerFixtureSupport.LoadPlannerFixture("F007");
		var outcome = calculationService.Calculate(fixture.PlannerState, fixture.UiState.ShowDebugOutput);
		var response = InternalPlannerCalculationResponse.FromOutcome(outcome);

		Assert.NotNull(response.Debug);
		Assert.Contains("feasible solution", response.Debug!.Message, StringComparison.OrdinalIgnoreCase);
		Assert.NotEmpty(response.Debug.Items);
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

	private static void AssertClose(double expected, double actual)
	{
		Assert.InRange(actual, expected - Tolerance, expected + Tolerance);
	}
}
