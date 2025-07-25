using System.Linq;
using Robust.Shared.Random;

namespace Content.Shared.Xenobiology.Systems;

public abstract partial class SharedSlimeGrowthSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

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
}
