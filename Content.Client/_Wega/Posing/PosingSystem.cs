using System.Numerics;
using Content.Shared.Input;
using Content.Shared.Posing;
using Robust.Client.GameObjects;
using Robust.Client.Input;
using Robust.Client.Player;

namespace Content.Client.Posing;

public sealed partial class PosingSystem : SharedPosingSystem
{
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private readonly Dictionary<EntityUid, (
        Vector2 Offset,
        Angle Angle,
        Vector2 TargetOffset,
        Angle TargetAngle,
        float LerpTime,
        bool IsReturning
    )> _interpolationState = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PosingComponent, ComponentRemove>(OnComponentRemove);
        SubscribeLocalEvent<PosingComponent, AfterAutoHandleStateEvent>(OnAfterHandleState);

        var posing = _input.Contexts.New("posing", "common");
        posing.AddFunction(ContentKeyFunctions.TogglePosing);
        posing.AddFunction(ContentKeyFunctions.PosingOffsetUp);
        posing.AddFunction(ContentKeyFunctions.PosingOffsetDown);
        posing.AddFunction(ContentKeyFunctions.PosingOffsetLeft);
        posing.AddFunction(ContentKeyFunctions.PosingOffsetRight);
        posing.AddFunction(ContentKeyFunctions.PosingRotatePositive);
        posing.AddFunction(ContentKeyFunctions.PosingRotateNegative);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var query = EntityQueryEnumerator<PosingComponent>();
        while (query.MoveNext(out var uid, out var posing))
        {
            if (posing.Posing)
            {
                if (!_interpolationState.TryGetValue(uid, out var state))
                {
                    state = (
                        posing.CurrentOffset,
                        posing.CurrentAngle,
                        posing.CurrentOffset,
                        posing.CurrentAngle,
                        0f,
                        false
                    );
                    _interpolationState[uid] = state;
                }

                if (state.IsReturning)
                {
                    state = (
                        posing.CurrentOffset,
                        posing.CurrentAngle,
                        posing.CurrentOffset,
                        posing.CurrentAngle,
                        0f,
                        false
                    );
                }

                if (state.TargetOffset != posing.CurrentOffset || state.TargetAngle != posing.CurrentAngle)
                {
                    state.TargetOffset = posing.CurrentOffset;
                    state.TargetAngle = posing.CurrentAngle;
                    state.LerpTime = 0f;
                }

                state.LerpTime += frameTime;
                var t = Math.Min(1f, state.LerpTime / 0.15f);

                var newOffset = Vector2.Lerp(state.Offset, state.TargetOffset, t);
                var newAngle = Angle.FromDegrees(
                    MathHelper.Lerp(state.Angle.Degrees, state.TargetAngle.Degrees, t)
                );

                if (t >= 1f)
                {
                    state.Offset = state.TargetOffset;
                    state.Angle = state.TargetAngle;
                }
                else
                {
                    state.Offset = newOffset;
                    state.Angle = newAngle;
                }

                _sprite.SetOffset(uid, posing.DefaultOffset + state.Offset);
                _sprite.SetRotation(uid, state.Angle);

                _interpolationState[uid] = state;
            }
            else
            {
                if (_interpolationState.TryGetValue(uid, out var state))
                {
                    if (!state.IsReturning)
                    {
                        state.IsReturning = true;
                        state.LerpTime = 0f;
                        state.TargetOffset = Vector2.Zero;
                        state.TargetAngle = Angle.Zero;
                    }

                    state.LerpTime += frameTime;
                    var t = Math.Min(1f, state.LerpTime / 0.2f);

                    var newOffset = Vector2.Lerp(state.Offset, state.TargetOffset, t);
                    var newAngle = Angle.FromDegrees(
                        MathHelper.Lerp(state.Angle.Degrees, state.TargetAngle.Degrees, t)
                    );

                    if (t >= 1f)
                    {
                        _interpolationState.Remove(uid);
                        _sprite.SetOffset(uid, posing.DefaultOffset);
                        _sprite.SetRotation(uid, Angle.FromDegrees(posing.DefaultAngle));
                    }
                    else
                    {
                        state.Offset = newOffset;
                        state.Angle = newAngle;
                        _interpolationState[uid] = state;
                        _sprite.SetOffset(uid, posing.DefaultOffset + state.Offset);
                        _sprite.SetRotation(uid, state.Angle);
                    }
                }
            }
        }
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _input.Contexts.Remove("posing");
        _interpolationState.Clear();
    }

    protected override void TogglePosing(Entity<PosingComponent?> entity)
    {
        base.TogglePosing(entity);

        if (!Resolve(entity, ref entity.Comp, false))
            return;

        _input.Contexts.SetActiveContext(entity.Comp.Posing ? "posing" : entity.Comp.DefaultInputContext);

        if (entity.Comp.Posing)
        {
            _interpolationState[entity.Owner] = (
                entity.Comp.CurrentOffset,
                entity.Comp.CurrentAngle,
                entity.Comp.CurrentOffset,
                entity.Comp.CurrentAngle,
                0f, false
            );
            _sprite.SetOffset(entity.Owner, entity.Comp.DefaultOffset + entity.Comp.CurrentOffset);
            _sprite.SetRotation(entity.Owner, entity.Comp.CurrentAngle);
        }
        else
        {
            if (_interpolationState.TryGetValue(entity.Owner, out var state))
            {
                state.IsReturning = true;
                state.LerpTime = 0f;
                state.TargetOffset = Vector2.Zero;
                state.TargetAngle = Angle.Zero;
                _interpolationState[entity.Owner] = state;
            }
        }
    }

    private void OnComponentRemove(EntityUid uid, PosingComponent component, ComponentRemove args)
    {
        _interpolationState.Remove(uid);
    }

    private void OnAfterHandleState(EntityUid uid, PosingComponent component, ref AfterAutoHandleStateEvent args)
    {
        if (_playerManager.LocalEntity == uid)
            return;

        if (component.Posing && _interpolationState.TryGetValue(uid, out var state))
        {
            state.TargetOffset = component.CurrentOffset;
            state.TargetAngle = component.CurrentAngle;
            state.LerpTime = 0f;
            state.IsReturning = false;
            _interpolationState[uid] = state;
        }
        else if (component.Posing && !_interpolationState.ContainsKey(uid))
        {
            _interpolationState[uid] = (
                component.CurrentOffset,
                component.CurrentAngle,
                component.CurrentOffset,
                component.CurrentAngle,
                0f, false
            );
        }
        else if (!component.Posing && _interpolationState.ContainsKey(uid) && !_interpolationState[uid].IsReturning)
        {
            var returnState = _interpolationState[uid];
            returnState.IsReturning = true;
            returnState.LerpTime = 0f;
            returnState.TargetOffset = Vector2.Zero;
            returnState.TargetAngle = Angle.Zero;
            _interpolationState[uid] = returnState;
        }
    }
}
