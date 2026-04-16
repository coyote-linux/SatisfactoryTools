using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using SatisfactoryTools.Solver.Api.Contracts;

namespace SatisfactoryTools.Solver.Api.Services;

public sealed class ShareStore
{
	private static readonly Regex ShareIdPattern = new("^[A-Za-z0-9_-]{16}$", RegexOptions.Compiled);

	private readonly string shareRoot;

	public ShareStore(IConfiguration configuration)
	{
		shareRoot = configuration["ShareStore:Root"] ?? Path.Combine(AppContext.BaseDirectory, "share-data");
		Directory.CreateDirectory(shareRoot);
	}

	public async Task<string> SaveAsync(JsonNode payload, CancellationToken cancellationToken)
	{
		for (var attempt = 0; attempt < 5; attempt++) {
			var shareId = GenerateShareId();
			var filePath = GetSharePath(shareId);
			if (File.Exists(filePath)) {
				continue;
			}

			await File.WriteAllTextAsync(filePath, payload.ToJsonString(SolverJson.Options), cancellationToken);
			return shareId;
		}

		throw new ShareValidationException("Could not create share id.");
	}

	public async Task<JsonNode> LoadAsync(string shareId, CancellationToken cancellationToken)
	{
		var normalizedId = NormalizeShareId(shareId);
		var filePath = GetSharePath(normalizedId);
		if (!File.Exists(filePath)) {
			throw new FileNotFoundException("Share not found.", filePath);
		}

		await using var stream = File.OpenRead(filePath);
		var payload = await JsonNode.ParseAsync(stream, cancellationToken: cancellationToken);
		if (payload is null) {
			throw new JsonException("Stored share payload is invalid.");
		}

		return payload;
	}

	private string GetSharePath(string shareId)
	{
		return Path.Combine(shareRoot, shareId + ".json");
	}

	private static string GenerateShareId()
	{
		Span<byte> randomBytes = stackalloc byte[12];
		RandomNumberGenerator.Fill(randomBytes);
		return Convert.ToBase64String(randomBytes).Replace('+', '-').Replace('/', '_');
	}

	private static string NormalizeShareId(string shareId)
	{
		if (!ShareIdPattern.IsMatch(shareId)) {
			throw new ShareValidationException("Invalid share id.");
		}

		return shareId;
	}
}
