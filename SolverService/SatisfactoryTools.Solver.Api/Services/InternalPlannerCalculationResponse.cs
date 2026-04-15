using System.Text.Json.Serialization;
using SatisfactoryTools.Solver.Api.Contracts;

namespace SatisfactoryTools.Solver.Api.Services;

internal sealed class InternalPlannerCalculationResponse
{
	public required PlannerResultDetails Details { get; init; }
	public required PlannerResultVisualization Visualization { get; init; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public SolverDebugInfo? Debug { get; init; }

	public static InternalPlannerCalculationResponse FromOutcome(PlannerSolveCompositionOutcome outcome, bool includeDebug)
	{
		return new InternalPlannerCalculationResponse
		{
			Details = outcome.ResultDomain.Details,
			Visualization = outcome.ResultDomain.Visualization,
			Debug = includeDebug ? outcome.SolverExecution.Debug : null,
		};
	}
}
