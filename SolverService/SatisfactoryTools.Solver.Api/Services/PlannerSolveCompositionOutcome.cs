using SatisfactoryTools.Solver.Api.Contracts;

namespace SatisfactoryTools.Solver.Api.Services;

internal sealed class PlannerSolveCompositionOutcome
{
	public required SolverRequest SolverRequest { get; init; }
	public required SolverExecutionResult SolverExecution { get; init; }
	public required PlannerResultDomain ResultDomain { get; init; }
}
