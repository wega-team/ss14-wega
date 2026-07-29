using Content.Shared.Interaction.Components;

namespace Content.Server.Interaction;

public sealed partial class SpawnOnDespawnSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpawnOnDeleteComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnShutdown(EntityUid uid, SpawnOnDeleteComponent comp, ComponentShutdown args)
    {
        if (TerminatingOrDeleted(uid))
            return;

        var xform = Transform(uid);
        if (!xform.Coordinates.IsValid(EntityManager))
            return;

        SpawnAtPosition(comp.Prototype, xform.Coordinates);
    }
}
