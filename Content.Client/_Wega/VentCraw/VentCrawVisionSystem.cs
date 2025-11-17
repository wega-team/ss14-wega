using Content.Client.SubFloor;
using Content.Shared.VentCraw;
using Robust.Client.Player;
using Robust.Shared.Timing;

namespace Content.Client.VentCraw;

public sealed class VentCrawVisionSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly SubFloorHideSystem _subFloorHideSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VentCrawlerComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<VentCrawlerComponent, ComponentShutdown>(OnComponentShutdown);
    }

    private void OnComponentStartup(EntityUid uid, VentCrawlerComponent component, ComponentStartup args)
    {
        UpdateVision(component.InTube);
    }

    private void OnComponentShutdown(EntityUid uid, VentCrawlerComponent component, ComponentShutdown args)
    {
        UpdateVision(false);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        var player = _player.LocalSession?.AttachedEntity;
        if (player == null)
            return;

        if (!TryComp<VentCrawlerComponent>(player, out var ventCrawler))
            return;

        UpdateVision(ventCrawler.InTube);
    }

    private void UpdateVision(bool inTube)
    {
        _subFloorHideSystem.ShowVentPipe = inTube;
    }
}
