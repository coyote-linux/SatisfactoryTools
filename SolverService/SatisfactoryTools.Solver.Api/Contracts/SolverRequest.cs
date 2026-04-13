using System.Text.Json.Serialization;

namespace SatisfactoryTools.Solver.Api.Contracts;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class SolverRequest
{
	public Dictionary<string, double> ResourceMax { get; init; } = [];
	public Dictionary<string, double> ResourceWeight { get; init; } = [];
	public List<string> BlockedResources { get; init; } = [];
	public List<string> BlockedRecipes { get; init; } = [];
	public List<string> AllowedAlternateRecipes { get; init; } = [];
	public List<string> SinkableResources { get; init; } = [];
	public List<SolverProductionItem> Production { get; init; } = [];
	public List<SolverInputItem> Input { get; init; } = [];
	public string? GameVersion { get; init; }
	public double? RecipeCostMultiplier { get; init; }
	public double? PowerConsumptionMultiplier { get; init; }
	public bool Debug { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class SolverProductionItem
{
	public string? Item { get; init; }
	public string? Type { get; init; }
	public double Amount { get; init; }
	public double Ratio { get; init; } = 100;
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class SolverInputItem
{
	public string? Item { get; init; }
	public double Amount { get; init; }
}
