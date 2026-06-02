using Content.Shared.Veil.Cult;
using Content.Shared.Veil.Cult.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Wega.VeilCult.UI;

[UsedImplicitly]
public sealed partial class TeleportEnchantBoundUserInterface : BoundUserInterface
{
    // Copy of TeleportLocationsUI for Teleportation enchantment
    [ViewVariables]
    private TeleportEnchantMenu? _menu;

    public TeleportEnchantBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<TeleportEnchantMenu>();

        if (!EntMan.TryGetComponent<TeleportationEnchantComponent>(Owner, out var teleComp))
            return;

        _menu.Title = Loc.GetString(teleComp.Name);
        _menu.TeleportClicked += netEnt =>
        {
            SendMessage(new TeleportEnchantDestinationMessage(netEnt));
        };
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is TeleportationEnchantBoundUserInterfaceState updateState && _menu != null)
            _menu.UpdateState(updateState);
    }
}
