namespace SatisfactoryTools.Solver.Api.Contracts;

public sealed class GameDataDocument
{
	public Dictionary<string, GameItem> Items { get; init; } = [];
	public Dictionary<string, GameRecipe> Recipes { get; init; } = [];
	public Dictionary<string, GameBuilding> Buildings { get; init; } = [];
	public Dictionary<string, GameResource> Resources { get; init; } = [];
}

public sealed class GameItem
{
	public string ClassName { get; init; } = string.Empty;
	public string Name { get; init; } = string.Empty;
	public bool Liquid { get; init; }
	public double SinkPoints { get; init; }
}

public sealed class GameResource
{
	public string Item { get; init; } = string.Empty;
}

public sealed class GameBuilding
{
	public string ClassName { get; init; } = string.Empty;
	public string Name { get; init; } = string.Empty;
	public GameBuildingMetadata Metadata { get; init; } = new();
}

public sealed class GameBuildingMetadata
{
	public double ManufacturingSpeed { get; init; }
	public double PowerConsumption { get; init; }
	public double PowerConsumptionExponent { get; init; }
}

public sealed class GameRecipe
{
	public string ClassName { get; init; } = string.Empty;
	public string Name { get; init; } = string.Empty;
	public bool Alternate { get; init; }
	public bool InMachine { get; init; }
	public bool ForBuilding { get; init; }
	public double Time { get; init; }
	public bool IsVariablePower { get; init; }
	public double MinPower { get; init; }
	public double MaxPower { get; init; }
	public List<GameItemAmount> Ingredients { get; init; } = [];
	public List<GameItemAmount> Products { get; init; } = [];
	public List<string> ProducedIn { get; init; } = [];
}

public sealed class GameItemAmount
{
	public string Item { get; init; } = string.Empty;
	public double Amount { get; init; }
}
