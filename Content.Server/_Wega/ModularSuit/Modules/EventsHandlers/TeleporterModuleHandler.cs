using Content.Shared.Mobs.Components;
using Content.Shared.Modular.Suit;
using Robust.Shared.Random;

namespace Content.Server.Modular.Suit;

public sealed class TeleporterModuleHandler : ModuleActionHandler
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public const float TeleportRadius = 5f;

    public override void Initialize()
    {
        SubscribeLocalEvent<ModularSuitActionHolderComponent, ActivateTeleporterModuleEvent>(OnActivate);
    }

    private void OnActivate(Entity<ModularSuitActionHolderComponent> ent, ref ActivateTeleporterModuleEvent args)
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

        if (PerformTeleport(args.Performer))
        {
            Audio.PlayPvs(args.ActivationSound, args.Performer);
            ModularSuit.UseCoreCharge(ent.Owner, moduleComp.PowerInstanceUsage);
        }

        args.Handled = true;
    }

    private bool PerformTeleport(EntityUid user)
    {
        var userCoords = Transform(user).Coordinates;

        var mobs = new HashSet<Entity<MobStateComponent>>();
        _lookup.GetEntitiesInRange(userCoords, TeleportRadius, mobs, LookupFlags.Uncontained);

        if (mobs.Count == 0)
        {
            Popup.PopupEntity(Loc.GetString("modsuit-teleporter-no-targets"), user, user);
            return false;
        }

        var target = _random.Pick(mobs).Owner;

        _transform.SetCoordinates(user, Transform(target).Coordinates);
        _transform.SetCoordinates(target, userCoords);

        return true;
    }
}
