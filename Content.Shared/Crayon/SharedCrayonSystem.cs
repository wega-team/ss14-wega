// Corvax-Wega-Full-Edit-start
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Input;
using Robust.Shared.Input.Binding;

namespace Content.Shared.Crayon;

public abstract class SharedCrayonSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    private readonly Angle _rotationIncrement = Angle.FromDegrees(2.5);

    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.MouseWheelUp, new PointerInputCmdHandler(MouseWheelUp))
            .Bind(ContentKeyFunctions.MouseWheelDown, new PointerInputCmdHandler(MouseWheelDown))
            .Register<SharedCrayonSystem>();
    }

    private bool MouseWheelUp(in PointerInputCmdHandler.PointerInputCmdArgs args)
    {
        return HandleMouseWheel(args, _rotationIncrement);
    }

    private bool MouseWheelDown(in PointerInputCmdHandler.PointerInputCmdArgs args)
    {
        return HandleMouseWheel(args, -_rotationIncrement);
    }

    private bool HandleMouseWheel(in PointerInputCmdHandler.PointerInputCmdArgs args, Angle rotation)
    {
        if (args.Session?.AttachedEntity is not { } player)
            return false;

        var active = _hands.GetActiveItem(player);
        if (!HasComp<CrayonComponent>(active))
            return false;

        var rotateEvent = new CrayonRotateEvent(rotation);
        RaiseNetworkEvent(rotateEvent);

        return true;
    }
}
// Corvax-Wega-Full-Edit-end
