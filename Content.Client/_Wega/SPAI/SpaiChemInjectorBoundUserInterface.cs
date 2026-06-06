using Content.Shared._Wega.SPAI;
using Robust.Client.UserInterface;

namespace Content.Client._Wega.SPAI;

public sealed class SpaiChemInjectorBoundUserInterface : BoundUserInterface
{
    private SpaiChemInjectorWindow? _window;

    public SpaiChemInjectorBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<SpaiChemInjectorWindow>();
        _window.OnInjectPressed += (reagentId, cost) =>
            SendMessage(new SpaiChemInjectorInjectMessage(reagentId, cost));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is SpaiChemInjectorBoundUserInterfaceState s)
            _window?.UpdateState(s);
    }
}
