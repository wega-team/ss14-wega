using Content.Shared.Modular.Suit;
using Robust.Client.UserInterface;

namespace Content.Client._Wega.ModularSuit.Ui;

public sealed class LightModuleBoundUserInterface : BoundUserInterface
{
    private LightModuleWindow? _window;

    public LightModuleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<LightModuleWindow>();

        _window.OnUpdate += (color, multicoloured) =>
        {
            SendMessage(new UpdateLightModuleMessage(color, multicoloured));
            _window.Close();
        };
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is LightModuleBoundUserInterfaceState cast)
            _window?.UpdateState(cast);
    }
}
