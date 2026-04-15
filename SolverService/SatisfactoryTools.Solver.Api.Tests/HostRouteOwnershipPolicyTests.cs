using Microsoft.AspNetCore.Http;
using SatisfactoryTools.Solver.Api.Services;

namespace SatisfactoryTools.Solver.Api.Tests;

public sealed class HostRouteOwnershipPolicyTests
{
	private static readonly HostRouteOwnershipPolicy Policy = new();

	[Theory]
	[InlineData("GET", "/v2", HostRouteOwner.Api)]
	[InlineData("GET", "/v2/", HostRouteOwner.Api)]
	[InlineData("POST", "/v2/solver", HostRouteOwner.Api)]
	[InlineData("GET", "/v2/share/abc123", HostRouteOwner.Api)]
	[InlineData("POST", "/_internal/planner/calculate", HostRouteOwner.Api)]
	[InlineData("GET", "/_internal/planner/not-a-route", HostRouteOwner.Api)]
	[InlineData("GET", "/", HostRouteOwner.LegacyShell)]
	[InlineData("HEAD", "/1.2/production", HostRouteOwner.LegacyShell)]
	[InlineData("GET", "/1.0", HostRouteOwner.LegacyShell)]
	[InlineData("GET", "/1.0-ficsmas", HostRouteOwner.LegacyShell)]
	[InlineData("GET", "/1.1", HostRouteOwner.LegacyShell)]
	[InlineData("GET", "/1.1-ficsmas", HostRouteOwner.LegacyShell)]
	[InlineData("GET", "/1.2", HostRouteOwner.LegacyShell)]
	[InlineData("GET", "/assets/app.js", HostRouteOwner.StaticFileOrUnhandled)]
	[InlineData("GET", "/assets/missing.js", HostRouteOwner.StaticFileOrUnhandled)]
	[InlineData("POST", "/1.2/production", HostRouteOwner.StaticFileOrUnhandled)]
	[InlineData("GET", "/1.3", HostRouteOwner.StaticFileOrUnhandled)]
	public void ClassifiesRoutesWithoutChangingCurrentOwnershipRules(string method, string path, HostRouteOwner expectedOwner)
	{
		var owner = Policy.GetOwner(method, new PathString(path));

		Assert.Equal(expectedOwner, owner);
	}

	[Theory]
	[InlineData("/1.0", true)]
	[InlineData("/1.0-ficsmas", true)]
	[InlineData("/1.1", true)]
	[InlineData("/1.1-ficsmas", true)]
	[InlineData("/1.2", true)]
	[InlineData("/1.3", false)]
	[InlineData("/1.2/production", false)]
	public void BareVersionRootAllowListRemainsExplicit(string path, bool expected)
	{
		Assert.Equal(expected, Policy.IsSupportedBareVersionRoot(new PathString(path)));
	}

	[Fact]
	public void HttpRequestOverloadMatchesStringAndPathOverload()
	{
		var context = new DefaultHttpContext();
		context.Request.Method = HttpMethods.Get;
		context.Request.Path = "/1.2/production";

		Assert.Equal(Policy.GetOwner(HttpMethods.Get, new PathString("/1.2/production")), Policy.GetOwner(context.Request));
		Assert.True(Policy.IsLegacyShellFallbackEligible(context.Request));
	}
}
