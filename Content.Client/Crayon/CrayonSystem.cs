// Corvax-Wega-Full-Edit
using Content.Client.Items;
using Content.Shared.Crayon;
using Content.Shared.Hands;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Timing;

namespace Content.Client.Crayon;

public sealed class CrayonSystem : SharedCrayonSystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private CrayonPreviewOverlay? _previewOverlay;

    public override void Initialize()
    {
        base.Initialize();

        Subs.ItemStatus<CrayonComponent>(ent => new StatusControl(ent));
        SubscribeLocalEvent<CrayonComponent, HandSelectedEvent>(OnCrayonSelected);
        SubscribeLocalEvent<CrayonComponent, HandDeselectedEvent>(OnCrayonDeselected);
        SubscribeLocalEvent<CrayonComponent, AfterAutoHandleStateEvent>(CrayonAfterAutoState);
    }

    private void OnCrayonSelected(EntityUid uid, CrayonComponent component, HandSelectedEvent args)
    {
        _previewOverlay ??= new CrayonPreviewOverlay(_sprite);
        _overlay.AddOverlay(_previewOverlay);
        component.UIUpdateNeeded = true;
    }

    private void OnCrayonDeselected(EntityUid uid, CrayonComponent component, HandDeselectedEvent args)
    {
        if (_previewOverlay != null)
        {
            _overlay.RemoveOverlay(_previewOverlay);
            _previewOverlay = null;
        }
        component.UIUpdateNeeded = true;
    }

    private void CrayonAfterAutoState(EntityUid uid, CrayonComponent comp, AfterAutoHandleStateEvent args)
    {
        comp.UIUpdateNeeded = true;
    }

    private sealed class StatusControl : Control
    {
        private readonly CrayonComponent _parent;

        public StatusControl(CrayonComponent parent)
        {
            _parent = parent;
            _parent.UIUpdateNeeded = true;
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);

            if (!_parent.UIUpdateNeeded)
                return;

            _parent.UIUpdateNeeded = false;
        }
    }
}
