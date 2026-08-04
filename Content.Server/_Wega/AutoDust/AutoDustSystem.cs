using Content.Shared._Wega.AutoDust;
using Content.Shared.Actions;
using Content.Shared.Mobs;
using Content.Shared.Ninja.Components;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;

namespace Content.Server._Wega.AutoDust;

public sealed partial class AutoDustSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedPopupSystem   _popup   = default!;

    private static readonly EntProtoId ActionProto = "ActionNinjaAutoDust";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AutoDustComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<AutoDustComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<AutoDustComponent, MobStateChangedEvent>(OnMobStateChanged);
        // No component filter — action events are raised globally, not on the provider entity.
        SubscribeLocalEvent<ToggleAutoDustEvent>(OnToggle);
    }

    private void OnStartup(Entity<AutoDustComponent> ent, ref ComponentStartup args)
    {
        _actions.AddAction(ent.Owner, ref ent.Comp.ActionEntity, ActionProto);
        Dirty(ent.Owner, ent.Comp);
    }

    private void OnShutdown(Entity<AutoDustComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.ActionEntity);
    }

    private void OnMobStateChanged(Entity<AutoDustComponent> ent, ref MobStateChangedEvent args)
    {
        if (TerminatingOrDeleted(ent.Owner))
            return;

        var shouldDust = ent.Comp.Trigger switch
        {
            AutoDustTrigger.OnDeath => args.NewMobState == MobState.Dead,
            // Also fires on Dead so instant kills (suicide) are caught even when Crit is skipped.
            AutoDustTrigger.OnCrit  => args.NewMobState == MobState.Critical || args.NewMobState == MobState.Dead,
            _                       => false,
        };

        if (shouldDust)
            DustEntity(ent.Owner);
    }

    private void OnToggle(ToggleAutoDustEvent args)
    {
        if (args.Handled)
            return;

        var performer = args.Performer;
        if (!TryComp<AutoDustComponent>(performer, out var comp))
            return;

        args.Handled = true;

        comp.Trigger = comp.Trigger switch
        {
            AutoDustTrigger.Disabled => AutoDustTrigger.OnDeath,
            AutoDustTrigger.OnDeath  => AutoDustTrigger.OnCrit,
            _                        => AutoDustTrigger.Disabled,
        };
        Dirty(performer, comp);

        var msg = comp.Trigger switch
        {
            AutoDustTrigger.OnDeath => Loc.GetString("auto-dust-mode-on-death"),
            AutoDustTrigger.OnCrit  => Loc.GetString("auto-dust-mode-on-crit"),
            _                       => Loc.GetString("auto-dust-mode-disabled"),
        };
        _popup.PopupEntity(msg, performer, performer, PopupType.Large);
    }

    /// <summary>
    /// Turns the entity to ash immediately. Safe to call externally (e.g. SpiderOS protection).
    /// </summary>
    public void DustEntity(EntityUid uid)
    {
        Spawn("Ash", Transform(uid).Coordinates);
        QueueDel(uid);
    }
}
