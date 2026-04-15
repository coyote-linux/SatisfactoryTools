using SatisfactoryTools.Solver.Api.Contracts;

namespace SatisfactoryTools.Solver.Api.Services;

internal sealed class PlannerResultCompositionService(
	PlannerCompatibilityService plannerCompatibilityService,
	ProductionPlannerSolver productionPlannerSolver,
	GameDataCatalog gameDataCatalog,
	PlannerResultDomainFactory plannerResultDomainFactory)
{
	public PlannerSolveCompositionOutcome Execute(PlannerState plannerState, bool showDebugOutput, string? plannerFacingVersion = null)
	{
		var normalizedPlannerState = plannerCompatibilityService.NormalizePlannerState(plannerState, plannerFacingVersion);
		var solverRequest = plannerCompatibilityService.CreateSolverRequest(
			normalizedPlannerState,
			showDebugOutput,
			normalizedPlannerState.Metadata.GameVersion);
		var solverExecution = productionPlannerSolver.Solve(solverRequest);
		var gameData = gameDataCatalog.Get(solverRequest.GameVersion ?? throw new InvalidOperationException("The derived solver request must include a game version."));
		var compositionRequest = CreateCompositionRequest(normalizedPlannerState, solverRequest);
		var resultDomain = plannerResultDomainFactory.Create(compositionRequest, solverExecution.Result, gameData);

		return new PlannerSolveCompositionOutcome
		{
			SolverRequest = solverRequest,
			SolverExecution = solverExecution,
			ResultDomain = resultDomain,
		};
	}

	private static SolverRequest CreateCompositionRequest(PlannerState normalizedPlannerState, SolverRequest solverRequest)
	{
		return new SolverRequest
		{
			GameVersion = solverRequest.GameVersion,
			ResourceMax = new Dictionary<string, double>(solverRequest.ResourceMax, StringComparer.Ordinal),
			ResourceWeight = new Dictionary<string, double>(solverRequest.ResourceWeight, StringComparer.Ordinal),
			BlockedResources = [.. solverRequest.BlockedResources],
			BlockedRecipes = [.. solverRequest.BlockedRecipes],
			AllowedAlternateRecipes = [.. solverRequest.AllowedAlternateRecipes],
			SinkableResources = [.. solverRequest.SinkableResources],
			Production = CloneProduction(solverRequest.Production),
			Input = CloneInput(solverRequest.Input),
			RecipeCostMultiplier = solverRequest.RecipeCostMultiplier,
			PowerConsumptionMultiplier = normalizedPlannerState.Request.PowerConsumptionMultiplier,
			Debug = solverRequest.Debug,
		};
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
}
