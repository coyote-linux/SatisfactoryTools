using System.Collections.Concurrent;
using System.Text.Json;
using SatisfactoryTools.Solver.Api.Contracts;

namespace SatisfactoryTools.Solver.Api.Services;

public sealed class GameDataCatalog(IHostEnvironment environment, IConfiguration configuration)
{
	private readonly ConcurrentDictionary<string, GameDataDocument> cache = new();
	private readonly string dataRoot = ResolveDataRoot(environment.ContentRootPath, configuration["DataRoot"]);

	public GameDataDocument Get(string gameVersion)
	{
		var fileName = gameVersion switch
		{
			"1.0.0-ficsmas" => "data1.0-ficsmas.json",
			"1.0.0" or "1.1.0" or "1.2.0" or "0.8.0" => gameVersion == "0.8.0" ? "data.json" : "data1.0.json",
			_ => throw new SolverValidationException("Invalid version")
		};

		return cache.GetOrAdd(fileName, Load);
	}

	private GameDataDocument Load(string fileName)
	{
		var filePath = Path.Combine(dataRoot, fileName);
		if (!File.Exists(filePath)) {
			throw new SolverValidationException($"Missing data file '{fileName}'.");
		}

		using var stream = File.OpenRead(filePath);
		var data = JsonSerializer.Deserialize<GameDataDocument>(stream, SolverJson.Options);
		if (data is null) {
			throw new SolverValidationException($"Couldn't parse data file '{fileName}'.");
		}

		return data;
	}

	private static string ResolveDataRoot(string contentRootPath, string? configuredPath)
	{
		if (!string.IsNullOrWhiteSpace(configuredPath)) {
			return Path.GetFullPath(configuredPath, contentRootPath);
		}

		var directory = new DirectoryInfo(contentRootPath);
		while (directory is not null) {
			var candidate = Path.Combine(directory.FullName, "data", "data1.0.json");
			if (File.Exists(candidate)) {
				return Path.GetDirectoryName(candidate)!;
			}

			directory = directory.Parent;
		}

		throw new SolverValidationException("Unable to locate the repository data directory.");
	}
}
