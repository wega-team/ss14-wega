using Content.Shared.Actions;
using Content.Server.Teleportation;
using Content.Shared.Body;
using Content.Shared.Roles;
using Content.Shared.Teleportation.Components;

namespace Content.Server.Traits;

public sealed partial class ShadekinCoreBurned : JobSpecial
{
    public override void AfterEquip(EntityUid mob)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        var body = entMan.System<BodySystem>();
        var teleporter = entMan.System<ActionTeleporterSystem>();
        var actions = entMan.System<SharedActionsSystem>();

        foreach (var organ in body.EnumerateOrgans<ShadekinCoreComponent>(mob))
        {
            organ.Comp2.IsBurned = true;
            organ.Comp2.Mode = ShadekinCoreMode.Inactive; // reflect immediately, since core is already inserted
            entMan.Dirty(organ.Owner, organ.Comp2);
        }

        if (entMan.TryGetComponent<ActionTeleporterComponent>(mob, out var teleComp))
        {
            teleporter.FizzlePortals((mob, teleComp), null, false);
            actions.RemoveAction(mob, teleComp.CreatePortalActionEntity);
            actions.RemoveAction(mob, teleComp.CreateSecondPortalActionEntity);
        }
    }
}
