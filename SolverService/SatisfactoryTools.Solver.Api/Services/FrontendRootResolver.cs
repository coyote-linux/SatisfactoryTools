namespace SatisfactoryTools.Solver.Api.Services;

internal static class FrontendRootResolver
{
	public static string Resolve(string contentRootPath, string? configuredPath)
	{
		if (!string.IsNullOrWhiteSpace(configuredPath)) {
			return ValidateRoot(Path.GetFullPath(configuredPath, contentRootPath));
		}

		var directCandidate = Path.Combine(contentRootPath, "www");
		if (HasShellTemplate(directCandidate)) {
			return directCandidate;
		}

		var directory = new DirectoryInfo(contentRootPath);
		while (directory is not null) {
			var candidate = Path.Combine(directory.FullName, "www");
			if (HasShellTemplate(candidate)) {
				return candidate;
			}

			directory = directory.Parent;
		}

		throw new InvalidOperationException("Unable to locate the frontend root containing www/index.php.");
	}

	private static bool HasShellTemplate(string candidate)
	{
		return File.Exists(Path.Combine(candidate, "index.php"));
	}

	private static string ValidateRoot(string candidate)
	{
		if (!HasShellTemplate(candidate)) {
			throw new InvalidOperationException($"Frontend root '{candidate}' does not contain index.php.");
		}

		return candidate;
	}
}
