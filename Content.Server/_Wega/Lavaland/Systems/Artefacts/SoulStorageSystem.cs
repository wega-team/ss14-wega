using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Lavaland.Artefacts.Components;
using Content.Shared.Mobs;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Player;

namespace Content.Server.Lavaland.Artefacts.Systems;

public sealed class SoulStorageSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SoulStorageComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<ActorComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMeleeHit(Entity<SoulStorageComponent> ent, ref MeleeHitEvent args)
    {
        if (ent.Comp.CurrentStolen.Count == 0)
            return;

        var modifier = ent.Comp.ModifierPerCount * ent.Comp.CurrentStolen.Count;
        modifier = Math.Min(modifier, ent.Comp.MaxDamageModifier);

        var bonusDamage = args.BaseDamage * modifier - args.BaseDamage;
        args.BonusDamage += bonusDamage;
    }

    private void OnMobStateChanged(EntityUid uid, ActorComponent component, MobStateChangedEvent args)
    {
        if (args.Origin == null || args.NewMobState != MobState.Dead)
            return;

        if (!HasComp<HumanoidProfileComponent>(args.Target)) // Only humanoids
            return;

        var item = _hands.GetActiveItemOrSelf(args.Origin.Value);
        if (!TryComp<SoulStorageComponent>(item, out var soulStorage))
            return;

        if (soulStorage.CurrentStolen.Contains(args.Target))
            return;

        soulStorage.CurrentStolen.Add(args.Target);
    }
}
