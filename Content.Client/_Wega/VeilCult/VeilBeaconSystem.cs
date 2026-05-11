using Content.Client.Veil.Cult.UI;
using Content.Shared.Veil.Cult;
using Content.Shared.Veil.Cult.Components;

namespace Content.Client.Veil.Cult;

public sealed class VeilBeaconSystem : SharedVeilBeaconSystem
{
    protected override void UpdateUI(Entity<VeilCultBeaconComponent> ent)
    {
        if (UserInterfaceSystem.TryGetOpenUi(ent.Owner, VeilBeaconUiKey.Key, out var bui)
            && bui is VeilBeaconBoundUserInterface cBui)
        {
            cBui.Reload();
        }
    }
}