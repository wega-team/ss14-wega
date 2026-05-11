using Content.Client.Veil.Cult.UI;
using Content.Shared.Veil.Cult;
using Content.Shared.Veil.Cult.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Veil.Cult.Ui;

[UsedImplicitly]
public sealed class TeleportEnchantBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private TeleportEnchantMenu? _menu;

    public TeleportEnchantBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<TeleportEnchantMenu>();

        if (!EntMan.TryGetComponent<TeleportationEnchantComponent>(Owner, out var teleComp))
            return;

        _menu.Title = Loc.GetString(teleComp.Name);
        _menu.Warps = teleComp.AvailableWarps;
        _menu.AddTeleportButtons();

        _menu.TeleportClicked += (netEnt, pointName) =>
        {
            SendMessage(new TeleportEnchantDestinationMessage(netEnt, pointName));
        };
    }
}
