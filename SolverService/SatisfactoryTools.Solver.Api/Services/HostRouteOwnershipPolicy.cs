namespace SatisfactoryTools.Solver.Api.Services;

public enum HostRouteOwner
{
	Api,
	LegacyShell,
	StaticFileOrUnhandled,
}

public sealed class HostRouteOwnershipPolicy
{
	public HostRouteOwner GetOwner(HttpRequest request)
	{
		ArgumentNullException.ThrowIfNull(request);
		return GetOwner(request.Method, request.Path);
	}

	public HostRouteOwner GetOwner(string? method, PathString requestPath)
	{
		var requestMethod = method ?? string.Empty;

		if (requestPath.StartsWithSegments("/v2", StringComparison.OrdinalIgnoreCase)
			|| requestPath.StartsWithSegments("/_internal/planner", StringComparison.OrdinalIgnoreCase)) {
			return HostRouteOwner.Api;
		}

		if (!HttpMethods.IsGet(requestMethod) && !HttpMethods.IsHead(requestMethod)) {
			return HostRouteOwner.StaticFileOrUnhandled;
		}

		if (Path.HasExtension(requestPath.Value) && !IsSupportedBareVersionRoot(requestPath)) {
			return HostRouteOwner.StaticFileOrUnhandled;
		}

		return HostRouteOwner.LegacyShell;
	}

	public bool IsLegacyShellFallbackEligible(HttpRequest request)
	{
		return GetOwner(request) == HostRouteOwner.LegacyShell;
	}

	public bool IsSupportedBareVersionRoot(PathString requestPath)
	{
		return requestPath.Value is "/1.0" or "/1.0-ficsmas" or "/1.1" or "/1.1-ficsmas" or "/1.2";
	}
}
