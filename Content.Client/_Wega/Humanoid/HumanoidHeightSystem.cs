using System.Numerics;
using Content.Shared.Humanoid;
using Robust.Client.GameObjects;

namespace Content.Client.Humanoid;

public sealed class HumanoidHeightSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HumanoidProfileComponent, AfterAutoHandleStateEvent>(OnAfterHandleState);
    }

    private void OnAfterHandleState(EntityUid uid, HumanoidProfileComponent component, ref AfterAutoHandleStateEvent args)
    {
        UpdateHeightScale(uid, component);
    }

    private void UpdateHeightScale(EntityUid uid, HumanoidProfileComponent component)
    {
        if (!HasComp<SpriteComponent>(uid))
            return;

        var scale = ConvertHeightToScale(component.Height);
        _sprite.SetScale(uid, new Vector2(scale, scale));
    }

    private float ConvertHeightToScale(float height)
    {
        const float minH = 140f, maxH = 300f;
        const float minS = 0.65f, maxS = 1.5f;

        var t = MathF.Pow((height - minH) / (maxH - minH), 0.7f);
        return Math.Clamp(minS + t * (maxS - minS), minS, maxS);
    }
}
