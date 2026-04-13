using System.Text.Json;
using System.Text.Json.Nodes;
using SatisfactoryTools.Solver.Api.Contracts;
using SatisfactoryTools.Solver.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors((options) =>
{
	options.AddDefaultPolicy((policy) =>
	{
		policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
	});
});

builder.Services.AddSingleton<GameDataCatalog>();
builder.Services.AddSingleton<ProductionPlannerSolver>();
builder.Services.AddSingleton<ShareStore>();

var app = builder.Build();

app.UseCors();

app.MapGet("/v2/", () => Results.Json(new {code = 200, active = true}));

app.MapPost("/v2/solver", async (HttpRequest request, ProductionPlannerSolver solver, CancellationToken cancellationToken) =>
{
	try {
		var payload = await JsonSerializer.DeserializeAsync<SolverRequest>(request.Body, SolverJson.Options, cancellationToken);
		if (payload is null) {
			throw new SolverValidationException("Invalid payload.");
		}

		var result = solver.Solve(payload);
		return Results.Json(new {code = 200, result});
	} catch (SolverValidationException exception) {
		return Results.Json(new {code = 500, error = exception.Message}, statusCode: 500);
	} catch (JsonException exception) {
		return Results.Json(new {code = 500, error = exception.Message}, statusCode: 500);
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

app.Run();

public partial class Program;
