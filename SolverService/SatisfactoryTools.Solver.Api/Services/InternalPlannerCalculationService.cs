using SatisfactoryTools.Solver.Api.Contracts;

namespace SatisfactoryTools.Solver.Api.Services;

internal sealed class InternalPlannerCalculationService(PlannerResultCompositionService plannerResultCompositionService)
{
	public PlannerSolveCompositionOutcome Calculate(PlannerState plannerState, bool showDebugOutput, string? plannerFacingVersion = null)
	{
		return plannerResultCompositionService.Execute(plannerState, showDebugOutput, plannerFacingVersion);
	}
}
