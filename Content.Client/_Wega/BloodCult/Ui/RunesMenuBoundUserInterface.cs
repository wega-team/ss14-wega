using Content.Shared.Blood.Cult;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Wega.BloodCult.Ui;

[UsedImplicitly]
public sealed class RunesMenuBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private RunesMenu? _menu;

    public RunesMenuBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<RunesMenu>();
        _menu.OnRuneSelected += rune =>
        {
            SendMessage(new SelectBloodRuneMessage(rune));
            Close();
        };

        _menu.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is BloodRitualBoundUserInterfaceState _)
            _menu?.AddRitualButton();
    }
}
