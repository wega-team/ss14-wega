using Content.Shared.Visuals;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;

namespace Content.Client.Visuals;

public sealed class FadeSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FadeComponent, ComponentStartup>(OnFadeStartup);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<FadeComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var fadeComp, out var spriteComp))
        {
            UpdateFade(uid, fadeComp, spriteComp);
        }
    }

    private void OnFadeStartup(EntityUid uid, FadeComponent component, ComponentStartup args)
    {
        if (component.StartTime == TimeSpan.Zero)
        {
            component.StartTime = _gameTiming.CurTime;
        }

        _sprite.SetColor(uid, component.StartColor);
    }

    private void UpdateFade(EntityUid uid, FadeComponent component, SpriteComponent sprite)
    {
        var currentTime = _gameTiming.CurTime;
        var elapsed = (currentTime - component.StartTime).TotalSeconds;

        if (elapsed <= 0)
            return;

        if (elapsed >= component.FadeDuration)
        {
            _sprite.SetColor(uid, component.TargetColor);
            return;
        }

        var progress = (float)(elapsed / component.FadeDuration);

        var currentColor = Color.InterpolateBetween(
            component.StartColor,
            component.TargetColor,
            progress
        );

        _sprite.SetColor(uid, currentColor);
    }

    public void StartFade(EntityUid uid, float duration = 5f, Color? startColor = null, Color? targetColor = null,
        FadeComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
        {
            component = AddComp<FadeComponent>(uid);
        }

        component.FadeDuration = duration;
        component.StartColor = startColor ?? Color.White;
        component.TargetColor = targetColor ?? component.StartColor.WithAlpha(0f);
        component.StartTime = _gameTiming.CurTime;

        _sprite.SetColor(uid, component.StartColor);
    }

    public void StopFade(EntityUid uid, FadeComponent? component = null)
    {
        if (Resolve(uid, ref component, false))
        {
            RemComp<FadeComponent>(uid);
        }
    }

    public float GetFadeProgress(EntityUid uid, FadeComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return 0f;

        var elapsed = (_gameTiming.CurTime - component.StartTime).TotalSeconds;
        return (float)(elapsed / component.FadeDuration);
    }
}
