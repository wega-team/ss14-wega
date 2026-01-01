using Content.Shared.Blood.Cult;
using Robust.Client.UserInterface;

namespace Content.Client._Wega.BloodCult.Ui;

public sealed class BloodRitesBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private BloodRitesMenu? _menu;

    public BloodRitesBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<BloodRitesMenu>();
        _menu.OnSelectRites += rites =>
        {
            SendMessage(new BloodRitesSelectRitesMessage(rites));
            Close();
        };

        _menu.OpenCentered();
    }
}
