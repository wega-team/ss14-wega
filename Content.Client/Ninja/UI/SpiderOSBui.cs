using Content.Shared.Ninja;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.IoC;
using Robust.Shared.Timing;

namespace Content.Client.Ninja.UI;

[UsedImplicitly]
public sealed class SpiderOSBui : BoundUserInterface
{
    [Dependency] private readonly IPlayerManager _player = default!;

    private SpiderOSWindow? _window;

    private string? _lastTerminalLine;
    private bool _showingLoadingScreen;
    private bool _transitionScheduled;

    public SpiderOSBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<SpiderOSWindow>();
        _window.OnStyleChanged       += (slot, index) => SendMessage(new SpiderOSSetStyleMessage(slot, index));
        _window.OnAbilityChanged     += (row, choice) => SendMessage(new SpiderOSSetAbilityMessage(row, choice));
        _window.OnActivate           += ShowActivationConfirm;
        _window.OnOpenShuttleConsole += () => SendMessage(new SpiderOSOpenShuttleConsoleMessage());

        if (_player.LocalEntity is { } player)
            _window.SetPlayerEntity(player);
    }

    private void ShowActivationConfirm()
    {
        var dialog = new SuitActivationConfirmDialog();
        dialog.OnConfirmed += () => SendMessage(new SpiderOSActivateMessage());
        dialog.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (_window == null || state is not SpiderOSBoundUserInterfaceState s)
            return;

        if (s.TerminalLine != null && s.TerminalLine != _lastTerminalLine)
        {
            _lastTerminalLine = s.TerminalLine;
            _window.AddTerminalLine(s.TerminalLine);
        }

        if (s.IsLoading && !_showingLoadingScreen)
        {
            _showingLoadingScreen = true;
            _window.AllowClose = false;
            _window.ShowLoadingScreen();
        }

        if (s.TerminalFinished && _showingLoadingScreen && !_transitionScheduled)
        {
            _transitionScheduled = true;
            Timer.Spawn(3000, () =>
            {
                if (_window == null)
                    return;
                _showingLoadingScreen = false;
                _transitionScheduled  = false;
                _lastTerminalLine     = null;
                _window.ClearTerminal();
                _window.ShowMainScreen();
                _window.AllowClose = true;
            });
        }

        if (!_showingLoadingScreen)
            _window.UpdateState(s.SuitGender, s.SuitStyleVariant, s.SuitColor,
                                s.IsActivated, s.AbilityChoices, s.ConsoleErrors,
                                s.ShuttleLinked, s.CoordX, s.CoordY, s.InNullspace);
    }
}
