using Content.Shared.CombatMode;
using Content.Shared.Martial.Arts.Components;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.Utility;
using System.Numerics;

namespace Content.Client._Wega.MartialArts.Overlays;

public sealed class ComboVisualizationOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entMan        = default!;
    [Dependency] private readonly IPlayerManager _player        = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;

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

        // Intent icon — always visible
        var intentState = GetIntentState(localEnt.Value);
        if (rsi.TryGetState(intentState, out var intentRsiState))
        {
            var intentX = right - IconSize - MarginRight;
            var intentY = bottom - IconSize - MarginBottom;
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
        var iconY  = bottom - IconSize - MarginBottom - IconSize - IconGap;

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

    private string GetIntentState(EntityUid uid)
    {
        if (!_entMan.TryGetComponent<CombatModeComponent>(uid, out var combat))
            return "help";

        if (!combat.IsInCombatMode)
            return "help";

        return combat.CanDisarm == true ? "disarm" : "harm";
    }
}
