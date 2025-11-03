using Content.Shared.Crayon;
using Content.Shared.Decals;
using Content.Shared.Hands.EntitySystems;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using System.Numerics;

namespace Content.Client.Crayon;

public sealed class CrayonPreviewOverlay : Overlay
{
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private readonly SpriteSystem _sprite;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowEntities;

    public CrayonPreviewOverlay(SpriteSystem sprite)
    {
        IoCManager.InjectDependencies(this);
        _sprite = sprite;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var worldHandle = args.WorldHandle;

        if (_player.LocalEntity is not { } player)
            return;

        var hand = _entMan.System<SharedHandsSystem>();
        var active = hand.GetActiveItem(player);

        if (!_entMan.TryGetComponent(active, out CrayonComponent? crayon))
            return;

        if (!_proto.TryIndex<DecalPrototype>(crayon.SelectedState, out var decalProto))
            return;

        var texture = _sprite.Frame0(decalProto.Sprite);

        var mousePos = _input.MouseScreenPosition;
        var worldPos = _eye.ScreenToMap(mousePos);

        if (worldPos.MapId != args.MapId)
            return;

        var transform = _entMan.System<TransformSystem>();
        var playerPos = transform.GetMapCoordinates(player);
        var distance = (worldPos.Position - playerPos.Position).Length();

        var alpha = 0.7f;
        if (distance > 2f)
            alpha = 0.3f;
        else if (distance > 1.5f)
            alpha = 0.5f;

        var color = crayon.Color.WithAlpha(alpha);
        var position = worldPos.Position - new Vector2(0.5f, 0.5f);

        var grid = transform.GetGrid(player);
        Angle rot = grid != null ? transform.GetWorldRotation(grid.Value) : 0;

        worldHandle.DrawTexture(texture, position, rot + crayon.Angle, color);
    }
}
