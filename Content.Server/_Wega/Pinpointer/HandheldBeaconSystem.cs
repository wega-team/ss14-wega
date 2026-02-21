using Content.Shared.DoAfter;
using Content.Shared.Pinpointer;
using Content.Shared.Verbs;
using Content.Shared.Visuals;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Utility;

namespace Content.Server.Pinpointer;

public sealed partial class HandheldBeaconSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HandheldBeaconComponent, GetVerbsEvent<AlternativeVerb>>(AddVerb);
        SubscribeLocalEvent<HandheldBeaconComponent, ToggleHandheldBeaconDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<HandheldBeaconComponent, BoundUIOpenedEvent>(OnUiOpened);
    }

    private void AddVerb(Entity<HandheldBeaconComponent> entity, ref GetVerbsEvent<AlternativeVerb> args)
    {
        var user = args.User;
        if (!args.CanAccess || !args.CanInteract)
            return;

        var text = entity.Comp.ActiveBeacon
            ? Loc.GetString("handhel-beacon-verb-toggle-off")
            : Loc.GetString("handhel-beacon-verb-toggle-on");

        AlternativeVerb verb = new()
        {
            Text = text,
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/Spare/poweronoff.svg.192dpi.png")),
            Act = () => ToggleHandheldBeacon(user, entity)
        };
        args.Verbs.Add(verb);
    }

    private void ToggleHandheldBeacon(EntityUid user, Entity<HandheldBeaconComponent> entity)
    {
        var ev = new ToggleHandheldBeaconDoAfterEvent();
        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, user, TimeSpan.FromSeconds(2f), ev, entity)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            MovementThreshold = 0.01f,
            NeedHand = true
        });
    }

    private void OnDoAfter(Entity<HandheldBeaconComponent> entity, ref ToggleHandheldBeaconDoAfterEvent ev)
    {
        if (ev.Cancelled)
            return;

        var toggle = !entity.Comp.ActiveBeacon;
        if (toggle)
        {
            _transform.SetCoordinates(entity, Transform(ev.Args.User).Coordinates);
            _transform.AnchorEntity(entity);
            _navMap.SetBeaconEnabled(entity, toggle);

            entity.Comp.ActiveBeacon = toggle;
            _appearance.SetData(entity, VisualLayers.Enabled, toggle);
        }
        else
        {
            _transform.Unanchor(entity);
            _navMap.SetBeaconEnabled(entity, toggle);
            _ui.CloseUi(entity.Owner, NavMapBeaconUiKey.Key);

            entity.Comp.ActiveBeacon = toggle;
            _appearance.SetData(entity, VisualLayers.Enabled, toggle);
        }

        _audio.PlayPvs(entity.Comp.SoundActivation, entity);
    }

    private void OnUiOpened(Entity<HandheldBeaconComponent> entity, ref BoundUIOpenedEvent args)
    {
        if (_ui.IsUiOpen(entity.Owner, NavMapBeaconUiKey.Key) && !entity.Comp.ActiveBeacon)
            _ui.CloseUi(entity.Owner, NavMapBeaconUiKey.Key);
    }
}
