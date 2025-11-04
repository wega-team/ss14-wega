using Content.Server.Speech.Components;
using Content.Shared.Xenobiology;
using Content.Shared.Xenobiology.Components;
using Content.Shared.Xenobiology.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Xenobiology;

public sealed class SlimeGrowthSystem : SharedSlimeGrowthSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    private static readonly EntProtoId DefaultSlime = "MobXenoSlimeGray";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SlimeGrowthComponent, SlimeHungerStateChangedEvent>(OnHungerChanged);
    }

    private void OnHungerChanged(EntityUid uid, SlimeGrowthComponent growth, ref SlimeHungerStateChangedEvent args)
    {
        if (args.NewState == SlimeBehaviorState.Dividing)
            TryDivideSlime(uid, growth);
    }

    public bool TryEvolve(EntityUid uid, SlimeHungerComponent hunger, SlimeGrowthComponent? growth = null)
    {
        if (!Resolve(uid, ref growth))
            return false;

        if (growth.CurrentStage >= SlimeStage.Ancient || hunger.Hunger < growth.NextStageHungerThreshold)
            return false;

        var wasBaby = growth.CurrentStage == SlimeStage.Young;

        growth.CurrentStage++;
        growth.NextStageHungerThreshold = hunger.MaxHunger;
        Dirty(uid, growth);

        UpdateSlimeAccent(uid, growth.CurrentStage);

        if (wasBaby && growth.CurrentStage != SlimeStage.Young)
        {
            var ev = new SlimeStageChangedEvent();
            RaiseLocalEvent(uid, ref ev);
        }

        return true;
    }

    public bool TryDivideSlime(EntityUid uid, SlimeGrowthComponent? growth = null)
    {
        if (!Resolve(uid, ref growth) || growth.CurrentStage != SlimeStage.Ancient)
            return false;

        int offspringCount = 3;
        var spawnPos = Transform(uid).Coordinates;
        for (int i = 0; i < offspringCount; i++)
        {
            SpawnOffspring(uid, spawnPos, growth);
        }

        growth.CurrentStage = SlimeStage.Young;
        growth.NextStageHungerThreshold = GetBaseHungerThreshold(growth.CurrentStage);

        UpdateSlimeAccent(uid, growth.CurrentStage);

        if (TryComp<SlimeHungerComponent>(uid, out var hunger))
        {
            hunger.Hunger = 100f;
            hunger.MaxHunger = GetBaseHungerThreshold(growth.CurrentStage);
            Dirty(uid, hunger);
        }

        var ev = new SlimeStageChangedEvent();
        RaiseLocalEvent(uid, ref ev);

        return true;
    }

    private void SpawnOffspring(EntityUid parent, EntityCoordinates spawnPos, SlimeGrowthComponent parentGrowth)
    {
        var offspring = Spawn(DefaultSlime, spawnPos.Offset(_random.NextVector2(1f)));
        if (!TryComp<SlimeGrowthComponent>(offspring, out var growth))
            return;

        growth.CurrentStage = SlimeStage.Young;
        growth.NextStageHungerThreshold = GetBaseHungerThreshold(growth.CurrentStage);

        var accent = EnsureComp<ReplacementAccentComponent>(offspring);
        accent.Accent = "slimes";

        growth.MutationChance = parentGrowth.MutationChance;
        if (_random.Prob(0.3f))
        {
            float reductionPercent = _random.NextFloat(0.15f, 0.45f);

            var newChance = growth.MutationChance * (1 - reductionPercent);
            growth.MutationChance = Math.Max(newChance, 0.05f);
        }

        if (_random.Prob(parentGrowth.MutationChance) || parentGrowth.SlimeType == SlimeType.Rainbow)
        {
            growth.SlimeType = GetMutationInternal(parentGrowth.SlimeType, parentGrowth.RainbowChance) ?? parentGrowth.SlimeType;
        }
        else
        {
            growth.SlimeType = parentGrowth.SlimeType;
        }

        Dirty(offspring, growth);
        ApplySlimeType(offspring);
    }

    private void ApplySlimeType(EntityUid uid)
    {
        var ev = new SlimeTypeChangedEvent();
        RaiseLocalEvent(uid, ref ev);
    }

    private void UpdateSlimeAccent(EntityUid uid, SlimeStage stage)
    {
        if (stage == SlimeStage.Young)
        {
            var accent = EnsureComp<ReplacementAccentComponent>(uid);
            accent.Accent = "slimes";
        }
        else
        {
            RemComp<ReplacementAccentComponent>(uid);
        }
    }

    private static float GetBaseHungerThreshold(SlimeStage stage) => stage switch
    {
        SlimeStage.Young => 200f,
        SlimeStage.Adult => 250f,
        SlimeStage.Old => 300f,
        SlimeStage.Ancient => 400f,
        _ => 200f
    };
}
