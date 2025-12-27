using Content.Shared.Blood.Cult;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Wega.BloodCult.Ui;

[UsedImplicitly]
public sealed class BloodConstructBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private BloodConstructMenu? _menu;

    public BloodConstructBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<BloodConstructMenu>();
        _menu.OnSelectConstruct += construct =>
        {
            SendMessage(new BloodConstructSelectMessage(construct));
            Close();
        };

        _menu.OpenCentered();
    }
}
