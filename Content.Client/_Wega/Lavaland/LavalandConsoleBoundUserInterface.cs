using Content.Shared.Lavaland;
using Robust.Client.UserInterface;

namespace Content.Client._Wega.Lavaland;

public sealed class LavalandConsoleBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private LavalandConsoleWindow? _window;

    public LavalandConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<LavalandConsoleWindow>();
        _window.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;
        _window.OpenCentered();

        _window.OnClose += Close;

        _window.OnCallButtonPressed += () => SendMessage(new LavalandShuttleCallMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is LavalandShuttleConsoleState cast)
            _window?.UpdateState(cast);
    }
}
