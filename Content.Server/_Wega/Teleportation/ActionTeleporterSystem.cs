using Content.Server.Administration.Logs;
using Content.Server.Popups;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Body;
using Content.Shared.Changeling.Components;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Gibbing;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Content.Shared.Teleportation.Components;
using Content.Shared.Teleportation.Systems;
using Content.Shared.Vampire.Components;
using Content.Shared.Zombies;
using Robust.Server.Audio;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;

namespace Content.Server.Teleportation;

public sealed partial class ActionTeleporterSystem : EntitySystem
{
    [Dependency] private IAdminLogManager _adminLogger = default!;
    [Dependency] private LinkedEntitySystem _link = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedDoAfterSystem _doafter = default!;
    [Dependency] private BodySystem _body = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<ActionTeleporterComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.FirstPortalDeadline != null && curTime >= comp.FirstPortalDeadline)
            {
                FizzlePortals((uid, comp), null, instability: false);
                continue;
            }

            if (comp.PortalsCloseDeadline != null && curTime >= comp.PortalsCloseDeadline)
                FizzlePortals((uid, comp), null, instability: false);
        }
    }

    [SubscribeLocalEvent]
    private void OnMapInit(EntityUid uid, ActionTeleporterComponent component, MapInitEvent args)
    {
        _actions.AddAction(uid, ref component.CreatePortalActionEntity, component.CreatePortalAction);
    }

    #region Portal Creation

    [SubscribeLocalEvent]
    private void OnCreatePortalAction(EntityUid uid, ActionTeleporterComponent component, ActionTeleportShadekin args)
    {
        if (args.Handled)
            return;

        var doafterArgs = new DoAfterArgs(
            EntityManager,
            args.Performer,
            component.PortalCreationDelay,
            new ActionTeleporterDoAfterEvent(),
            uid,
            used: uid)
        {
            BreakOnDamage = component.DoAfterBreakOnDamage,
            BreakOnMove = component.DoAfterBreakOnMove,
            MovementThreshold = component.DoAfterMovementThreshold,
        };

        _doafter.TryStartDoAfter(doafterArgs);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnDoAfter(EntityUid uid, ActionTeleporterComponent component, ActionTeleporterDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        HandlePortalUpdating(uid, component, args.Args.User);
        args.Handled = true;
    }

    private void HandlePortalUpdating(EntityUid uid, ActionTeleporterComponent component, EntityUid user)
    {
        if (TerminatingOrDeleted(user))
            return;

        var xform = Transform(user);

        if (TerminatingOrDeleted(component.FirstPortal) && TerminatingOrDeleted(component.SecondPortal))
        {
            SpawnFirstPortal(uid, component, user, xform);
        }
        else if (TerminatingOrDeleted(component.SecondPortal))
        {
            if (!TrySpawnSecondPortal(uid, component, user, xform))
                return;
        }
        else
        {
            FizzlePortals((uid, component), user, instability: false);
        }
    }

    private void SpawnFirstPortal(EntityUid uid, ActionTeleporterComponent component, EntityUid user, TransformComponent xform)
    {
        if (xform.ParentUid != xform.GridUid)
            return;

        var timeout = EnsureComp<PortalTimeoutComponent>(user);
        timeout.EnteredPortal = null;

        component.FirstPortal = Spawn(component.FirstPortalPrototype, xform.Coordinates);
        component.FirstPortalDeadline = _timing.CurTime + component.FirstPortalTimeout;
        Dirty(uid, component);

        _appearance.SetData(component.FirstPortal.Value, PortalVisuals.State, PortalVisualState.Closed);
        ConfigurePortal(component, component.FirstPortal.Value);

        _adminLogger.Add(LogType.EntitySpawn, LogImpact.High,
            $"{ToPrettyString(user):player} opened {ToPrettyString(component.FirstPortal.Value)} at {Transform(component.FirstPortal.Value).Coordinates} using {ToPrettyString(uid)}");

        _audio.PlayPvs(component.NewPortalSound, uid);
        _actions.RemoveAction(uid, component.CreatePortalActionEntity);
        _actions.AddAction(uid, ref component.CreateSecondPortalActionEntity, component.CreateSecondPortalAction);
    }

    private bool TrySpawnSecondPortal(EntityUid uid, ActionTeleporterComponent component, EntityUid user, TransformComponent xform)
    {
        if (xform.ParentUid != xform.GridUid)
            return false;

        if (component.FirstPortal is not { } firstPortal)
            return false;

        if (!component.AllowPortalsOnDifferentGrids
            && xform.ParentUid != Transform(firstPortal).ParentUid)
        {
            FizzlePortals((uid, component), user, instability: true);
            return false;
        }

        var timeout = EnsureComp<PortalTimeoutComponent>(user);
        timeout.EnteredPortal = null;

        var secondPortal = Spawn(component.SecondPortalPrototype, xform.Coordinates);
        component.SecondPortal = secondPortal;
        component.FirstPortalDeadline = null;
        component.PortalsCloseDeadline = _timing.CurTime + component.PortalsCloseTimeout;
        Dirty(uid, component);

        _appearance.SetData(firstPortal, PortalVisuals.State, PortalVisualState.Opening);
        _appearance.SetData(secondPortal, PortalVisuals.State, PortalVisualState.Opening);

        ConfigurePortal(component, firstPortal);
        _link.TryLink(firstPortal, secondPortal, true);

        _adminLogger.Add(LogType.EntitySpawn, LogImpact.High,
            $"{ToPrettyString(user):player} opened {ToPrettyString(secondPortal)} at {Transform(secondPortal).Coordinates} linked to {ToPrettyString(firstPortal)} using {ToPrettyString(uid)}");

        _audio.PlayPvs(component.NewPortalSound, uid);
        _actions.RemoveAction(uid, component.CreateSecondPortalActionEntity);
        _actions.AddAction(uid, ref component.ClosePortalsActionEntity, component.ClosePortalsAction);

        return true;
    }

    private void ConfigurePortal(ActionTeleporterComponent component, EntityUid portalUid)
    {
        if (!TryComp<PortalComponent>(portalUid, out var portal))
            return;

        portal.RandomTeleport = false;
        if (component.AllowPortalsOnDifferentMaps)
            portal.CanTeleportToOtherMaps = true;
    }

    #endregion

    #region Portal Cleanup

    public void FizzlePortals(Entity<ActionTeleporterComponent> entity, EntityUid? user, bool instability)
    {
        var comp = entity.Comp;

        LogPortalClosure(entity, user);

        ClosePortalVisuals(comp.FirstPortal);
        ClosePortalVisuals(comp.SecondPortal);

        var hadSecondPortal = comp.SecondPortal != null;

        comp.FirstPortal = null;
        comp.SecondPortal = null;
        comp.FirstPortalDeadline = null;
        comp.PortalsCloseDeadline = null;
        Dirty(entity);

        _audio.PlayPvs(comp.ClearPortalsSound, entity);

        RemoveActionIfAttached(entity.Owner, comp.CreateSecondPortalActionEntity);
        RemoveActionIfAttached(entity.Owner, comp.ClosePortalsActionEntity);

        _actions.SetCooldown(comp.CreatePortalActionEntity,
            hadSecondPortal ? comp.TwoPortalCooldown : comp.OnePortalCooldown);

        _actions.AddAction(entity.Owner, ref comp.CreatePortalActionEntity, comp.CreatePortalAction);

        if (instability && user != null)
            _popup.PopupEntity(Loc.GetString("action-teleporter-instability-fizzle"), entity, user.Value, PopupType.MediumCaution);
    }

    private void ClosePortalVisuals(EntityUid? portal)
    {
        if (Deleted(portal))
            return;

        _appearance.SetData(portal.Value, PortalVisuals.State, PortalVisualState.Closing);

        var lifetime = TryComp<PortalComponent>(portal.Value, out var portalComp)
            ? portalComp.ClosingAnimationTime
            : TimeSpan.FromSeconds(0.5);

        var despawn = EnsureComp<TimedDespawnComponent>(portal.Value);
        despawn.Lifetime = (float)lifetime.TotalSeconds;
    }

    private void LogPortalClosure(Entity<ActionTeleporterComponent> entity, EntityUid? user)
    {
        var portalStrings = BuildPortalStrings(entity.Comp);

        if (portalStrings == string.Empty)
            return;

        var message = user != null
            ? $"{ToPrettyString(user.Value):player} closed {portalStrings} with {ToPrettyString(entity)}"
            : $"{portalStrings} were closed";

        _adminLogger.Add(LogType.EntityDelete, LogImpact.High, $"{message}");
    }

    private string BuildPortalStrings(ActionTeleporterComponent comp)
    {
        var parts = new List<string>(2);

        if (!Deleted(comp.FirstPortal))
            parts.Add(ToPrettyString(comp.FirstPortal.Value));

        if (!Deleted(comp.SecondPortal))
            parts.Add(ToPrettyString(comp.SecondPortal.Value));

        return string.Join(" and ", parts);
    }

    private void RemoveActionIfAttached(EntityUid owner, EntityUid? actionUid)
    {
        if (actionUid == null)
            return;

        if (TryComp(actionUid.Value, out ActionComponent? actionComp)
            && actionComp.AttachedEntity == owner)
        {
            _actions.RemoveAction(owner, actionUid.Value);
        }
    }

    [SubscribeLocalEvent]
    private void OnClosePortalAction(EntityUid uid, ActionTeleporterComponent component, ActionCloseTeleportShadekin args)
    {
        if (args.Handled)
            return;

        FizzlePortals((uid, component), args.Performer, instability: false);
        args.Handled = true;
    }

    #endregion

    #region Interference / Restrictions

    [SubscribeLocalEvent]
    private void OnShadekinPortalTeleport(EntityUid uid, ActionTeleporterComponent component, EntityTeleportedFromPortalEvent args) // No chain-portals for ya
    {
        if (!component.FizzleOnForeignPortal)
            return;

        if (component.FirstPortal == null && component.SecondPortal == null)
            return;

        if (args.EnteredPortal == component.FirstPortal || args.EnteredPortal == component.SecondPortal)
            return;

        _popup.PopupEntity(Loc.GetString("action-teleporter-popup-fizzle-foreign-portal"), uid);
        FizzlePortals((uid, component), uid, instability: false);
    }

    [SubscribeLocalEvent]
    private void OnMobStateChanged(EntityUid uid, ActionTeleporterComponent component, MobStateChangedEvent args) // Can't support portals if you dead
    {
        if (args.NewMobState != MobState.Dead)
            return;

        FizzlePortals((uid, component), null, instability: false);
    }

    [SubscribeLocalEvent]
    private void OnGibbed(EntityUid uid, ActionTeleporterComponent component, ref BeingGibbedEvent args) // Can't support portals if you de.... oh god
    {
        FizzlePortals((uid, component), null, instability: false);
    }

    [SubscribeLocalEvent]
    private void OnZombified(EntityUid uid, ActionTeleporterComponent component, ref EntityZombifiedEvent args) // Gruaa can't use it - core dead
    {
        FizzlePortals((uid, component), null, instability: false);
        RemovePortalActions(uid, component);
    }

    // TODO: Replace ComponentInit to something else
    [SubscribeLocalEvent]
    private void OnBecameVampire(Entity<VampireComponent> ent, ref ComponentInit args) // RedSpace and Core(BlueSpace) not compatible
    {
        if (!TryComp<ActionTeleporterComponent>(ent.Owner, out var component))
            return;

        RemoveTeleport(ent.Owner, component);
    }

    // TODO: Replace ComponentStartup to something else
    [SubscribeLocalEvent]
    private void OnBecameChangeling(Entity<ChangelingIdentityComponent> ent, ref ComponentStartup args) // Core to difficult to mimic
    {
        if (!TryComp<ActionTeleporterComponent>(ent, out var component))
            return;

        RemoveTeleport(ent, component);
    }

    private void RemoveTeleport(EntityUid uid, ActionTeleporterComponent component)
    {
        FizzlePortals((uid, component), null, instability: false);
        RemovePortalActions(uid, component);
    }

    private void RemovePortalActions(EntityUid uid, ActionTeleporterComponent component)
    {
        _actions.RemoveAction(uid, component.CreatePortalActionEntity);
        _actions.RemoveAction(uid, component.CreateSecondPortalActionEntity);
    }

    #endregion

    #region Shadekin Core

    private bool HasWorkingCore(EntityUid uid)
    {
        foreach (var organ in _body.EnumerateOrgans<ShadekinCoreComponent>(uid))
        {
            if (organ.Comp2.Mode == ShadekinCoreMode.Active && organ.Comp2.OwnerBody == uid)
                return true;
        }

        return false;
    }

    [SubscribeLocalEvent]
    private void OnCoreRemoved(EntityUid uid, ActionTeleporterComponent component, ref OrganRemovedFromEvent args)
    {
        if (TerminatingOrDeleted(uid))
        {
            FizzlePortals((uid, component), null, instability: false);
            return;
        }

        if (HasWorkingCore(uid))
            return;

        FizzlePortals((uid, component), null, instability: false);
        RemovePortalActions(uid, component);

        component.CreatePortalActionEntity = null;
        component.CreateSecondPortalActionEntity = null;
    }

    [SubscribeLocalEvent]
    private void OnCoreInserted(EntityUid uid, ActionTeleporterComponent component, ref OrganInsertedIntoEvent args)
    {
        if (HasWorkingCore(uid) && component.CreatePortalActionEntity == null)
            _actions.AddAction(uid, ref component.CreatePortalActionEntity, component.CreatePortalAction);
    }

    #endregion
}
