using Content.Shared.Interaction.Components;

namespace Content.Server.Interaction;

public sealed class SpawnOnDespawnSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpawnOnDeleteComponent, EntityTerminatingEvent>(OnDelete);
    }

    private void OnDelete(EntityUid uid, SpawnOnDeleteComponent comp, EntityTerminatingEvent args)
        => Spawn(comp.Prototype, Transform(uid).Coordinates);
}
