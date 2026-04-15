using SatisfactoryTools.Solver.Api.Contracts;

namespace SatisfactoryTools.Solver.Api.Services;

public sealed class PlannerResultDomain
{
	public required PlannerResultGraph Graph { get; init; }
	public required PlannerResultDetails Details { get; init; }
}

public sealed class PlannerResultDetails
{
	public PlannerBuildingsResultDetails Buildings { get; init; } = new();
	public Dictionary<string, PlannerItemResultDetails> Items { get; init; } = [];
	public Dictionary<string, PlannerInputResultDetails> Input { get; init; } = [];
	public PlannerPowerResultDetails Power { get; init; } = new();
	public bool HasInput { get; set; }
	public Dictionary<string, PlannerRawResourceResultDetails> RawResources { get; init; } = [];
	public Dictionary<string, double> Output { get; init; } = [];
	public bool HasOutput { get; set; }
	public Dictionary<string, double> Byproducts { get; init; } = [];
	public bool HasByproducts { get; set; }
	public List<GameRecipe> AlternatesNeeded { get; init; } = [];
}

public sealed class PlannerBuildingsResultDetails
{
	public Dictionary<string, PlannerBuildingResultDetails> Buildings { get; init; } = [];
	public Dictionary<string, double> Resources { get; set; } = [];
	public int Amount { get; set; }
}

public sealed class PlannerBuildingResultDetails
{
	public int Amount { get; set; }
	public Dictionary<string, PlannerBuildingRecipeResultDetails> Recipes { get; init; } = [];
	public Dictionary<string, double> Resources { get; set; } = [];
}

public sealed class PlannerBuildingRecipeResultDetails
{
	public int Amount { get; set; }
	public Dictionary<string, double> Resources { get; init; } = [];
}

public sealed class PlannerItemResultDetails
{
	public double Produced { get; set; }
	public double Consumed { get; set; }
	public double Diff { get; set; }
	public Dictionary<string, PlannerItemBuildingAmountResultDetails> Producers { get; init; } = [];
	public Dictionary<string, PlannerItemBuildingAmountResultDetails> Consumers { get; init; } = [];
}

public sealed class PlannerItemBuildingAmountResultDetails
{
	public string Type { get; set; } = string.Empty;
	public double ItemAmount { get; set; }
	public double ItemPercentage { get; set; }
}

public sealed class PlannerInputResultDetails
{
	public double Max { get; set; }
	public double Used { get; set; }
	public double UsedPercentage { get; set; }
	public double ProducedExtra { get; set; }
}

public sealed class PlannerRawResourceResultDetails
{
	public bool Enabled { get; set; }
	public double Max { get; set; }
	public double Used { get; set; }
	public double UsedPercentage { get; set; }
}

public sealed class PlannerPowerResultDetails
{
	public Dictionary<string, PlannerRecipePowerDetails> ByRecipe { get; init; } = [];
	public Dictionary<string, PlannerMachinePowerDetails> ByBuilding { get; init; } = [];
	public PlannerMachineGroupPower Total { get; init; } = new();
}

public sealed class PlannerRecipePowerDetails
{
	public string Machine { get; set; } = string.Empty;
	public List<PlannerMachineGroupItem> Machines { get; init; } = [];
	public PlannerMachineGroupPower Power { get; init; } = new();
}

public sealed class PlannerMachinePowerDetails
{
	public int Amount { get; set; }
	public Dictionary<string, PlannerBuildingPowerRecipeDetails> Recipes { get; init; } = [];
	public PlannerMachineGroupPower Power { get; init; } = new();
}

public sealed class PlannerBuildingPowerRecipeDetails
{
	public int ClockSpeed { get; set; }
	public int Amount { get; set; }
	public PlannerMachineGroupPower Power { get; init; } = new();
}

public sealed class PlannerMachineGroupPower
{
	public double Average { get; set; }
	public bool IsVariable { get; set; }
	public double Max { get; set; }
}

public sealed class PlannerMachineGroupItem
{
	public int Amount { get; set; }
	public double ClockSpeed { get; set; }
	public PlannerMachineGroupPower Power { get; init; } = new();
}
