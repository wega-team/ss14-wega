using Content.Shared.Ninja.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Ninja.Systems;

public sealed class NinjaCloneVisualSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private readonly HashSet<EntityUid> _pending = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NinjaCloneVisualComponent, AfterAutoHandleStateEvent>(OnStateUpdated);
    }

    private void OnStateUpdated(EntityUid uid, NinjaCloneVisualComponent comp, ref AfterAutoHandleStateEvent args)
    {
        if (Exists(comp.NinjaUid))
            _pending.Add(uid);
    }

    public override void FrameUpdate(float frameTime)
    {
        foreach (var uid in _pending)
        {
            if (!TryComp<NinjaCloneVisualComponent>(uid, out var comp) || !Exists(comp.NinjaUid))
                continue;
            _sprite.CopySprite(comp.NinjaUid, uid);
        }
        _pending.Clear();
    }
}
