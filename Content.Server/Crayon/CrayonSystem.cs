using System.Linq;
using System.Numerics;
using Content.Server.Administration.Logs;
using Content.Server.Decals;
using Content.Server.Popups;
using Content.Shared.Charges.Systems;
using Content.Shared.Crayon;
using Content.Shared.Database;
using Content.Shared.Decals;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server.Crayon;

public sealed class CrayonSystem : SharedCrayonSystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly DecalSystem _decals = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedChargesSystem _charges = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!; // Corvax-Wega-Add

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CrayonComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<CrayonComponent, CrayonSelectMessage>(OnCrayonBoundUI);
        SubscribeLocalEvent<CrayonComponent, CrayonColorMessage>(OnCrayonBoundUIColor);
        SubscribeLocalEvent<CrayonComponent, UseInHandEvent>(OnCrayonUse);
        SubscribeLocalEvent<CrayonComponent, AfterInteractEvent>(OnCrayonAfterInteract, after: [typeof(IngestionSystem)]);
        SubscribeLocalEvent<CrayonComponent, DroppedEvent>(OnCrayonDropped);

        SubscribeNetworkEvent<CrayonRotateEvent>(OnCrayonRotate); // Corvax-Wega-Add
    }

    private void OnMapInit(Entity<CrayonComponent> ent, ref MapInitEvent args)
    {
        // Get the first one from the catalog and set it as default
        var decal = _prototypeManager.EnumeratePrototypes<DecalPrototype>().FirstOrDefault(x => x.Tags.Contains("crayon"));
        ent.Comp.SelectedState = decal?.ID ?? string.Empty;
        Dirty(ent);
    }

    // Runs after IngestionSystem so it doesn't bulldoze force-feeding
    private void OnCrayonAfterInteract(EntityUid uid, CrayonComponent component, AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        if (_charges.IsEmpty(uid))
        {
            if (component.DeleteEmpty)
                UseUpCrayon(uid, args.User);
            else
                _popup.PopupEntity(Loc.GetString("crayon-interact-not-enough-left-text"), uid, args.User);

            args.Handled = true;
            return;
        }

        if (!args.ClickLocation.IsValid(EntityManager))
        {
            _popup.PopupEntity(Loc.GetString("crayon-interact-invalid-location"), uid, args.User);
            args.Handled = true;
            return;
        }

        // Corvax-Wega-Edit-start
        var grid = _transform.GetGrid(args.User);
        Angle rot = grid != null ? _transform.GetWorldRotation(grid.Value) : 0;

        if (!_decals.TryAddDecal(component.SelectedState, args.ClickLocation.Offset(new Vector2(-0.5f, -0.5f)),
            out _, component.Color, rot + component.Angle, cleanable: true))
            return;
        // Corvax-Wega-Edit-end

        if (component.UseSound != null)
            _audio.PlayPvs(component.UseSound, uid, AudioParams.Default.WithVariation(0.125f));

        _charges.TryUseCharge(uid);

        _adminLogger.Add(LogType.CrayonDraw, LogImpact.Low,
            $"{ToPrettyString(args.User):user} drew a {component.Color:color} {component.SelectedState}");

        args.Handled = true;

        if (component.DeleteEmpty && _charges.IsEmpty(uid))
            UseUpCrayon(uid, args.User);
        else
            _uiSystem.ServerSendUiMessage(uid, CrayonUiKey.Key, new CrayonUsedMessage(component.SelectedState));
    }

    private void OnCrayonUse(EntityUid uid, CrayonComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (!_uiSystem.HasUi(uid, CrayonUiKey.Key))
            return;

        _uiSystem.TryToggleUi(uid, CrayonUiKey.Key, args.User);

        _uiSystem.SetUiState(uid, CrayonUiKey.Key, new CrayonBoundUserInterfaceState(component.SelectedState, component.SelectableColor, component.Color));
        args.Handled = true;
    }

    private void OnCrayonBoundUI(EntityUid uid, CrayonComponent component, CrayonSelectMessage args)
    {
        if (!_prototypeManager.TryIndex<DecalPrototype>(args.State, out var prototype) || !prototype.Tags.Contains("crayon"))
            return;

        component.SelectedState = args.State;
        Dirty(uid, component);
    }

    private void OnCrayonBoundUIColor(EntityUid uid, CrayonComponent component, CrayonColorMessage args)
    {
        // Ensure that the given color can be changed or already matches
        if (!component.SelectableColor || args.Color == component.Color)
            return;

        component.Color = args.Color;
        Dirty(uid, component);
    }

    private void OnCrayonDropped(EntityUid uid, CrayonComponent component, DroppedEvent args)
    {
        // TODO: Use the existing event.
        _uiSystem.CloseUi(uid, CrayonUiKey.Key, args.User);
    }

    private void UseUpCrayon(EntityUid uid, EntityUid user)
    {
        _popup.PopupEntity(Loc.GetString("crayon-interact-used-up-text", ("owner", uid)), user, user);
        QueueDel(uid);
    }

    // Corvax-Wega-Add-start
    private void OnCrayonRotate(CrayonRotateEvent args, EntitySessionEventArgs eventArgs)
    {
        if (eventArgs.SenderSession.AttachedEntity is not { } player)
            return;

        var active = _hands.GetActiveItem(player);
        if (!TryComp<CrayonComponent>(active, out var crayon))
            return;

        crayon.Angle += args.Angle;
        Dirty(active.Value, crayon);
    }
    // Corvax-Wega-Add-end
}
