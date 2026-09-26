using Content.Shared.Mobs.Components;
using Content.Shared.Modular.Suit;
using Content.Shared.Interaction;
using Content.Shared.Physics;
using Robust.Shared.Serialization;
using Robust.Shared.Map;

namespace Content.Server.Modular.Suit;

public sealed partial class TeleporterModuleHandler : ModuleActionHandler
{
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;

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

        if (PerformTeleport(args.Performer, args.Target))
        {
            Audio.PlayPvs(args.ActivationSound, args.Performer);
            ModularSuit.UseCoreCharge(ent.Owner, moduleComp.PowerInstanceUsage);
        }

        args.Handled = true;
    }

    private bool PerformTeleport(EntityUid user, EntityCoordinates coordinates)
    {
        var transform = Transform(user);
        if (transform.MapID != _transform.GetMapId(coordinates) || !_interaction.InRangeUnobstructed(user, coordinates, range: 1000F, collisionMask: CollisionGroup.Opaque, popup: true))
            return false;

        _transform.SetCoordinates(user, coordinates);
        _transform.AttachToGridOrMap(user, transform);

        return true;
    }
}
