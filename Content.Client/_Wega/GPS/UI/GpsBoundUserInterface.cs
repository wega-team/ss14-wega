using Content.Shared.GPS;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Wega.GPS.UI;

[UsedImplicitly]
public sealed class GpsBoundUserInterface : BoundUserInterface
{
    private GpsWindow? _window;

    public GpsBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<GpsWindow>();

        _window.OnUpdateGpsName += name =>
            SendMessage(new UpdateGpsNameMessage(name));

        _window.OnUpdateGpsDescription += desc =>
            SendMessage(new UpdateGpsDescriptionMessage(desc));

        _window.OnToggleBroadcast += enabled =>
            SendMessage(new ToggleGpsBroadcastMessage(enabled));

        _window.OnClose += Close;
        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_window == null)
            return;

        if (state is GpsUpdateState updateState)
            _window.UpdateState(updateState, EntMan);
    }
}
