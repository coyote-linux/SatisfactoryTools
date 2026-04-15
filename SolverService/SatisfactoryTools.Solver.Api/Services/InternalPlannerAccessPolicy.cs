namespace SatisfactoryTools.Solver.Api.Services;

public sealed class InternalPlannerAccessPolicy
{
	public const string SameOriginError = "Internal planner route requires same-origin requests.";

	public bool TryAuthorize(HttpRequest request, out string error)
	{
		ArgumentNullException.ThrowIfNull(request);

		var origin = request.Headers.Origin.ToString();
		if (string.IsNullOrWhiteSpace(origin)) {
			error = SameOriginError;
			return false;
		}

		if (!Uri.TryCreate(origin, UriKind.Absolute, out var originUri)) {
			error = SameOriginError;
			return false;
		}

		var requestOrigin = GetRequestOrigin(request);
		if (!string.Equals(originUri.GetLeftPart(UriPartial.Authority), requestOrigin, StringComparison.OrdinalIgnoreCase)) {
			error = SameOriginError;
			return false;
		}

		error = string.Empty;
		return true;
	}

	private static string GetRequestOrigin(HttpRequest request)
	{
		return request.Scheme + "://" + request.Host.Value;
	}
}
