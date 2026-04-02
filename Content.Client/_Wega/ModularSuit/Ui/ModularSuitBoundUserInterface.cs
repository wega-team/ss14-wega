using Content.Shared.Modular.Suit;
using Robust.Client.UserInterface;

namespace Content.Client._Wega.ModularSuit.Ui;

public sealed class ModularSuitBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private ModularSuitWindow? _window;

    public ModularSuitBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindowCenteredLeft<ModularSuitWindow>();
        _window.OnClose += Close;

        _window.OnToggleActive += active =>
        {
            SendMessage(new ToggleSuitActiveMessage(active));
        };

        _window.OnToggleModule += (moduleUid, active) =>
        {
            SendMessage(new ToggleModuleMessage(moduleUid, active));
        };
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is ModularSuitBoundUserInterfaceState cast)
            _window?.UpdateState(cast);
    }
}
