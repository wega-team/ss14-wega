using Content.Shared.StepTrigger.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.Whitelist; // Corvax-Wega-Lavaland

namespace Content.Server.Tiles;

public sealed class TileEntityEffectSystem : EntitySystem
{
    [Dependency] private readonly SharedEntityEffectsSystem _entityEffects = default!;
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!; // Corvax-Wega-Lavaland

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TileEntityEffectComponent, StepTriggeredOffEvent>(OnTileStepTriggered);
        SubscribeLocalEvent<TileEntityEffectComponent, StepTriggerAttemptEvent>(OnTileStepTriggerAttempt);
    }
    private void OnTileStepTriggerAttempt(Entity<TileEntityEffectComponent> ent, ref StepTriggerAttemptEvent args)
    {
        // Corvax-Wega-Lavaland-start
        if (_entityWhitelist.IsWhitelistPass(ent.Comp.Blacklist, args.Tripper))
        {
            args.Cancelled = true;
            return;
        }
        // Corvax-Wega-Lavaland-end

        args.Continue = true;
    }

    private void OnTileStepTriggered(Entity<TileEntityEffectComponent> ent, ref StepTriggeredOffEvent args)
    {
        var otherUid = args.Tripper;

        _entityEffects.ApplyEffects(otherUid, ent.Comp.Effects.ToArray(), user: otherUid);
    }
}
