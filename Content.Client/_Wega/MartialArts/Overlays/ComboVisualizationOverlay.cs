using Content.Client.UserInterface.Systems.Hotbar.Widgets;
using Content.Shared.CombatMode;
using Content.Shared.Martial.Arts.Components;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Enums;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.Utility;
using System.Numerics;

namespace Content.Client._Wega.MartialArts.Overlays;

public sealed partial class ComboVisualizationOverlay : Overlay
{
    [Dependency] private IEntityManager _entMan        = default!;
    [Dependency] private IPlayerManager _player        = default!;
    [Dependency] private IResourceCache _resourceCache = default!;
    [Dependency] private IUserInterfaceManager _uiManager = default!;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    private static readonly ResPath IntentsRsi = new("/Textures/_Wega/Interface/intents.rsi");

    private const float IconSize     = 32f;
    private const float IconGap      = 6f;
    private const float MarginRight  = 12f;
    private const float MarginBottom = 12f;

    public ComboVisualizationOverlay()
    {
        IoCManager.InjectDependencies(this);
        ZIndex = 10;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var localEnt = _player.LocalEntity;
        if (localEnt == null)
            return;

        if (!_resourceCache.TryGetResource<RSIResource>(IntentsRsi, out var rsiRes))
            return;

        var rsi    = rsiRes.RSI;
        var handle = args.ScreenHandle;
        var right  = (float) args.ViewportBounds.Right;
        var bottom = (float) args.ViewportBounds.Bottom;

        // Anchor above the hands hotbar so the icons are never covered by it,
        // regardless of window size / UI scale.
        var hotbarTop = GetHotbarTop(bottom);
        var intentY = hotbarTop - IconSize - MarginBottom;

        // Intent icon — always visible
        var intentState = GetIntentState(localEnt.Value);
        if (rsi.TryGetState(intentState, out var intentRsiState))
        {
            var intentX = right - IconSize - MarginRight;
            handle.DrawTextureRect(intentRsiState.Frame0,
                UIBox2.FromDimensions(intentX, intentY, IconSize, IconSize),
                Color.White.WithAlpha(0.9f));
        }

        // Combo icons — shown above the intent icon when active
        if (!_entMan.TryGetComponent<ComboVisualizationComponent>(localEnt.Value, out var comp)
            || comp.Icons.Count == 0)
            return;

        var total  = comp.Icons.Count * IconSize + (comp.Icons.Count - 1) * IconGap;
        var startX = right  - total  - MarginRight;
        var iconY  = intentY - IconSize - IconGap;

        for (var i = 0; i < comp.Icons.Count; i++)
        {
            var stateName = comp.Icons[i];
            if (!rsi.TryGetState(stateName, out var state))
                continue;

            var x = startX + i * (IconSize + IconGap);
            handle.DrawTextureRect(state.Frame0,
                UIBox2.FromDimensions(x, iconY, IconSize, IconSize),
                Color.White.WithAlpha(0.9f));
        }
    }

    /// <summary>
    /// Returns the physical-pixel Y of the top of the hands hotbar, so HUD elements can be placed
    /// above it. Falls back to the given screen bottom when the hotbar isn't present.
    /// </summary>
    private float GetHotbarTop(float fallbackBottom)
    {
        var hotbar = _uiManager.GetActiveUIWidgetOrNull<HotbarGui>();
        if (hotbar?.HandContainer is { } hands && hands.VisibleInTree)
            return hands.GlobalPixelPosition.Y;
        return fallbackBottom;
    }

    private string GetIntentState(EntityUid uid)
    {
        if (!_entMan.TryGetComponent<CombatModeComponent>(uid, out var combat))
            return "help";

        if (!combat.IsInCombatMode)
            return "help";

        return combat.CanDisarm == true ? "disarm" : "harm";
    }
}
