using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace SatisfactoryTools.Solver.Api.Tests;

internal static class TestApplicationFactoryExtensions
{
	public static HttpClient CreateConfiguredClient(this WebApplicationFactory<Program> factory, Dictionary<string, string?> settings)
	{
		return factory.WithWebHostBuilder((builder) =>
		{
			builder.ConfigureAppConfiguration((_, configuration) =>
			{
				configuration.AddInMemoryCollection(settings);
			});
		}).CreateClient();
	}

	public static HttpClient CreateShareClient(this WebApplicationFactory<Program> factory, string shareRoot)
	{
		return factory.CreateConfiguredClient(new Dictionary<string, string?>
		{
			["ShareStore:Root"] = shareRoot,
		});
	}

	public static HttpClient CreateFrontendClient(this WebApplicationFactory<Program> factory, string frontendRoot, string? solverUrl = null)
	{
		var settings = new Dictionary<string, string?>
		{
			["Frontend:Root"] = frontendRoot,
		};

		if (solverUrl is not null) {
			settings["SOLVER_URL"] = solverUrl;
		}

		return factory.CreateConfiguredClient(settings);
	}
}
