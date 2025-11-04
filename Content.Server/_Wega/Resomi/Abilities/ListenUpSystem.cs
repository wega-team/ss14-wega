using Content.Shared.Resomi.Abilities.Hearing;
using Content.Shared.Popups;
using Content.Shared.IdentityManagement;

namespace Content.Server.Resomi.Abilities;

public sealed class ListenUpSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ListenUpComponent, ComponentStartup>(OnListenStartup);
    }

    private void OnListenStartup(Entity<ListenUpComponent> ent, ref ComponentStartup args)
    {
        _popup.PopupEntity(Loc.GetString("listen-up-activated-massage", ("name", Identity.Entity(ent.Owner, EntityManager))), ent.Owner);
    }
}
