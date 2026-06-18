using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Examine;
using Content.Shared.Item;
using Content.Shared.Surgery.Components;
using Content.Shared.Throwing;
using Robust.Shared.Prototypes;

namespace Content.Server.Surgery;

public sealed partial class SterileSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;

    private static readonly ProtoId<ReagentPrototype> EthanolReagent = "Ethanol";

    private const float EthanolUnitsPerSterilePoint = 0.05f;
    private const float MaxSterileAmount = 100f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SterileComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<SterileComponent, ThrownEvent>(OnThrow);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var sterileQuery = EntityQueryEnumerator<SterileComponent>();
        while (sterileQuery.MoveNext(out var uid, out var sterile))
        {
            if (sterile.AlwaysSterile)
                continue;

            if (sterile.NextUpdateTick <= 0)
            {
                sterile.NextUpdateTick = 5f;
                sterile.Amount -= sterile.DecayRate;
                if (sterile.Amount <= 0)
                {
                    RemComp<SterileComponent>(uid);
                }
            }
            sterile.NextUpdateTick -= frameTime;
        }
    }

    private void OnExamined(Entity<SterileComponent> entity, ref ExaminedEvent args)
    {
        if (args.IsInDetailsRange)
            args.AddMarkup(Loc.GetString("surgery-sterile-examined") + "\n");
    }

    private void OnThrow(Entity<SterileComponent> entity, ref ThrownEvent args)
        => RemCompDeferred<SterileComponent>(entity);

    public float ApplySterilityFromSolution(EntityUid target, Solution solution, float transferAmount)
    {
        if (!HasComp<ItemComponent>(target))
            return 0f;

        var totalEthanol = GetTotalEthanolEquivalent(solution, transferAmount);
        if (totalEthanol <= 0)
            return 0f;

        var sterileAmount = totalEthanol / EthanolUnitsPerSterilePoint;
        sterileAmount = Math.Min(sterileAmount, MaxSterileAmount);

        var sterileComp = EnsureComp<SterileComponent>(target);
        sterileComp.Amount = Math.Min(sterileComp.Amount + sterileAmount, MaxSterileAmount);
        sterileComp.NextUpdateTick = 0f;

        return sterileAmount;
    }

    private float GetTotalEthanolEquivalent(Solution solution, float transferAmount)
    {
        if (transferAmount <= 0)
            return 0f;

        var totalSolutionVolume = solution.Volume.Float();
        if (totalSolutionVolume <= 0)
            return 0f;

        var ratio = transferAmount / totalSolutionVolume;
        float total = 0f;

        foreach (var reagent in solution.Contents)
        {
            var reagentProto = _proto.Index<ReagentPrototype>(reagent.Reagent.Prototype);
            float ethanolPerUnit = 0f;

            if (reagent.Reagent.Prototype == EthanolReagent)
            {
                ethanolPerUnit = 1f;
            }
            else
            {
                if (reagentProto.Metabolisms?.Metabolisms.TryGetValue("Digestion", out var metabolism) == true
                    && metabolism.Metabolites != null)
                {
                    foreach (var metabolite in metabolism.Metabolites)
                    {
                        if (metabolite.Key == EthanolReagent)
                        {
                            ethanolPerUnit = metabolite.Value.Float();
                            break;
                        }
                    }
                }
            }

            if (ethanolPerUnit > 0f)
            {
                var transferredReagentAmount = reagent.Quantity.Float() * ratio;
                total += transferredReagentAmount * ethanolPerUnit;
            }
        }

        return total;
    }
}
