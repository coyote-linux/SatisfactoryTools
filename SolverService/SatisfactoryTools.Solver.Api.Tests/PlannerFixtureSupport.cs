using System.Text.Json;
using SatisfactoryTools.Solver.Api.Contracts;

namespace SatisfactoryTools.Solver.Api.Tests;

internal static class PlannerFixtureSupport
{
	public static IEnumerable<object[]> PlannerFixtureIds()
	{
		for (var index = 1; index <= 8; index++) {
			yield return [$"F{index:000}"];
		}
	}

	public static IEnumerable<object[]> PlannerFixtureIdsWithSolverRequest()
	{
		foreach (var fixtureId in PlannerFixtureIds().Select((entry) => (string)entry[0])) {
			if (LoadPlannerFixture(fixtureId).SolverRequest is not null) {
				yield return [fixtureId];
			}
		}
	}

	public static IEnumerable<object[]> PlannerFixtureIdsWithShareExpectation()
	{
		foreach (var fixtureId in PlannerFixtureIds().Select((entry) => (string)entry[0])) {
			if (LoadPlannerFixture(fixtureId).ShareExpectation is not null) {
				yield return [fixtureId];
			}
		}
	}

	public static PlannerFixture LoadPlannerFixture(string fixtureId)
	{
		var filePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Planner", fixtureId + ".json");
		using var stream = File.OpenRead(filePath);
		var fixture = JsonSerializer.Deserialize<PlannerFixture>(stream, SolverJson.Options);
		return fixture ?? throw new InvalidOperationException($"Couldn't parse fixture '{fixtureId}'.");
	}

	public static string GetExpectedStorageKey(string routeVersion)
	{
		return routeVersion switch
		{
			"1.1" => "production1",
			"1.1-ficsmas" => "production-ficsmas",
			"1.2" => "production12",
			_ => throw new InvalidOperationException($"Unsupported route version '{routeVersion}'.")
		};
	}

	public static string GetExpectedSolverGameVersion(string routeVersion)
	{
		return routeVersion switch
		{
			"1.1" => "1.1.0",
			"1.1-ficsmas" => "1.0.0-ficsmas",
			"1.2" => "1.2.0",
			_ => throw new InvalidOperationException($"Unsupported route version '{routeVersion}'.")
		};
	}

	public sealed class PlannerFixture
	{
		public string Id { get; init; } = string.Empty;
		public string Scenario { get; init; } = string.Empty;
		public string RouteVersion { get; init; } = string.Empty;
		public string RoutePath { get; init; } = string.Empty;
		public string StorageKey { get; init; } = string.Empty;
		public PlannerFixtureUiState UiState { get; init; } = new();
		public PlannerState PlannerState { get; init; } = new();
		public SolverRequest? SolverRequest { get; init; }
		public PlannerFixtureSolveExpectation? SolveExpectation { get; init; }
		public PlannerFixtureShareExpectation? ShareExpectation { get; init; }
	}

	public sealed class PlannerFixtureUiState
	{
		public bool ShowDebugOutput { get; init; }
	}

	public sealed class PlannerFixtureSolveExpectation
	{
		public string ResultStatus { get; init; } = string.Empty;
		public List<string> ResultKeysPresent { get; init; } = [];
		public List<string> ResultKeysAbsent { get; init; } = [];
		public Dictionary<string, double> ResultValues { get; init; } = [];
		public PlannerFixtureDebugExpectation? Debug { get; init; }
	}

	public sealed class PlannerFixtureDebugExpectation
	{
		public string MessageContains { get; init; } = string.Empty;
		public string Item { get; init; } = string.Empty;
		public List<string> ReasonsContain { get; init; } = [];
	}

	public sealed class PlannerFixtureShareExpectation
	{
		public string CreateQueryVersion { get; init; } = string.Empty;
		public string ExpectedLinkPrefix { get; init; } = string.Empty;
		public string LoadedMetadataName { get; init; } = string.Empty;
		public string LoadedMetadataGameVersion { get; init; } = string.Empty;
		public string LoadedFirstProductionItem { get; init; } = string.Empty;
	}
}
