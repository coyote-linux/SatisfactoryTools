using Microsoft.AspNetCore.Http;
using SatisfactoryTools.Solver.Api.Services;

namespace SatisfactoryTools.Solver.Api.Tests;

public sealed class InternalPlannerAccessPolicyTests
{
	private static readonly InternalPlannerAccessPolicy Policy = new();

	[Fact]
	public void AllowsMatchingOrigin()
	{
		var request = CreateRequest("https", "planner.example.test", "https://planner.example.test");

		var allowed = Policy.TryAuthorize(request, out var error);

		Assert.True(allowed);
		Assert.Equal(string.Empty, error);
	}

	[Fact]
	public void AllowsMatchingOriginWithExplicitPort()
	{
		var request = CreateRequest("http", "127.0.0.1:54321", "http://127.0.0.1:54321");

		var allowed = Policy.TryAuthorize(request, out var error);

		Assert.True(allowed);
		Assert.Equal(string.Empty, error);
	}

	[Fact]
	public void RejectsMissingOrigin()
	{
		var request = CreateRequest("https", "planner.example.test", null);

		var allowed = Policy.TryAuthorize(request, out var error);

		Assert.False(allowed);
		Assert.Equal(InternalPlannerAccessPolicy.SameOriginError, error);
	}

	[Fact]
	public void RejectsMalformedOrigin()
	{
		var request = CreateRequest("https", "planner.example.test", "not-a-uri");

		var allowed = Policy.TryAuthorize(request, out var error);

		Assert.False(allowed);
		Assert.Equal(InternalPlannerAccessPolicy.SameOriginError, error);
	}

	[Fact]
	public void RejectsCrossOriginRequest()
	{
		var request = CreateRequest("https", "planner.example.test", "https://attacker.example.test");

		var allowed = Policy.TryAuthorize(request, out var error);

		Assert.False(allowed);
		Assert.Equal(InternalPlannerAccessPolicy.SameOriginError, error);
	}

	private static HttpRequest CreateRequest(string scheme, string host, string? origin)
	{
		var context = new DefaultHttpContext();
		context.Request.Scheme = scheme;
		context.Request.Host = new HostString(host);
		if (origin is not null) {
			context.Request.Headers.Origin = origin;
		}

		return context.Request;
	}
}
