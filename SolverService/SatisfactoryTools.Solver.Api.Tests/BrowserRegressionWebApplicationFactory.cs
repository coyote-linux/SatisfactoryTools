using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace SatisfactoryTools.Solver.Api.Tests;

public sealed class BrowserRegressionWebApplicationFactory : IDisposable
{
	private static readonly Regex ListeningAddressPattern = new(@"Now listening on:\s+(https?://\S+)", RegexOptions.Compiled);
	private readonly string shareRoot;
	private readonly Process hostProcess;
	private readonly StringBuilder hostOutput = new();
	private bool disposed;

	public BrowserRegressionWebApplicationFactory()
		: this(null)
	{
	}

	public static BrowserRegressionWebApplicationFactory Create(bool? useInternalPlannerCalculate)
	{
		return new BrowserRegressionWebApplicationFactory(useInternalPlannerCalculate);
	}

	private BrowserRegressionWebApplicationFactory(bool? useInternalPlannerCalculate)
	{
		var frontendRoot = ResolveRepositoryFrontendRoot();
		var repositoryRoot = Directory.GetParent(frontendRoot)?.FullName ?? throw new InvalidOperationException($"Unable to resolve the repository root from frontend path '{frontendRoot}'.");
		shareRoot = Path.Combine(Path.GetTempPath(), "satisfactorytools-browser-share-tests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(shareRoot);

		var serverAddressSource = new TaskCompletionSource<Uri>(TaskCreationOptions.RunContinuationsAsynchronously);
		hostProcess = StartHostProcess(repositoryRoot, frontendRoot, shareRoot, useInternalPlannerCalculate, serverAddressSource);
		ServerAddress = WaitForServerAddress(serverAddressSource.Task);
	}

	public Uri ServerAddress { get; }

	public HttpClient CreateClient()
	{
		ThrowIfDisposed();
		return new HttpClient { BaseAddress = ServerAddress };
	}

	public void Dispose()
	{
		if (disposed) {
			return;
		}

		disposed = true;

		if (!hostProcess.HasExited) {
			hostProcess.Kill(true);
			hostProcess.WaitForExit(5000);
		}

		hostProcess.Dispose();

		if (Directory.Exists(shareRoot)) {
			Directory.Delete(shareRoot, true);
		}
	}

	private Process StartHostProcess(string repositoryRoot, string frontendRoot, string shareRoot, bool? useInternalPlannerCalculate, TaskCompletionSource<Uri> serverAddressSource)
	{
		var appDllPath = Path.Combine(AppContext.BaseDirectory, "SatisfactoryTools.Solver.Api.dll");
		if (!File.Exists(appDllPath)) {
			throw new InvalidOperationException($"Unable to locate the solver host assembly at '{appDllPath}'.");
		}

		var process = new Process
		{
			StartInfo = new ProcessStartInfo
			{
				FileName = "dotnet",
				Arguments = '"' + appDllPath + '"' + " --urls http://127.0.0.1:0",
				WorkingDirectory = repositoryRoot,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
			},
			EnableRaisingEvents = true,
		};

		process.StartInfo.Environment["Frontend__Root"] = frontendRoot;
		process.StartInfo.Environment["ShareStore__Root"] = shareRoot;
		process.StartInfo.Environment.Remove("SOLVER_URL");
		process.StartInfo.Environment.Remove("Planner__UseInternalCalculate");
		process.StartInfo.Environment.Remove("Planner:UseInternalCalculate");
		if (useInternalPlannerCalculate.HasValue) {
			process.StartInfo.Environment["Planner__UseInternalCalculate"] = useInternalPlannerCalculate.Value ? "true" : "false";
		}

		process.StartInfo.Environment["ASPNETCORE_CONTENTROOT"] = repositoryRoot;

		process.OutputDataReceived += (_, args) => HandleHostOutput(args.Data, serverAddressSource);
		process.ErrorDataReceived += (_, args) => HandleHostOutput(args.Data, serverAddressSource);
		process.Exited += (_, _) =>
		{
			if (!serverAddressSource.Task.IsCompleted) {
				serverAddressSource.TrySetException(new InvalidOperationException($"Browser regression host exited before announcing a listening address. Output:{Environment.NewLine}{hostOutput}"));
			}
		};

		if (!process.Start()) {
			throw new InvalidOperationException("Failed to start the browser regression host process.");
		}

		process.BeginOutputReadLine();
		process.BeginErrorReadLine();
		return process;
	}

	private void HandleHostOutput(string? line, TaskCompletionSource<Uri> serverAddressSource)
	{
		if (string.IsNullOrWhiteSpace(line)) {
			return;
		}

		lock (hostOutput) {
			hostOutput.AppendLine(line);
		}

		var match = ListeningAddressPattern.Match(line);
		if (match.Success) {
			serverAddressSource.TrySetResult(new Uri(match.Groups[1].Value));
		}
	}

	private Uri WaitForServerAddress(Task<Uri> serverAddressTask)
	{
		if (serverAddressTask.Wait(TimeSpan.FromSeconds(30))) {
			return serverAddressTask.GetAwaiter().GetResult();
		}

		throw new TimeoutException($"Timed out waiting for the browser regression host to announce a listening address. Output so far:{Environment.NewLine}{hostOutput}");
	}

	private void ThrowIfDisposed()
	{
		ObjectDisposedException.ThrowIf(disposed, this);
	}

	private static string ResolveRepositoryFrontendRoot([CallerFilePath] string sourceFilePath = "")
	{
		var sourceDirectory = Path.GetDirectoryName(sourceFilePath);
		if (!string.IsNullOrWhiteSpace(sourceDirectory)) {
			var repositoryRoot = Path.GetFullPath(Path.Combine(sourceDirectory, "..", ".."));
			var frontendRoot = Path.Combine(repositoryRoot, "www");
			var bundlePath = Path.Combine(frontendRoot, "assets", "app.js");
			if (File.Exists(Path.Combine(repositoryRoot, "package.json")) && Directory.Exists(frontendRoot)) {
				if (!File.Exists(bundlePath)) {
					throw new InvalidOperationException($"Browser regression tests require a built frontend bundle at '{bundlePath}'. Run 'yarn build' before 'dotnet test'.");
				}

				return frontendRoot;
			}
		}

		throw new InvalidOperationException("Unable to locate the repository frontend root for browser regression tests.");
	}
}
