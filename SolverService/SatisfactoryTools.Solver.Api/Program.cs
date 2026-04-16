using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using SatisfactoryTools.Solver.Api.Contracts;
using SatisfactoryTools.Solver.Api.Components;
using SatisfactoryTools.Solver.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors((options) =>
{
	options.AddDefaultPolicy((policy) =>
	{
		policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
	});
});

builder.Services.Configure<ForwardedHeadersOptions>((options) =>
{
	options.ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
});

builder.Services.AddRazorComponents();

builder.Services.AddSingleton<GameDataCatalog>();
builder.Services.AddSingleton<HostRouteOwnershipPolicy>();
builder.Services.AddSingleton<InternalPlannerAccessPolicy>();
builder.Services.AddSingleton<SpaShellRenderer>();
builder.Services.AddSingleton<ProductionPlannerSolver>();
builder.Services.AddSingleton<PlannerCompatibilityService>();
builder.Services.AddSingleton<PlannerResultDomainFactory>();
builder.Services.AddSingleton<PlannerResultCompositionService>();
builder.Services.AddSingleton<InternalPlannerCalculationService>();
builder.Services.AddSingleton<ShareStore>();

var app = builder.Build();
var hostRouteOwnershipPolicy = app.Services.GetRequiredService<HostRouteOwnershipPolicy>();
var shellRenderer = app.Services.GetRequiredService<SpaShellRenderer>();

app.UseForwardedHeaders();
app.UseCors();
app.UseStaticFiles(new StaticFileOptions
{
	FileProvider = new PhysicalFileProvider(shellRenderer.FrontendRoot),
	ContentTypeProvider = CreateContentTypeProvider(),
});

app.Use(async (HttpContext context, Func<Task> next) =>
{
	await next();

	if (context.Response.HasStarted || context.Response.StatusCode != StatusCodes.Status404NotFound) {
		return;
	}

	if (!hostRouteOwnershipPolicy.IsLegacyShellFallbackEligible(context.Request)) {
		return;
	}

	context.Response.StatusCode = StatusCodes.Status200OK;
	context.Response.ContentType = "text/html; charset=utf-8";
	await context.Response.WriteAsync(await shellRenderer.RenderAsync(context.RequestAborted), context.RequestAborted);
});

app.MapGet("/v2/", () => Results.Json(new {code = 200, active = true}));

app.MapGet("/beta/production", Results<NotFound, RazorComponentResult<BetaProductionPlaceholder>> (IConfiguration configuration) =>
	configuration.GetValue("Planner:BetaRouteEnabled", false)
		? new RazorComponentResult<BetaProductionPlaceholder>()
		: TypedResults.NotFound());

app.MapPost("/v2/solver", async (HttpRequest request, ProductionPlannerSolver solver, CancellationToken cancellationToken) =>
{
	try {
		var payload = await JsonSerializer.DeserializeAsync<SolverRequest>(request.Body, SolverJson.Options, cancellationToken);
		if (payload is null) {
			throw new SolverValidationException("Invalid payload.");
		}

		var execution = solver.Solve(payload);
		return Results.Json(new {code = 200, result = execution.Result, debug = execution.Debug});
	} catch (SolverValidationException exception) {
		return Results.Json(new {code = 500, error = exception.Message}, statusCode: 500);
	} catch (JsonException exception) {
		return Results.Json(new {code = 500, error = exception.Message}, statusCode: 500);
	}
});

app.MapPost("/_internal/planner/calculate", async (HttpRequest request, InternalPlannerAccessPolicy accessPolicy, InternalPlannerCalculationService calculationService, bool? showDebugOutput, CancellationToken cancellationToken) =>
{
	if (!accessPolicy.TryAuthorize(request, out var accessError)) {
		return Results.Json(new { error = accessError }, statusCode: StatusCodes.Status403Forbidden);
	}

	var includeDebug = showDebugOutput ?? false;

	try {
		var payload = await JsonSerializer.DeserializeAsync<PlannerState>(request.Body, SolverJson.Options, cancellationToken);
		if (payload is null) {
			return Results.BadRequest(new { error = "Invalid planner payload." });
		}

		var outcome = calculationService.Calculate(payload, includeDebug);
		return Results.Json(InternalPlannerCalculationResponse.FromOutcome(outcome, includeDebug), SolverJson.InternalPlannerResponseOptions);
	} catch (SolverValidationException) {
		return Results.BadRequest(new { error = "Unable to calculate planner result." });
	} catch (JsonException) {
		return Results.BadRequest(new { error = "Invalid planner payload." });
	}
});

app.MapPost("/v2/share/", async (HttpRequest request, ShareStore shareStore, CancellationToken cancellationToken) =>
{
	try {
		var payload = await JsonNode.ParseAsync(request.Body, cancellationToken: cancellationToken);
		if (payload is not JsonObject sharePayload) {
			throw new ShareValidationException("Invalid share payload.");
		}

		if (sharePayload["metadata"] is not JsonObject || sharePayload["request"] is not JsonObject) {
			throw new ShareValidationException("Share payload must include metadata and request.");
		}

		var shareId = await shareStore.SaveAsync(sharePayload, cancellationToken);
		var requestedVersion = request.Query["version"].FirstOrDefault();
		var metadataVersion = sharePayload["metadata"]?["gameVersion"]?.GetValue<string>();
		var version = string.IsNullOrWhiteSpace(requestedVersion) ? metadataVersion : requestedVersion;
		version ??= "1.1";

		return Results.Json(new {code = 200, link = $"/{version}/production?share={shareId}"});
	} catch (ShareValidationException exception) {
		return Results.Json(new {code = 400, error = exception.Message}, statusCode: 400);
	} catch (JsonException exception) {
		return Results.Json(new {code = 400, error = exception.Message}, statusCode: 400);
	}
});

app.MapGet("/v2/share/{shareId}", async (string shareId, ShareStore shareStore, CancellationToken cancellationToken) =>
{
	try {
		var payload = await shareStore.LoadAsync(shareId, cancellationToken);
		return Results.Json(new {code = 200, data = payload});
	} catch (FileNotFoundException) {
		return Results.Json(new {code = 404, error = "Share not found."}, statusCode: 404);
	} catch (ShareValidationException exception) {
		return Results.Json(new {code = 400, error = exception.Message}, statusCode: 400);
	} catch (JsonException exception) {
		return Results.Json(new {code = 500, error = exception.Message}, statusCode: 500);
	}
});

app.MapGet("/index.php", async (SpaShellRenderer renderer, CancellationToken cancellationToken) =>
	Results.Content(await renderer.RenderAsync(cancellationToken), "text/html; charset=utf-8"));

static FileExtensionContentTypeProvider CreateContentTypeProvider()
{
	var provider = new FileExtensionContentTypeProvider();
	provider.Mappings[".webmanifest"] = "application/manifest+json";
	return provider;
}

app.Run();

public partial class Program;
