using Content.Shared.Ninja.Components;
using Robust.Client.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Client.Ninja.Systems;

/// <summary>
/// Applies an acid-green color tint to the ninja while Spirit Form is active.
/// </summary>
public sealed partial class NinjaSpiritFormVisualsSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    // acid neon-green tint with slight transparency
    private static readonly Color SpiritColor = Color.FromHex("#00ff22");

    private readonly Dictionary<EntityUid, Color> _savedColors = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SpiritFormComponent, AfterAutoHandleStateEvent>(OnStateUpdated);
        SubscribeLocalEvent<SpiritFormComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStateUpdated(EntityUid suitUid, SpiritFormComponent comp, ref AfterAutoHandleStateEvent args)
    {
        var ninja = Transform(suitUid).ParentUid;
        if (!TryComp<SpriteComponent>(ninja, out var sprite))
            return;

        if (comp.Active)
        {
            if (!_savedColors.ContainsKey(ninja))
                _savedColors[ninja] = sprite.Color;

            _sprite.SetColor((ninja, sprite), SpiritColor);
        }
        else
        {
            if (_savedColors.Remove(ninja, out var original))
                _sprite.SetColor((ninja, sprite), original);
        }
    }

    private void OnShutdown(EntityUid suitUid, SpiritFormComponent comp, ComponentShutdown args)
    {
        var ninja = Transform(suitUid).ParentUid;
        if (!TryComp<SpriteComponent>(ninja, out var sprite))
            return;

        if (_savedColors.Remove(ninja, out var original))
            _sprite.SetColor((ninja, sprite), original);
    }
}
