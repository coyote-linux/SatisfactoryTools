using System.Text.Json.Serialization;

namespace SatisfactoryTools.Solver.Api.Contracts;

public sealed class PlannerState
{
	public PlannerMetadata Metadata { get; init; } = new();
	public PlannerRequest Request { get; init; } = new();
}

public sealed class PlannerMetadata
{
	public string? Name { get; init; }
	public string? Icon { get; init; }
	public int SchemaVersion { get; init; } = 1;
	public string GameVersion { get; init; } = string.Empty;
}

public sealed class PlannerRequest
{
	public Dictionary<string, double> ResourceMax { get; init; } = [];
	public Dictionary<string, double> ResourceWeight { get; init; } = [];
	public List<string> BlockedResources { get; init; } = [];
	public List<string> BlockedRecipes { get; init; } = [];
	public List<string> BlockedMachines { get; init; } = [];
	public List<string> AllowedAlternateRecipes { get; init; } = [];
	public double RecipeCostMultiplier { get; init; } = 1d;
	public double PowerConsumptionMultiplier { get; init; } = 1d;
	public List<string> SinkableResources { get; init; } = [];
	public List<SolverProductionItem> Production { get; init; } = [];
	public List<SolverInputItem> Input { get; init; } = [];
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Skip)]
public sealed class LegacyPlannerRequest
{
	public int? Version { get; init; }
	public string? Name { get; init; }
	public string? Icon { get; init; }
	public Dictionary<string, double> ResourceMax { get; init; } = [];
	public Dictionary<string, double> ResourceWeight { get; init; } = [];
	public List<string> BlockedResources { get; init; } = [];
	public List<string> BlockedRecipes { get; init; } = [];
	public List<string> AllowedAlternateRecipes { get; init; } = [];
	public List<string> SinkableResources { get; init; } = [];
	public List<SolverProductionItem> Production { get; init; } = [];
	public List<SolverInputItem> Input { get; init; } = [];
}
