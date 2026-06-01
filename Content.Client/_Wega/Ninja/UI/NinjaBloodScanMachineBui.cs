using Content.Shared._Wega.Ninja;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Wega.Ninja.UI;

[UsedImplicitly]
public sealed class NinjaBloodScanMachineBui : BoundUserInterface
{
    private NinjaBloodScanMachineWindow? _window;

    public NinjaBloodScanMachineBui(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<NinjaBloodScanMachineWindow>();
        _window.OnSlotAction += idx => SendMessage(new NinjaBloodScanSlotActionMessage(idx));
        _window.OnScan += () => SendMessage(new NinjaBloodScanScanMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (_window == null || state is not NinjaBloodScanBuiState s)
            return;
        _window.UpdateState(s);
    }
}
