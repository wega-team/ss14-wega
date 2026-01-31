using Content.Server.Chat.Systems;
using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Shared.Chat;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Lavaland.Artefacts.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Visuals;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server.Lavaland.Artefacts.Systems;

public sealed class RodOfAsclepiusSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RodOfAsclepiusComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<RodOfAsclepiusComponent, RodOathDoAfterEvent>(OnOathComplete);
        SubscribeLocalEvent<RodOfAsclepiusComponent, ComponentShutdown>(OnShutdown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<RodOfAsclepiusComponent>();

        while (query.MoveNext(out var uid, out var rod))
        {
            if (rod.BoundTo == null || rod.NextHealTime > curTime)
                continue;

            PerformHealing(uid, rod);
            rod.NextHealTime = curTime + rod.HealInterval;
        }
    }

    private void OnUseInHand(Entity<RodOfAsclepiusComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.BoundTo != null)
        {
            _popup.PopupEntity(Loc.GetString("lavaland-artefacts-rod-already-bound"),
                args.User, args.User);
            return;
        }

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, TimeSpan.FromSeconds(10),
            new RodOathDoAfterEvent(), ent.Owner, args.User)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = false
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        _popup.PopupEntity(Loc.GetString("lavaland-artefacts-rod-oath-start"),
            args.User, args.User);

        args.Handled = true;
    }

    private void OnOathComplete(Entity<RodOfAsclepiusComponent> ent, ref RodOathDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target == null)
            return;

        _chat.TrySendInGameICMessage(args.Target.Value, Loc.GetString("lavaland-artefacts-rod-latin-oath"),
            InGameICChatType.Speak, false);

        ent.Comp.BoundTo = args.Target.Value;

        EnsureComp<UnremoveableComponent>(ent);
        EnsureComp<PacifiedComponent>(args.Target.Value);

        _popup.PopupEntity(Loc.GetString("lavaland-artefacts-rod-bound",
            ("target", Identity.Entity(args.Target.Value, EntityManager))),
            args.Target.Value, args.Target.Value, PopupType.Medium);

        _item.SetHeldPrefix(ent.Owner, "active");
        _appearance.SetData(ent, VisualLayers.Enabled, true);

        args.Handled = true;
    }

    private void OnShutdown(Entity<RodOfAsclepiusComponent> ent, ref ComponentShutdown args)
    {
        Spawn("Ash", Transform(ent).Coordinates);
        _popup.PopupEntity(Loc.GetString("lavaland-artefacts-rod-disintegrated", ("name", Name(ent))),
            ent, ent, PopupType.MediumCaution);
    }

    private void PerformHealing(EntityUid rodUid, RodOfAsclepiusComponent rod)
    {
        if (rod.BoundTo == null || !TryComp(rod.BoundTo.Value, out MobStateComponent? mobState))
            return;

        if (!_mobState.IsDead(rod.BoundTo.Value, mobState))
        {
            var heal = new DamageSpecifier();
            heal.DamageDict.Add("Asphyxiation", -rod.HealAmount);
            heal.DamageDict.Add("Bloodloss", -rod.HealAmount);
            heal.DamageDict.Add("Blunt", -rod.HealAmount);
            heal.DamageDict.Add("Slash", -rod.HealAmount);
            heal.DamageDict.Add("Piercing", -rod.HealAmount);
            heal.DamageDict.Add("Heat", -rod.HealAmount);
            heal.DamageDict.Add("Cold", -rod.HealAmount);
            heal.DamageDict.Add("Poison", -rod.HealAmount);

            _damageable.TryChangeDamage(rod.BoundTo.Value, heal);
        }

        if (rod.HealOthers)
        {
            var coordinates = Transform(rod.BoundTo.Value).Coordinates;
            var entities = _lookup.GetEntitiesInRange<MobStateComponent>(coordinates, rod.HealRadius);

            foreach (var (entity, _) in entities)
            {
                if (entity == rod.BoundTo.Value)
                    continue;

                if (!_mobState.IsDead(entity))
                {
                    var healOther = new DamageSpecifier();
                    healOther.DamageDict.Add("Blunt", -rod.HealAmount / 3);
                    healOther.DamageDict.Add("Slash", -rod.HealAmount / 3);
                    healOther.DamageDict.Add("Piercing", -rod.HealAmount / 3);
                    healOther.DamageDict.Add("Heat", -rod.HealAmount / 3);
                    healOther.DamageDict.Add("Cold", -rod.HealAmount / 3);

                    _damageable.TryChangeDamage(entity, healOther);
                }
            }
        }
    }
}
