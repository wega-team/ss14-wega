using Content.Shared.Ninja.Components;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.Utility;
using System.Numerics;

namespace Content.Client.Ninja.Overlays;

/// <summary>
/// Screen-space overlay that renders the current Widow Art combo input sequence
/// (harm / disarm / help / grab icons from intents.rsi) at the center-bottom of the screen.
/// Active whenever the local player has <see cref="NinjaConcentrationComponent"/>.
/// Independent of hand-item display CVars.
/// </summary>
public sealed class NinjaComboOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entMan        = default!;
    [Dependency] private readonly IPlayerManager _player        = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    private static readonly ResPath IntentsRsi = new("/Textures/_Wega/Interface/intents.rsi");

    private const float IconSize    = 48f;
    private const float IconGap     = 8f;
    private const float RightMargin = 40f;
    private const float VerticalCenter = 0.983f; // fraction of screen height

    public NinjaComboOverlay()
    {
        IoCManager.InjectDependencies(this);
        ZIndex = 10;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var localEnt = _player.LocalEntity;
        if (localEnt == null)
            return;

        if (!_entMan.TryGetComponent<NinjaConcentrationComponent>(localEnt.Value, out var conc))
            return;

        var history = conc.ComboHistory;
        if (history.Count == 0)
            return;

        if (!_resourceCache.TryGetResource<RSIResource>(IntentsRsi, out var rsiRes))
            return;

        var rsi    = rsiRes.RSI;
        var handle = args.ScreenHandle;
        var vp     = args.ViewportBounds;
        var total  = history.Count * IconSize + (history.Count - 1) * IconGap;
        var startX = vp.Right - RightMargin - total;
        var iconY  = vp.Top + (vp.Bottom - vp.Top) * VerticalCenter - IconSize / 2f;

        for (var i = 0; i < history.Count; i++)
        {
            var stateName = history[i] switch
            {
                WidowInput.Harm   => "harm",
                WidowInput.Disarm => "disarm",
                WidowInput.Help   => "help",
                WidowInput.Grab   => "grab",
                _                 => null,
            };

            if (stateName == null || !rsi.TryGetState(stateName, out var state))
                continue;

            var x = startX + i * (IconSize + IconGap);
            handle.DrawTextureRect(state.Frame0,
                UIBox2.FromDimensions(x, iconY, IconSize, IconSize),
                Color.White.WithAlpha(0.9f));
        }
    }
}
