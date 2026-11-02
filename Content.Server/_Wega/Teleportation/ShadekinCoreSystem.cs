using Content.Shared.Teleportation.Components;
using Content.Shared.Body;

namespace Content.Server.Teleportation;

public sealed partial class ShadekinCoreSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnCoreInserted(Entity<ShadekinCoreComponent> ent, ref OrganGotInsertedEvent args)
    {
        ent.Comp.OwnerBody ??= args.Target;
        ent.Comp.Mode = ent.Comp.IsBurned ? ShadekinCoreMode.Inactive : ShadekinCoreMode.Active;
        Dirty(ent);
    }
}
