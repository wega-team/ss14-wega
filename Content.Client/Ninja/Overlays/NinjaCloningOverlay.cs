using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.Timing;

namespace Content.Client.Ninja.Overlays;

/// <summary>
/// Full-screen RSI animation overlay shown while a ninja is waiting for their clone to spawn.
/// </summary>
public sealed class NinjaCloningOverlay : Overlay
{
    private const string RsiPath = "/Textures/_Wega/Effects/ninjarestart.rsi";
    private const string RsiState = "ninjarestart";

    [Dependency] private readonly IResourceCache _resourceCache = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private readonly Texture[] _frames;
    private readonly float[] _frameDelays;
    private int _frameIndex;
    private float _frameTimer;

    public NinjaCloningOverlay()
    {
        IoCManager.InjectDependencies(this);
        ZIndex = 200;

        var rsi = _resourceCache.GetResource<RSIResource>(RsiPath).RSI;
        if (rsi.TryGetState(RsiState, out var state))
        {
            _frames = state.GetFrames(RsiDirection.South);
            _frameDelays = state.GetDelays();
        }
        else
        {
            _frames = [];
            _frameDelays = [];
        }
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (_frames.Length == 0)
            return;

        _frameTimer += args.DeltaSeconds;
        while (_frameDelays.Length > 0 && _frameTimer >= _frameDelays[_frameIndex])
        {
            _frameTimer -= _frameDelays[_frameIndex];
            _frameIndex = (_frameIndex + 1) % _frames.Length;
        }
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_frames.Length == 0)
            return;

        var texture = _frames[_frameIndex];
        args.WorldHandle.DrawTextureRect(texture, args.WorldBounds);
    }
}
