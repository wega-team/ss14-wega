using Content.Shared.Mobs.Components;
using Content.Shared.Modular.Suit;
using Content.Shared.Interaction;
using Content.Shared.Physics;
using Content.Shared.Mobs.Components;
using Content.Shared.Gravity;
using Content.Shared.Movement.Components;
using Robust.Shared.Serialization;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Timing;
﻿using System.Linq;

namespace Content.Server.Modular.Suit;

public sealed partial class AntiGravitationHandler : ModuleActionHandler
{
    [Dependency] private EntityLookupSystem _entityLookup = default!;
    [Dependency] private INetManager _net = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ModularSuitActionHolderComponent, AntiGravitationEvent>(OnActivate);
    }

    private void OnActivate(Entity<ModularSuitActionHolderComponent> ent, ref AntiGravitationEvent args)
    {
        if (args.Handled)
            return;

        if (!TryFindModuleByAction(ent, args.Action, out var moduleEnt))
            return;

        if (!TryComp<ModularSuitModuleComponent>(moduleEnt, out var moduleComp) || !moduleComp.IsActive)
            return;

        var attemptEvent = new ModularSuitModuleAttemptEvent(ent.Owner);
        RaiseLocalEvent(moduleEnt.Value, ref attemptEvent);

        if (attemptEvent.Cancelled)
            return;

        if (PerformAntiGrav(args.Performer))
        {
            Audio.PlayPvs(args.ActivationSound, args.Performer);
            ModularSuit.UseCoreCharge(ent.Owner, moduleComp.PowerInstanceUsage);
        }

        args.Handled = true;
    }

    private bool PerformAntiGrav(EntityUid user)
    {
        if (_net.IsClient)
            return false;		

        var nearbyTargets = _entityLookup.GetEntitiesInRange<MobStateComponent>(Transform(user).Coordinates, 10f)
           .Where(target => target.Owner != user)
           .ToList();

        foreach (var target in nearbyTargets)
        {
            EnsureComp<GravityAffectedComponent>(target, out var weightless);
            weightless.Weightless = true;

            Dirty(target, weightless);

			Timer.Spawn(TimeSpan.FromSeconds(15), () => weightless.Weightless = false);

            Timer.Spawn(TimeSpan.FromSeconds(15), () => Dirty(target, weightless));
		}
		
        return true;
    }
}
