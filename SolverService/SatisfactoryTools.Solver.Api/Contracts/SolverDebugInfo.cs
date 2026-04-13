namespace SatisfactoryTools.Solver.Api.Contracts;

public sealed class SolverDebugInfo
{
	public string Status { get; init; } = string.Empty;
	public string Phase { get; init; } = string.Empty;
	public string Message { get; init; } = string.Empty;
	public string SolverVersion { get; init; } = string.Empty;
	public int VariableCount { get; init; }
	public int ConstraintCount { get; init; }
	public long WallTimeMs { get; init; }
	public long Iterations { get; init; }
	public long Nodes { get; init; }
	public List<SolverDebugItemInfo> Items { get; init; } = [];
}

public sealed class SolverDebugItemInfo
{
	public string Item { get; init; } = string.Empty;
	public string Name { get; init; } = string.Empty;
	public bool Reachable { get; init; }
	public List<string> Reasons { get; init; } = [];
}

public sealed class SolverExecutionResult
{
	public IReadOnlyDictionary<string, double> Result { get; init; } = new SortedDictionary<string, double>(StringComparer.Ordinal);
	public SolverDebugInfo? Debug { get; init; }
}
