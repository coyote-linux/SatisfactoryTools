using System.Text.Json;
using System.Text.Json.Serialization;

namespace SatisfactoryTools.Solver.Api.Contracts;

public static class SolverJson
{
	public static readonly JsonSerializerOptions Options = new()
	{
		PropertyNameCaseInsensitive = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = false,
	};

	public static readonly JsonSerializerOptions InternalPlannerResponseOptions = new(Options)
	{
		NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
	};
}
