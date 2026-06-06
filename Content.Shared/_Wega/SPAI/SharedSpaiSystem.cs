using Content.Shared.Interaction;

namespace Content.Shared._Wega.SPAI;

public sealed class SharedSpaiSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SpaiWeakAiCapabilityComponent, AccessibleOverrideEvent>(OnWeakAiAccessible);
    }

    private void OnWeakAiAccessible(Entity<SpaiWeakAiCapabilityComponent> ent, ref AccessibleOverrideEvent args)
    {
        args.Handled = true;
        args.Accessible = true;
    }
}
