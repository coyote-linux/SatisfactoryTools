namespace SatisfactoryTools.Solver.Api.Tests;

internal sealed class FrontendTestSite : IDisposable
{
	public const string BundleContents = "console.log('frontend test bundle');";
	private static readonly DateTimeOffset BundleTimestamp = DateTimeOffset.FromUnixTimeSeconds(1735689600);

	private FrontendTestSite(string rootPath)
	{
		RootPath = rootPath;
	}

	public string RootPath { get; }

	public long BundleVersion => BundleTimestamp.ToUnixTimeSeconds();

	public static FrontendTestSite Create()
	{
		var rootPath = Path.Combine(Path.GetTempPath(), "satisfactorytools-frontend-tests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(rootPath);
		Directory.CreateDirectory(Path.Combine(rootPath, "assets"));
		File.Copy(ResolveRepositoryShellTemplatePath(), Path.Combine(rootPath, "index.php"));

		var bundlePath = Path.Combine(rootPath, "assets", "app.js");
		File.WriteAllText(bundlePath, BundleContents);
		File.SetLastWriteTimeUtc(bundlePath, BundleTimestamp.UtcDateTime);

		return new FrontendTestSite(rootPath);
	}

	public void Dispose()
	{
		if (Directory.Exists(RootPath)) {
			Directory.Delete(RootPath, true);
		}
	}

	private static string ResolveRepositoryShellTemplatePath()
	{
		var directory = new DirectoryInfo(AppContext.BaseDirectory);
		while (directory is not null) {
			var candidate = Path.Combine(directory.FullName, "www", "index.php");
			if (File.Exists(candidate)) {
				return candidate;
			}

			directory = directory.Parent;
		}

		throw new InvalidOperationException("Unable to locate the repository shell template at www/index.php.");
	}
}
