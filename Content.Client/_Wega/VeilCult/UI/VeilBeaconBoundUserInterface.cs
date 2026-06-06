using Content.Shared.Veil.Cult;
using Robust.Client.UserInterface;

namespace Content.Client._Wega.VeilCult.UI;

public sealed partial class VeilBeaconBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private VeilBeaconWindow? _window;

    public VeilBeaconBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<VeilBeaconWindow>();

        _window.OnNameChanged += OnNameChanged;
        _window.SetInitialNameState();

        _window.OpenCentered();
    }

    private void OnNameChanged(string newName)
    {
        SendMessage(new VeilBeaconNameChangedMessage(newName));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is VeilBeaconNameBoundUserInterfaceState updateState && _window != null)
        {
            _window.SetCurrentLabel(updateState.Name);
            _window.SetMaxLabelLength(updateState.MaxChars);
        }
    }
}
