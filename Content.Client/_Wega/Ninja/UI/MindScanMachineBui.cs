using Content.Shared._Wega.Ninja;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Wega.Ninja.UI;

[UsedImplicitly]
public sealed class MindScanMachineBui : BoundUserInterface
{
    private MindScanMachineWindow? _window;

    public MindScanMachineBui(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<MindScanMachineWindow>();
        _window.OnScan     += () => SendMessage(new MindScanScanMessage());
        _window.OnEject    += () => SendMessage(new MindScanEjectMessage());
        _window.OnTeleport += () => SendMessage(new MindScanTeleportMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (_window == null || state is not MindScanMachineBuiState s)
            return;

        _window.UpdateState(s);
    }
}
