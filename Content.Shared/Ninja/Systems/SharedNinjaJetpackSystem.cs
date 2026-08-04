using Content.Shared.Actions;
using Content.Shared.Gravity;
using Content.Shared.Interaction.Events;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Ninja.Components;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Ninja.Systems;

public abstract partial class SharedNinjaJetpackSystem : EntitySystem
{
    [Dependency] private MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] protected SharedContainerSystem Container = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private ActionContainerSystem _actionContainer = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NinjaJetpackComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<NinjaJetpackComponent, GetItemActionsEvent>(OnGetItemActions);
        SubscribeLocalEvent<NinjaJetpackComponent, ToggleNinjaJetpackEvent>(OnToggle);
        SubscribeLocalEvent<NinjaJetpackComponent, DroppedEvent>(OnDropped);
        SubscribeLocalEvent<NinjaJetpackComponent, EntGotInsertedIntoContainerMessage>(OnMoved);
        SubscribeLocalEvent<SpaceNinjaComponent, EntParentChangedMessage>(OnNinjaParentChanged);
        SubscribeLocalEvent<GravityChangedEvent>(OnGravityChanged);
    }

    private void OnMapInit(EntityUid uid, NinjaJetpackComponent comp, MapInitEvent args)
    {
        _actionContainer.EnsureAction(uid, ref comp.ToggleActionEntity, comp.ToggleAction);
        Dirty(uid, comp);
    }

    private void OnGetItemActions(EntityUid uid, NinjaJetpackComponent comp, GetItemActionsEvent args)
    {
        args.AddAction(ref comp.ToggleActionEntity, comp.ToggleAction);
    }

    private void OnToggle(EntityUid uid, NinjaJetpackComponent comp, ToggleNinjaJetpackEvent args)
    {
        if (args.Handled)
            return;

        if (TryComp(uid, out TransformComponent? xform) && !CanEnableOnGrid(xform.GridUid))
            return;

        SetEnabled(uid, comp, !IsEnabled(uid));
        args.Handled = true;
    }

    private void OnDropped(EntityUid uid, NinjaJetpackComponent comp, DroppedEvent args)
        => SetEnabled(uid, comp, false, args.User);

    private void OnMoved(Entity<NinjaJetpackComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        if (args.Container.Owner != ent.Comp.JetpackUser)
            SetEnabled(ent, ent.Comp, false, ent.Comp.JetpackUser);
    }

    private void OnNinjaParentChanged(Entity<SpaceNinjaComponent> ent, ref EntParentChangedMessage args)
    {
        if (ent.Comp.Suit == null)
            return;
        if (!TryComp<NinjaJetpackComponent>(ent.Comp.Suit.Value, out var jetpack))
            return;
        if (!CanEnableOnGrid(args.Transform.GridUid))
            SetEnabled(ent.Comp.Suit.Value, jetpack, false, ent.Owner);
    }

    private void OnGravityChanged(ref GravityChangedEvent ev)
    {
        if (!ev.HasGravity)
            return;

        var query = EntityQueryEnumerator<ActiveNinjaJetpackComponent, NinjaJetpackComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var comp, out var xform))
        {
            if (xform.GridUid == ev.ChangedGridIndex)
                SetEnabled(uid, comp, false, comp.JetpackUser);
        }
    }

    public bool IsEnabled(EntityUid uid) => HasComp<ActiveNinjaJetpackComponent>(uid);

    private bool CanEnableOnGrid(EntityUid? gridUid)
        => gridUid == null || !HasComp<GravityComponent>(gridUid);

    protected virtual bool CanEnable(EntityUid uid, NinjaJetpackComponent comp) => true;

    public void SetEnabled(EntityUid uid, NinjaJetpackComponent comp, bool enabled, EntityUid? user = null)
    {
        if (IsEnabled(uid) == enabled || enabled && !CanEnable(uid, comp))
            return;

        if (user == null)
        {
            if (!Container.TryGetContainingContainer((uid, null, null), out var container))
                return;
            user = container.Owner;
        }

        if (enabled)
        {
            SetupUser(user.Value, uid, comp);
            EnsureComp<ActiveNinjaJetpackComponent>(uid);
        }
        else
        {
            RemoveUser(user.Value, comp);
            RemComp<ActiveNinjaJetpackComponent>(uid);
        }

        Dirty(uid, comp);
    }

    private void SetupUser(EntityUid user, EntityUid suitUid, NinjaJetpackComponent comp)
    {
        EnsureComp<JetpackUserComponent>(user, out var userComp);
        comp.JetpackUser = user;

        if (TryComp<PhysicsComponent>(user, out var physics))
            _physics.SetBodyStatus(user, physics, BodyStatus.InAir);

        userComp.Jetpack = suitUid;
        userComp.WeightlessAcceleration = comp.Acceleration;
        userComp.WeightlessModifier = comp.WeightlessModifier;
        userComp.WeightlessFriction = comp.Friction;
        userComp.WeightlessFrictionNoInput = comp.Friction;
        _movementSpeedModifier.RefreshWeightlessModifiers(user);
    }

    private void RemoveUser(EntityUid user, NinjaJetpackComponent comp)
    {
        if (!RemComp<JetpackUserComponent>(user))
            return;

        comp.JetpackUser = null;

        if (TryComp<PhysicsComponent>(user, out var physics))
            _physics.SetBodyStatus(user, physics, BodyStatus.OnGround);

        _movementSpeedModifier.RefreshWeightlessModifiers(user);
    }
}
