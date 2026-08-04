using Content.Shared.Body;
using Content.Shared.DragDrop;

namespace Content.Shared._Wega.Ninja;

/// <summary>
/// Runs on both client and server so the client can highlight the machine
/// as a valid drag-drop target.
/// </summary>
public sealed class SharedMindScanMachineSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MindScanMachineComponent, CanDropTargetEvent>(OnCanDrop);
    }

    private void OnCanDrop(EntityUid uid, MindScanMachineComponent comp, ref CanDropTargetEvent args)
    {
        args.CanDrop = HasComp<BodyComponent>(args.Dragged) && comp.BodyContainer?.ContainedEntity == null;
        args.Handled = true;
    }
}
