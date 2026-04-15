using System.Globalization;
using System.Text.Json;

namespace SatisfactoryTools.Solver.Api.Services;

internal sealed class SpaShellRenderer(IHostEnvironment environment, IConfiguration configuration)
{
	private const string SolverUrlPlaceholder = "<?= json_encode(getenv('SOLVER_URL') ?: '/v2/solver') ?>";
	private const string UseInternalPlannerCalculatePlaceholder = "<?= json_encode(filter_var(getenv('USE_INTERNAL_PLANNER_CALCULATE') ?: false, FILTER_VALIDATE_BOOLEAN)) ?>";
	private const string InternalPlannerCalculateUrlPlaceholder = "<?= json_encode(getenv('INTERNAL_PLANNER_CALCULATE_URL') ?: '/_internal/planner/calculate') ?>";
	private const string AssetVersionPlaceholder = "<?= filemtime(__DIR__ . '/assets/app.js') ?>";
	private readonly string solverUrl = string.IsNullOrWhiteSpace(configuration["SOLVER_URL"]) ? "/v2/solver" : configuration["SOLVER_URL"]!;
	private readonly bool useInternalPlannerCalculate = configuration.GetValue<bool>("Planner:UseInternalCalculate");
	private readonly string internalPlannerCalculateUrl = string.IsNullOrWhiteSpace(configuration["Planner:InternalCalculateUrl"]) ? "/_internal/planner/calculate" : configuration["Planner:InternalCalculateUrl"]!;

	public string FrontendRoot { get; } = FrontendRootResolver.Resolve(environment.ContentRootPath, configuration["Frontend:Root"] ?? configuration["FrontendRoot"]);

	public async Task<string> RenderAsync(CancellationToken cancellationToken)
	{
		var indexPath = Path.Combine(FrontendRoot, "index.php");
		var shell = await File.ReadAllTextAsync(indexPath, cancellationToken);
		var rendered = shell
			.Replace(SolverUrlPlaceholder, JsonSerializer.Serialize(solverUrl), StringComparison.Ordinal)
			.Replace(UseInternalPlannerCalculatePlaceholder, JsonSerializer.Serialize(useInternalPlannerCalculate), StringComparison.Ordinal)
			.Replace(InternalPlannerCalculateUrlPlaceholder, JsonSerializer.Serialize(internalPlannerCalculateUrl), StringComparison.Ordinal)
			.Replace(AssetVersionPlaceholder, ResolveAssetVersion(), StringComparison.Ordinal);

		if (rendered.Contains("<?", StringComparison.Ordinal)) {
			throw new InvalidOperationException($"Shell template '{indexPath}' contains unsupported PHP fragments.");
		}

		return rendered;
	}

	private string ResolveAssetVersion()
	{
		var appBundlePath = Path.Combine(FrontendRoot, "assets", "app.js");
		if (!File.Exists(appBundlePath)) {
			return "0";
		}

		var lastWriteUtc = File.GetLastWriteTimeUtc(appBundlePath);
		return new DateTimeOffset(lastWriteUtc).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
	}
}
