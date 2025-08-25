using Content.Server.Bible.Components;
using Content.Shared.Hands;
using Content.Shared.Inventory.Events;
using Content.Shared.NullRod.Components;

namespace Content.Server.NullRod;

public sealed class NullRodSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NullRodComponent, GotEquippedEvent>(OnDidEquip);
        SubscribeLocalEvent<NullRodComponent, GotEquippedHandEvent>(OnHandEquipped);
        SubscribeLocalEvent<NullRodComponent, GotUnequippedEvent>(OnDidUnequip);
        SubscribeLocalEvent<NullRodComponent, GotUnequippedHandEvent>(OnHandUnequipped);
    }

    private void OnDidEquip(Entity<NullRodComponent> ent, ref GotEquippedEvent args)
    {
        if (!HasComp<BibleUserComponent>(args.Equipee) || HasComp<NullRodOwnerComponent>(args.Equipee))
            return;

        EnsureComp<NullRodOwnerComponent>(args.Equipee);
    }

    private void OnHandEquipped(Entity<NullRodComponent> ent, ref GotEquippedHandEvent args)
    {
        if (!HasComp<BibleUserComponent>(args.User) || HasComp<NullRodOwnerComponent>(args.User))
            return;

        EnsureComp<NullRodOwnerComponent>(args.User);
    }

    private void OnDidUnequip(Entity<NullRodComponent> ent, ref GotUnequippedEvent args)
    {
        if (!HasComp<NullRodOwnerComponent>(args.Equipee))
            return;

        RemComp<NullRodOwnerComponent>(args.Equipee);
    }

    private void OnHandUnequipped(Entity<NullRodComponent> ent, ref GotUnequippedHandEvent args)
    {
        if (!HasComp<NullRodOwnerComponent>(args.User))
            return;

        RemComp<NullRodOwnerComponent>(args.User);
    }
}