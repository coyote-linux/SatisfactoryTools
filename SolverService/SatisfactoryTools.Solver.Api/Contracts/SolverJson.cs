using System.Text.Json;

namespace SatisfactoryTools.Solver.Api.Contracts;

public static class SolverJson
{
	public static readonly JsonSerializerOptions Options = new()
	{
		PropertyNameCaseInsensitive = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = false,
	};
}
