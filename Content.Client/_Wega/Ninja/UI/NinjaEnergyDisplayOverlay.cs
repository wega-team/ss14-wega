using Content.Client.Power.EntitySystems;
using Content.Client.Resources;
using Content.Shared.Ninja.Components;
using Content.Shared.PowerCell;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.IoC;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Numerics;

namespace Content.Client._Wega.Ninja.UI;

public sealed class NinjaEnergyDisplayOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;

    private readonly SpriteSystem _sprite;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    private const string RsiPath = "/Textures/_Wega/Interface/display_ninja";
    private const float DisplaySize = 130f;
    private const float WarningThreshold = 0.15f;

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
        var cy = ((float) bounds.Top + (float) bounds.Bottom) / 2f;

        var x = cx - DisplaySize / 2f;
        var y = cy - DisplaySize / 2f + 400f;

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
}
