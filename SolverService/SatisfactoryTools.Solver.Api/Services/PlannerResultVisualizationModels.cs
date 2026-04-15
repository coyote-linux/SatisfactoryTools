using System.Text.Json.Serialization;

namespace SatisfactoryTools.Solver.Api.Services;

public sealed class PlannerResultVisualization
{
	public List<PlannerVisualizationNode> Nodes { get; init; } = [];
	public List<PlannerVisualizationEdge> Edges { get; init; } = [];
	public PlannerElkGraph ElkGraph { get; init; } = new();
}

public sealed class PlannerVisualizationNode
{
	public int Id { get; init; }
	public string Label { get; init; } = string.Empty;
	public string? Title { get; init; }
	public PlannerVisualizationNodeColor? Color { get; init; }
	public PlannerVisualizationFont? Font { get; init; }
}

public sealed class PlannerVisualizationNodeColor
{
	public string Border { get; init; } = string.Empty;
	public string Background { get; init; } = string.Empty;
	public PlannerVisualizationHighlightColor Highlight { get; init; } = new();
}

public sealed class PlannerVisualizationHighlightColor
{
	public string Border { get; init; } = string.Empty;
	public string Background { get; init; } = string.Empty;
}

public sealed class PlannerVisualizationFont
{
	public string Color { get; init; } = string.Empty;
}

public sealed class PlannerVisualizationEdge
{
	public int Id { get; init; }
	public int From { get; init; }
	public int To { get; init; }
	public string Label { get; init; } = string.Empty;
	public PlannerVisualizationEdgeColor Color { get; init; } = new();
	public PlannerVisualizationFont Font { get; init; } = new();
	public PlannerVisualizationSmooth Smooth { get; init; } = new();
}

public sealed class PlannerVisualizationEdgeColor
{
	public string Color { get; init; } = string.Empty;
	public string Highlight { get; init; } = string.Empty;
}

public sealed class PlannerVisualizationSmooth
{
	public bool Enabled { get; init; }
	public string? Type { get; init; }
	public double? Roundness { get; init; }
}

public sealed class PlannerElkGraph
{
	public string Id { get; init; } = string.Empty;
	public PlannerElkLayoutOptions LayoutOptions { get; init; } = new();
	public List<PlannerElkNode> Children { get; init; } = [];
	public List<PlannerElkEdge> Edges { get; init; } = [];
}

public sealed class PlannerElkLayoutOptions
{
	[JsonPropertyName("elk.algorithm")]
	public string Algorithm { get; init; } = string.Empty;

	[JsonPropertyName("org.eclipse.elk.layered.nodePlacement.favorStraightEdges")]
	public bool FavorStraightEdges { get; init; }

	[JsonPropertyName("org.eclipse.elk.spacing.nodeNode")]
	public string NodeNodeSpacing { get; init; } = string.Empty;
}

public sealed class PlannerElkNode
{
	public string Id { get; init; } = string.Empty;
	public int Width { get; init; }
	public int Height { get; init; }
}

public sealed class PlannerElkEdge
{
	public string Id { get; init; } = string.Empty;
	public string Source { get; init; } = string.Empty;
	public string Target { get; init; } = string.Empty;
}
