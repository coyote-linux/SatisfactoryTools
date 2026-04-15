using System.Net.Http.Json;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace SatisfactoryTools.Solver.Api.Tests;

public sealed class PlannerBrowserRegressionTests : PageTest, IClassFixture<BrowserRegressionWebApplicationFactory>
{
	private readonly BrowserRegressionWebApplicationFactory factory;

	public PlannerBrowserRegressionTests(BrowserRegressionWebApplicationFactory factory)
	{
		this.factory = factory;
	}

	public override BrowserNewContextOptions ContextOptions()
	{
		return new BrowserNewContextOptions
		{
			BaseURL = factory.ServerAddress.ToString(),
		};
	}

	[Fact]
	public async Task GuardedSharedEntryWithEmptyStoredTabsLoadsSingleSharedTabAndUsesInternalPlannerRoute()
	{
		var fixture = PlannerFixtureSupport.LoadPlannerFixture("F004");
		var requestLog = new List<string>();
		TrackRelevantRequests(requestLog);

		await Page.AddInitScriptAsync($"() => window.localStorage.setItem({SerializeForJavaScript(fixture.StorageKey)}, '[]');");
		await Page.GotoAsync(await CreateShareLinkAsync(fixture), new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

		await Expect(Page.Locator(".production-line-name-title span")).ToHaveTextAsync("Shared: Shared Motor Plan");
		await Page.WaitForFunctionAsync("() => !window.location.search.includes('share=')");
		await Page.Locator(".visualization-result-container").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });

		Assert.Equal(3, await Page.Locator(".production-nav-tabs > li").CountAsync());
		Assert.DoesNotContain("share=", new Uri(Page.Url).Query, StringComparison.Ordinal);

		var configKeys = await Page.EvaluateAsync<string[]>("() => Object.keys(window.SATISFACTORY_TOOLS_CONFIG || {})");
		Assert.Contains("useInternalPlannerCalculate", configKeys);
		Assert.DoesNotContain("internalPlannerCalculateUrl", configKeys);

		Assert.Contains(requestLog, (entry) => entry.StartsWith("GET /v2/share/", StringComparison.Ordinal));
		Assert.Contains(requestLog, (entry) => entry.StartsWith("POST /_internal/planner/calculate", StringComparison.Ordinal));
		Assert.DoesNotContain(requestLog, (entry) => entry.StartsWith("POST /v2/solver", StringComparison.Ordinal));
	}

	[Fact]
	public async Task GuardedVisualizationUsesVisualizationPayloadAndAppliesDistinctNodePositions()
	{
		var fixture = PlannerFixtureSupport.LoadPlannerFixture("F004");
		await Page.GotoAsync(await CreateShareLinkAsync(fixture), new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

		await Expect(Page.Locator(".production-line-name-title span")).ToHaveTextAsync("Shared: Shared Motor Plan");
		await Expect(Page.Locator("visualization")).ToBeVisibleAsync();
		await Page.WaitForFunctionAsync(@"() => {
			const element = document.querySelector('visualization');
			if (!element || !window.angular) {
				return false;
			}

			const ngElement = window.angular.element(element);
			const scope = typeof ngElement.isolateScope === 'function' ? ngElement.isolateScope() : undefined;
			const controller = (typeof ngElement.controller === 'function' ? ngElement.controller('visualization') : undefined) || scope?.$ctrl;
			const positions = controller?.network?.getPositions?.() || {};
			const entries = Object.values(positions);
			const distinct = new Set(entries.map((position) => `${Math.round(position.x)}:${Math.round(position.y)}`));
			return !!scope?.$ctrl?.result?.visualization && !scope?.$ctrl?.result?.graph && entries.length > 1 && distinct.size > 1;
		}");

		var snapshot = await Page.EvaluateAsync<VisualizationSnapshot>(@"() => {
			const element = document.querySelector('visualization');
			if (!element || !window.angular) {
				return { hasVisualization: false, hasGraph: false, nodeCount: 0, distinctPositionCount: 0 };
			}

			const ngElement = window.angular.element(element);
			const scope = typeof ngElement.isolateScope === 'function' ? ngElement.isolateScope() : undefined;
			const controller = (typeof ngElement.controller === 'function' ? ngElement.controller('visualization') : undefined) || scope?.$ctrl;
			const positions = controller?.network?.getPositions?.() || {};
			const entries = Object.values(positions);
			return {
				hasVisualization: !!scope?.$ctrl?.result?.visualization,
				hasGraph: !!scope?.$ctrl?.result?.graph,
				nodeCount: entries.length,
				distinctPositionCount: new Set(entries.map((position) => `${Math.round(position.x)}:${Math.round(position.y)}`)).size,
			};
		}");

		Assert.True(snapshot.HasVisualization);
		Assert.False(snapshot.HasGraph);
		Assert.True(snapshot.NodeCount > 1);
		Assert.True(snapshot.DistinctPositionCount > 1);
	}

	[Fact]
	public async Task GuardedNoResultFlowShowsDebugOutputAndStaysOnInternalPlannerRoute()
	{
		var fixture = PlannerFixtureSupport.LoadPlannerFixture("F007");
		var requestLog = new List<string>();
		TrackRelevantRequests(requestLog);

		await Page.GotoAsync(await CreateShareLinkAsync(fixture), new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
		await Expect(Page.Locator(".production-line-name-title span")).ToHaveTextAsync("Shared: Fixture F007 - Turbofuel Debug");
		await Expect(Page.Locator(".visualization-result").First).ToContainTextAsync("Unfortunately we couldn't calculate any result.");

		var debugToggle = Page.Locator("label:has-text('Debug') input[type='checkbox']");
		await debugToggle.CheckAsync();
		await Page.Locator(".production-input-table tbody tr").First.Locator("input[type='number']").FillAsync("2");

		await Expect(Page.Locator(".solver-debug-output")).ToContainTextAsync("feasible");
		Assert.False(await Page.Locator(".visualization-result-container").IsVisibleAsync());

		Assert.Contains(requestLog, (entry) => entry.StartsWith("POST /_internal/planner/calculate", StringComparison.Ordinal) && entry.Contains("showDebugOutput=true", StringComparison.Ordinal));
		Assert.DoesNotContain(requestLog, (entry) => entry.StartsWith("POST /v2/solver", StringComparison.Ordinal));
	}

	private async Task<string> CreateShareLinkAsync(PlannerFixtureSupport.PlannerFixture fixture)
	{
		using var client = factory.CreateClient();
		var response = await client.PostAsJsonAsync(
			"/v2/share/?version=" + Uri.EscapeDataString(fixture.RouteVersion),
			fixture.PlannerState);
		response.EnsureSuccessStatusCode();

		var payload = await response.Content.ReadFromJsonAsync<ShareCreateResponse>();
		return payload?.Link ?? throw new InvalidOperationException($"Share creation for fixture '{fixture.Id}' did not return a link.");
	}

	private void TrackRelevantRequests(List<string> requestLog)
	{
		Page.Request += (_, request) =>
		{
			var uri = new Uri(request.Url);
			if (!uri.AbsolutePath.StartsWith("/_internal/", StringComparison.Ordinal)
				&& !uri.AbsolutePath.StartsWith("/v2/", StringComparison.Ordinal)) {
				return;
			}

			requestLog.Add(request.Method + " " + uri.AbsolutePath + uri.Query);
		};
	}

	private static string SerializeForJavaScript(string value)
	{
		return System.Text.Json.JsonSerializer.Serialize(value);
	}

	private sealed class ShareCreateResponse
	{
		public string? Link { get; init; }
	}

	private sealed class VisualizationSnapshot
	{
		public bool HasVisualization { get; init; }
		public bool HasGraph { get; init; }
		public int NodeCount { get; init; }
		public int DistinctPositionCount { get; init; }
	}
}
