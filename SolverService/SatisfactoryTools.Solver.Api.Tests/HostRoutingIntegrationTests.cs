using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SatisfactoryTools.Solver.Api.Tests;

public sealed class HostRoutingIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
	private readonly WebApplicationFactory<Program> factory;

	public HostRoutingIntegrationTests(WebApplicationFactory<Program> factory)
	{
		this.factory = factory;
	}

	[Fact]
	public async Task RootServesShellWithDefaultSolverUrlAndCacheBustedBundleReference()
	{
		using var frontendSite = FrontendTestSite.Create();
		using var frontendClient = factory.CreateFrontendClient(frontendSite.RootPath);

		var response = await frontendClient.GetAsync("/");
		response.EnsureSuccessStatusCode();
		Assert.Equal("text/html; charset=utf-8", response.Content.Headers.ContentType?.ToString());

		var html = await response.Content.ReadAsStringAsync();
		Assert.Contains("<base href=\"/\">", html, StringComparison.Ordinal);
		Assert.Contains("window.SATISFACTORY_TOOLS_CONFIG = {", html, StringComparison.Ordinal);
		Assert.Contains("solverUrl: \"/v2/solver\"", html, StringComparison.Ordinal);
		Assert.Contains($"/assets/app.js?v={frontendSite.BundleVersion}", html, StringComparison.Ordinal);
		Assert.DoesNotContain("<?=", html, StringComparison.Ordinal);
	}

	[Fact]
	public async Task DeepLinksServeShellAndPreserveRuntimeSolverOverrideInjection()
	{
		using var frontendSite = FrontendTestSite.Create();
		const string solverUrl = "https://solver.example.test/v2/solver";
		using var frontendClient = factory.CreateFrontendClient(frontendSite.RootPath, solverUrl: solverUrl);

		var response = await frontendClient.GetAsync("/1.2/production?share=abc123");
		response.EnsureSuccessStatusCode();

		var html = await response.Content.ReadAsStringAsync();
		Assert.Contains("<app></app>", html, StringComparison.Ordinal);
		Assert.Contains($"solverUrl: \"{solverUrl}\"", html, StringComparison.Ordinal);
	}

	[Theory]
	[InlineData("/1.0")]
	[InlineData("/1.0-ficsmas")]
	[InlineData("/1.1")]
	[InlineData("/1.1-ficsmas")]
	[InlineData("/1.2")]
	public async Task SupportedBareVersionRootsStillServeShell(string path)
	{
		using var frontendSite = FrontendTestSite.Create();
		using var frontendClient = factory.CreateFrontendClient(frontendSite.RootPath);

		var response = await frontendClient.GetAsync(path);
		response.EnsureSuccessStatusCode();

		var html = await response.Content.ReadAsStringAsync();
		Assert.Contains("<!DOCTYPE html>", html, StringComparison.Ordinal);
		Assert.Contains("<app></app>", html, StringComparison.Ordinal);
	}

	[Fact]
	public async Task EmptySolverUrlOverrideFallsBackToDefaultSolverPath()
	{
		using var frontendSite = FrontendTestSite.Create();
		using var frontendClient = factory.CreateFrontendClient(frontendSite.RootPath, solverUrl: string.Empty);

		var response = await frontendClient.GetAsync("/");
		response.EnsureSuccessStatusCode();

		var html = await response.Content.ReadAsStringAsync();
		Assert.Contains("solverUrl: \"/v2/solver\"", html, StringComparison.Ordinal);
	}

	[Fact]
	public async Task ConfiguredFrontendRootServesStaticAssetsFromAssetTree()
	{
		using var frontendSite = FrontendTestSite.Create();
		using var frontendClient = factory.CreateFrontendClient(frontendSite.RootPath);

		var response = await frontendClient.GetAsync("/assets/app.js");
		response.EnsureSuccessStatusCode();
		Assert.Contains("javascript", response.Content.Headers.ContentType?.MediaType ?? string.Empty, StringComparison.OrdinalIgnoreCase);

		var script = await response.Content.ReadAsStringAsync();
		Assert.Equal(FrontendTestSite.BundleContents, script);
	}

	[Fact]
	public async Task UnknownV2RouteDoesNotFallBackToShellHtml()
	{
		using var frontendSite = FrontendTestSite.Create();
		using var frontendClient = factory.CreateFrontendClient(frontendSite.RootPath);

		var response = await frontendClient.GetAsync("/v2/not-a-route");
		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

		var body = await response.Content.ReadAsStringAsync();
		Assert.DoesNotContain("<!DOCTYPE html>", body, StringComparison.Ordinal);
	}

	[Fact]
	public async Task MissingAssetPathDoesNotFallBackToShellHtml()
	{
		using var frontendSite = FrontendTestSite.Create();
		using var frontendClient = factory.CreateFrontendClient(frontendSite.RootPath);

		var response = await frontendClient.GetAsync("/assets/missing.js");
		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

		var body = await response.Content.ReadAsStringAsync();
		Assert.DoesNotContain("<!DOCTYPE html>", body, StringComparison.Ordinal);
	}

	[Fact]
	public async Task HeadDeepLinkRemainsShellFallbackEligible()
	{
		using var frontendSite = FrontendTestSite.Create();
		using var frontendClient = factory.CreateFrontendClient(frontendSite.RootPath);

		using var request = new HttpRequestMessage(HttpMethod.Head, "/1.2/production");
		var response = await frontendClient.SendAsync(request);

		response.EnsureSuccessStatusCode();
		Assert.Equal("text/html; charset=utf-8", response.Content.Headers.ContentType?.ToString());
	}
}
