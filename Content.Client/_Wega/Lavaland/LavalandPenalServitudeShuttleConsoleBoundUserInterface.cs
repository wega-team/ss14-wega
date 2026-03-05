using Content.Shared.Lavaland;
using Robust.Client.UserInterface;

namespace Content.Client._Wega.Lavaland;

public sealed class LavalandPenalServitudeConsoleBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private LavalandPenalServitudeConsoleWindow? _window;

    public LavalandPenalServitudeConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<LavalandPenalServitudeConsoleWindow>();
        _window.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;
        _window.OpenCentered();

        _window.OnClose += Close;

        _window.OnCallButtonPressed += () => SendMessage(new PenalServitudeLavalandShuttleCallMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is PenalServitudeLavalandShuttleConsoleState cast)
            _window?.UpdateState(cast);
    }
}
