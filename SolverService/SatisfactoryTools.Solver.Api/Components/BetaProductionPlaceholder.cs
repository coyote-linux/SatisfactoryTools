using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace SatisfactoryTools.Solver.Api.Components;

internal sealed class BetaProductionPlaceholder : ComponentBase
{
	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		builder.OpenElement(0, "section");
		builder.AddAttribute(1, "id", "beta-production-placeholder");
		builder.AddAttribute(2, "data-testid", "beta-production-placeholder");

		builder.OpenElement(3, "h1");
		builder.AddContent(4, "Blazor beta planner placeholder");
		builder.CloseElement();

		builder.OpenElement(5, "p");
		builder.AddContent(6, "The M4 slice 1 host seam is active for ");
		builder.OpenElement(7, "code");
		builder.AddContent(8, "/beta/production");
		builder.CloseElement();
		builder.AddContent(9, ".");
		builder.CloseElement();

		builder.CloseElement();
	}
}
