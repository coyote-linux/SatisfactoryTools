using System.Text.Json;
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

app.Run();

public partial class Program;
