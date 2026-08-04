using Content.Shared._Wega.Ninja;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Wega.Ninja.UI;

[UsedImplicitly]
public sealed class NinjaTeleConsoleBui : BoundUserInterface
{
    private NinjaTeleConsoleWindow? _window;

    public NinjaTeleConsoleBui(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<NinjaTeleConsoleWindow>();
        _window.OnConfirm += () => SendMessage(new NinjaTeleConsoleConfirmMessage());
        _window.OnCancel  += Close;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (_window == null || state is not NinjaTeleConsoleBuiState s)
            return;

        _window.UpdateState(s);
    }
}
