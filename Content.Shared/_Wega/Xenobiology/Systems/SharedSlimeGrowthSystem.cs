using System.Linq;
using Robust.Shared.Random;

namespace Content.Shared.Xenobiology.Systems;

public abstract partial class SharedSlimeGrowthSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;

    public static readonly Dictionary<SlimeType, List<(SlimeType type, float weight)>> MutationTable = new()
    {
        [SlimeType.Gray] = new()
        {
            (SlimeType.Purple, 1f),
            (SlimeType.Orange, 1f),
            (SlimeType.Metallic, 1f),
            (SlimeType.Blue, 1f)
        },

        [SlimeType.Purple] = new()
        {
            (SlimeType.DarkPurple, 1f),
            (SlimeType.Green, 2f),
            (SlimeType.DarkBlue, 1f)
        },

        [SlimeType.Orange] = new()
        {
            (SlimeType.DarkPurple, 1f),
            (SlimeType.Red, 2f),
            (SlimeType.Yellow, 1f)
        },

        [SlimeType.Metallic] = new()
        {
            (SlimeType.Yellow, 1f),
            (SlimeType.Gold, 2f),
            (SlimeType.Silver, 1f)
        },

        [SlimeType.Blue] = new()
        {
            (SlimeType.DarkBlue, 1f),
            (SlimeType.Pink, 2f),
            (SlimeType.Silver, 1f)
        },

        [SlimeType.DarkPurple] = new()
        {
            (SlimeType.Purple, 1f),
            (SlimeType.Orange, 1f),
            (SlimeType.Sepia, 2f)
        },

        [SlimeType.Green] = new()
        {
            (SlimeType.Green, 2f),
            (SlimeType.Black, 2f)
        },

        [SlimeType.DarkBlue] = new()
        {
            (SlimeType.Blue, 1f),
            (SlimeType.Purple, 1f),
            (SlimeType.Azure, 2f)
        },

        [SlimeType.Pink] = new()
        {
            (SlimeType.Pink, 2f),
            (SlimeType.LightPink, 2f)
        },

        [SlimeType.Red] = new()
        {
            (SlimeType.Red, 2f),
            (SlimeType.Oil, 2f)
        },

        [SlimeType.Yellow] = new()
        {
            (SlimeType.Orange, 1f),
            (SlimeType.Bluespace, 2f),
            (SlimeType.Metallic, 1f)
        },

        [SlimeType.Gold] = new()
        {
            (SlimeType.Gold, 2f),
            (SlimeType.Adamantine, 2f)
        },

        [SlimeType.Silver] = new()
        {
            (SlimeType.Metallic, 1f),
            (SlimeType.Pyrite, 2f),
            (SlimeType.Blue, 1f)
        },

        [SlimeType.Black] = new() { (SlimeType.Black, 1f) },
        [SlimeType.Sepia] = new() { (SlimeType.Sepia, 1f) },
        [SlimeType.Oil] = new() { (SlimeType.Oil, 1f) },
        [SlimeType.Bluespace] = new() { (SlimeType.Bluespace, 1f) },
        [SlimeType.Adamantine] = new() { (SlimeType.Adamantine, 1f) },
        [SlimeType.Pyrite] = new() { (SlimeType.Pyrite, 1f) },
        [SlimeType.Azure] = new() { (SlimeType.Azure, 1f) },
        [SlimeType.LightPink] = new() { (SlimeType.LightPink, 1f) },

        [SlimeType.Rainbow] = Enum.GetValues<SlimeType>()
            .Where(t => t != SlimeType.Rainbow)
            .Select(t => (t, 1f))
            .ToList()
    };

    public static readonly Dictionary<SlimeType, (SlimeType Type, int Point, float ModifierFood)> ParametrTable = new()
    {
		[SlimeType.Gray] = (SlimeType.Gray, 5000, 3f),
    
    // lvl 1
		[SlimeType.Purple] = (SlimeType.Purple, 10000, 2.5f),
		[SlimeType.Orange] = (SlimeType.Orange, 10000, 2.5f),
		[SlimeType.Metallic] = (SlimeType.Metallic, 10000, 2.5f),
		[SlimeType.Blue] = (SlimeType.Blue, 10000, 2.5f),
    
    // lvl 2
		[SlimeType.DarkPurple] = (SlimeType.DarkPurple, 12500, 2f),
		[SlimeType.Yellow] = (SlimeType.Yellow, 12500, 2f),
		[SlimeType.DarkBlue] = (SlimeType.DarkBlue, 12500, 2f),
		[SlimeType.Silver] = (SlimeType.Silver, 12500, 2f),
    
    // lvl 2.5
		[SlimeType.Bluespace] = (SlimeType.Bluespace, 15000, 1.75f),
		[SlimeType.Pyrite] = (SlimeType.Pyrite, 15000, 1.75f),
		[SlimeType.Azure] = (SlimeType.Azure, 15000, 1.75f),
		[SlimeType.Sepia] = (SlimeType.Sepia, 15000, 1.75f),
    
    // lvl 3
		[SlimeType.Pink] = (SlimeType.Pink, 20000, 1.5f),
		[SlimeType.Red] = (SlimeType.Red, 20000, 1.5f),
		[SlimeType.Green] = (SlimeType.Green, 20000, 1.5f),
		[SlimeType.Gold] = (SlimeType.Gold, 20000, 1.5f),
    
    // lvl 4
		[SlimeType.Black] = (SlimeType.Black, 25000, 1f),
		[SlimeType.Oil] = (SlimeType.Oil, 25000, 1f),
		[SlimeType.Adamantine] = (SlimeType.Adamantine, 25000, 1f),
		[SlimeType.LightPink] = (SlimeType.LightPink, 25000, 1f),

		[SlimeType.Rainbow] = (SlimeType.Rainbow, 1000000, 0.5f),
    };

    public SlimeType? GetMutationInternal(SlimeType currentType, float rainbowChance)
    {
        if (currentType != SlimeType.Rainbow && _random.Prob(rainbowChance))
        {
            return SlimeType.Rainbow;
        }

        if (!MutationTable.TryGetValue(currentType, out var mutations))
            return null;

        var totalWeight = mutations.Sum(m => m.weight);
        var roll = _random.NextFloat() * totalWeight;
        foreach (var (type, weight) in mutations)
        {
            if (roll <= weight)
                return type;

            roll -= weight;
        }

        return currentType;
    }
	
	public (float Point, float ModifierFood)? GetSlimeParameters(SlimeType slimeType)
	{
		if (ParametrTable.TryGetValue(slimeType, out var parameters))
			return (parameters.Point, parameters.ModifierFood);
    
		return null;
	}

	public int GetPoint(SlimeType slimeType)
	{
		if (ParametrTable.TryGetValue(slimeType, out var parameters))
			return parameters.Point;
    
		return 0;
	}

	public float GetModifier(SlimeType slimeType)
	{
		if (ParametrTable.TryGetValue(slimeType, out var parameters))
			return parameters.ModifierFood;
    
		return 1f;
	}
}
