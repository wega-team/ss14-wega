using Content.Client.Gameplay;
using Content.Client.Ghost;
using Content.Client.UserInterface.Systems.Gameplay;
using Content.Client.UserInterface.Systems.Ghost.Widgets;
using Content.Client.Wega.Ghost.Respawn; // Corvax-Wega-GhostRespawn
using Content.Shared.Ghost;
using Content.Shared.CCVar; // Corvax-Wega-GhostRespawn
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Console; // Corvax-Wega-GhostRespawn
using Robust.Shared.Configuration; // Corvax-Wega-GhostRespawn

namespace Content.Client.UserInterface.Systems.Ghost;

// TODO hud refactor BEFORE MERGE fix ghost gui being too far up
public sealed class GhostUIController : UIController, IOnSystemChanged<GhostSystem>, IOnSystemChanged<GhostRespawnSystem> // Corvax-Wega-GhostRespawn
{
    [Dependency] private readonly IEntityNetworkManager _net = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!; // Corvax-Wega-GhostRespawn
    [Dependency] private readonly IConsoleHost _consoleHost = default!; // Corvax-Wega-GhostRespawn

    [UISystemDependency] private readonly GhostSystem? _system = default;
    [UISystemDependency] private readonly GhostRespawnSystem? _respawn = default; // Corvax-Wega-GhostRespawn

    private GhostGui? Gui => UIManager.GetActiveUIWidgetOrNull<GhostGui>();

    public override void Initialize()
    {
        base.Initialize();

        var gameplayStateLoad = UIManager.GetUIController<GameplayStateLoadController>();
        gameplayStateLoad.OnScreenLoad += OnScreenLoad;
        gameplayStateLoad.OnScreenUnload += OnScreenUnload;
    }

    private void OnScreenLoad()
    {
        LoadGui();
    }

    private void OnScreenUnload()
    {
        UnloadGui();
    }

    public void OnSystemLoaded(GhostSystem system)
    {
        system.PlayerRemoved += OnPlayerRemoved;
        system.PlayerUpdated += OnPlayerUpdated;
        system.PlayerAttached += OnPlayerAttached;
        system.PlayerDetached += OnPlayerDetached;
        system.GhostWarpsResponse += OnWarpsResponse;
        system.GhostRoleCountUpdated += OnRoleCountUpdated;
    }

    public void OnSystemUnloaded(GhostSystem system)
    {
        system.PlayerRemoved -= OnPlayerRemoved;
        system.PlayerUpdated -= OnPlayerUpdated;
        system.PlayerAttached -= OnPlayerAttached;
        system.PlayerDetached -= OnPlayerDetached;
        system.GhostWarpsResponse -= OnWarpsResponse;
        system.GhostRoleCountUpdated -= OnRoleCountUpdated;
    }

    // Corvax-Wega-GhostRespawn-start
    private void UpdateGhostRespawn(TimeSpan? timeOfDeath)
    {
        Gui?.UpdateGhostRespawn(timeOfDeath);
    }
    // Corvax-Wega-GhostRespawn-end

    public void UpdateGui()
    {
        if (Gui == null)
        {
            return;
        }

        Gui.Visible = _system?.IsGhost ?? false;
        Gui.Update(
            _system?.AvailableGhostRoleCount,
            _system?.Player?.CanReturnToBody,
            _respawn?.GhostRespawnTime, // Corvax-Wega-GhostRespawn
            _cfg.GetCVar(WegaCVars.GhostRespawnTime) // Corvax-Wega-GhostRespawn
        );
    }

    private void OnPlayerRemoved(GhostComponent component)
    {
        Gui?.Hide();
        UpdateGhostRespawn(component.TimeOfDeath); // Corvax-Wega-GhostRespawn
    }

    private void OnPlayerUpdated(GhostComponent component)
    {
        UpdateGui();
        UpdateGhostRespawn(component.TimeOfDeath); // Corvax-Wega-GhostRespawn
    }

    private void OnPlayerAttached(GhostComponent component)
    {
        if (Gui == null)
            return;

        Gui.Visible = true;
        UpdateGui();
    }

    private void OnPlayerDetached()
    {
        Gui?.Hide();
    }

    private void OnWarpsResponse(GhostWarpsResponseEvent msg)
    {
        if (Gui?.TargetWindow is not { } window)
            return;

        window.UpdateWarps(msg.Warps);
        window.Populate();
    }

    private void OnRoleCountUpdated(GhostUpdateGhostRoleCountEvent msg)
    {
        UpdateGui();
    }

    private void OnWarpClicked(NetEntity player)
    {
        var msg = new GhostWarpToTargetRequestEvent(player);
        _net.SendSystemNetworkMessage(msg);
    }

    private void OnGhostnadoClicked()
    {
        var msg = new GhostnadoRequestEvent();
        _net.SendSystemNetworkMessage(msg);
    }

    public void LoadGui()
    {
        if (Gui == null)
            return;

        Gui.RequestWarpsPressed += RequestWarps;
        Gui.ReturnToBodyPressed += ReturnToBody;
        Gui.GhostRolesPressed += GhostRolesPressed;
        Gui.TargetWindow.WarpClicked += OnWarpClicked;
        Gui.TargetWindow.OnGhostnadoClicked += OnGhostnadoClicked;
        Gui.GhostRespawnPressed += GuiOnGhostRespawnPressed; // Corvax-Wega-GhostRespawn

        UpdateGui();
    }

    public void UnloadGui()
    {
        if (Gui == null)
            return;

        Gui.RequestWarpsPressed -= RequestWarps;
        Gui.ReturnToBodyPressed -= ReturnToBody;
        Gui.GhostRolesPressed -= GhostRolesPressed;
        Gui.TargetWindow.WarpClicked -= OnWarpClicked;

        Gui.Hide();
    }

    private void ReturnToBody()
    {
        _system?.ReturnToBody();
    }

    private void RequestWarps()
    {
        _system?.RequestWarps();
        Gui?.TargetWindow.Populate();
        Gui?.TargetWindow.OpenCentered();
    }

    private void GhostRolesPressed()
    {
        _system?.OpenGhostRoles();
    }

    // Corvax-Wega-GhostRespawn-start
    private void GuiOnGhostRespawnPressed()
    {
        _consoleHost.ExecuteCommand("ghostrespawn");
    }

    public void OnSystemLoaded(GhostRespawnSystem system)
    {
        system.GhostRespawn += OnGhostRespawn;
    }
    public void OnSystemUnloaded(GhostRespawnSystem system)
    {
        system.GhostRespawn -= OnGhostRespawn;
    }
    private void OnGhostRespawn()
    {
        UpdateGui();
    }
    // Corvax-Wega-GhostRespawn-end
}
