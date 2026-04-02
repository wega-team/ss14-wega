using Content.Shared.Modular.Suit;
using Content.Shared.Throwing;
using Robust.Shared.Random;
using Robust.Shared.Physics.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Stunnable;

namespace Content.Server.Modular.Suit;

public sealed class AtrocinatorModuleHandler : ModuleActionHandler
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;

    private const float Radius = 5f;
    private const float ThrowStrength = 20f;

    public override void Initialize()
    {
        SubscribeLocalEvent<ModularSuitActionHolderComponent, ActivateAtrocinatorModuleEvent>(OnActivate);
    }

    private void OnActivate(Entity<ModularSuitActionHolderComponent> ent, ref ActivateAtrocinatorModuleEvent args)
    {
        if (args.Handled)
            return;

        if (!TryFindModuleByAction(ent, args.Action, out var moduleEnt))
            return;

        if (!TryComp<ModularSuitModuleComponent>(moduleEnt, out var moduleComp) || !moduleComp.IsActive)
            return;

        var user = args.Performer;

        var entities = _lookup.GetEntitiesInRange<MobStateComponent>(Transform(user).Coordinates, Radius);
        if (entities.Count == 0)
        {
            Popup.PopupEntity(Loc.GetString("modsuit-atrocinator-no-targets"), user, user);
            return;
        }

        foreach (var entity in entities)
        {
            if (!HasComp<PhysicsComponent>(entity))
                continue;

            var direction = _random.NextVector2();
            _throwing.TryThrow(entity.Owner, direction, ThrowStrength, user);
            _stun.TryKnockdown(entity.Owner, TimeSpan.FromSeconds(2));
        }

        Audio.PlayPvs(args.ActivationSound, user);
        ModularSuit.UseCoreCharge(ent.Owner, moduleComp.PowerInstanceUsage);
        Popup.PopupPredicted(Loc.GetString("modsuit-atrocinator-used"), user, null);

        args.Handled = true;
    }
}
