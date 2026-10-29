using Content.Shared.Veil.Cult.UI;
using Robust.Client.UserInterface;

namespace Content.Client._Wega.VeilCult.UI;

public sealed partial class VeilAltarBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private VeilAltarMenu? _menu;

    public VeilAltarBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<VeilAltarMenu>();
        _menu.OpenCentered();

        _menu.OnSelectEnergy += () =>
        {
            SendMessage(new VeilAltarSelectEnergyMessage());
            Close();
        };

        _menu.OnSelectOffer += () =>
        {
            SendMessage(new VeilAltarSelectOfferMessage());
            Close();
        };
    }
}
