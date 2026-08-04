using Content.Client.Power.EntitySystems;
using Content.Client.Resources;
using Content.Client.UserInterface.Systems.Hotbar.Widgets;
using Content.Shared.Ninja.Components;
using Content.Shared.PowerCell;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Enums;
using Robust.Shared.IoC;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Numerics;

namespace Content.Client._Wega.Ninja.UI;

public sealed partial class NinjaEnergyDisplayOverlay : Overlay
{
    [Dependency] private IEntityManager _entMan = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private IResourceCache _resourceCache = default!;
    [Dependency] private IUserInterfaceManager _uiManager = default!;

    private readonly SpriteSystem _sprite;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    private const string RsiPath = "/Textures/_Wega/Interface/display_ninja";
    private const float DisplaySize = 130f;
    private const float WarningThreshold = 0.15f;
    // Gap kept between the display's bottom and the top of the hands hotbar.
    private const float HotbarMargin = 8f;

    private readonly Font _font;

    public NinjaEnergyDisplayOverlay()
    {
        IoCManager.InjectDependencies(this);
        ZIndex = 10;
        _sprite = _entMan.System<SpriteSystem>();
        _font = _resourceCache.GetFont("/Fonts/NotoSans/NotoSans-Regular.ttf", 16);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var localEnt = _player.LocalEntity;
        if (localEnt == null)
            return;

        if (!_entMan.TryGetComponent<SpaceNinjaComponent>(localEnt.Value, out var ninja) || ninja.Suit == null)
            return;

        if (!_entMan.TryGetComponent<SpiderOSComponent>(ninja.Suit.Value, out var spiderOS) || !spiderOS.IsActivated)
            return;

        var powerCell = _entMan.System<PowerCellSystem>();
        if (!powerCell.TryGetBatteryFromSlot(ninja.Suit.Value, out var battery))
            return;

        var batterySystem = _entMan.System<BatterySystem>();
        var charge = batterySystem.GetCharge(battery.Value.AsNullable());
        var maxCharge = battery.Value.Comp.MaxCharge;
        var pct = maxCharge > 0f ? charge / maxCharge : 0f;
        var isWarning = pct <= WarningThreshold;
        var colorIdx = spiderOS.SuitColor;

        var colorStr = colorIdx switch { 0 => "red", 1 => "blue", _ => "green" };
        var stateName = isWarning
            ? $"ninja_energy_display_{colorStr}_warning"
            : $"ninja_energy_display_{colorStr}";

        var spec = new SpriteSpecifier.Rsi(new ResPath(RsiPath), stateName);
        var texture = _sprite.GetFrame(spec, _gameTiming.CurTime);

        var handle = args.ScreenHandle;
        var bounds = args.ViewportBounds;
        var cx = ((float) bounds.Left + (float) bounds.Right) / 2f;

        var x = cx - DisplaySize / 2f;
        // Anchor the display just above the hands hotbar so it is never covered by it,
        // regardless of window size / UI scale.
        var y = GetHotbarTop((float) bounds.Bottom) - DisplaySize - HotbarMargin;

        handle.DrawTextureRect(texture, UIBox2.FromDimensions(x, y, DisplaySize, DisplaySize), Color.White);

        var chargeStr = $"{(int) charge}";
        var textWidth = 0f;
        foreach (var rune in chargeStr.EnumerateRunes())
            textWidth += _font.GetCharMetrics(rune, 1f)?.Advance ?? 0;

        handle.DrawString(_font,
            new Vector2(x + (DisplaySize - textWidth) / 2f, y + DisplaySize * 0.63f),
            chargeStr,
            Color.White);
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
}
