using Content.Shared.Achievements;
using Content.Shared.Chemistry;
using Content.Shared.Damage.Systems;
using Content.Shared.Inventory;
using Content.Shared.Modular.Suit;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.Modular.Suit;

public sealed class AffectedModuleSpringlockSystem : EntitySystem
{
    [Dependency] private readonly SharedAchievementsSystem _achievement = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedModularSuitSystem _modular = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AffectedModuleSpringlockComponent, ReactionEntityEvent>(OnReaction);
    }

    private void OnReaction(Entity<AffectedModuleSpringlockComponent> ent, ref ReactionEntityEvent args)
    {
        if (ent.Comp.Locked || ent.Comp.Triggered)
            return;

        if (args.Method != ent.Comp.LockMethod || args.Reagent.ID != ent.Comp.TargetReagent)
            return;

        var user = ent.Owner;
        if (!TryComp<ModularSuitCarrierComponent>(user, out var carrier) || string.IsNullOrEmpty(carrier.CurrentSlot)
            || !_inventory.TryGetSlotEntity(user, carrier.CurrentSlot, out var suit))
            return;

        if (!TryComp<ModularSuitSpringlockInstalledComponent>(suit, out var installed) || installed.Module == null)
            return;

        var alert = new SoundPathSpecifier("/Audio/_Wega/Effects/Modsuit/springlock.ogg");
        _audio.PlayPredicted(alert, user, null);

        ent.Comp.Triggered = true;
        Dirty(ent.Owner, ent.Comp);

        Timer.Spawn(TimeSpan.FromSeconds(5), () =>
        {
            if (!Exists(installed.Module.Value))
                return;

            _modular.SetModulePermanent(installed.Module.Value, true);
            if (HasComp<AffectedModuleSpringlockComponent>(user))
            {
                ent.Comp.Locked = true;
                Dirty(ent.Owner, ent.Comp);

                _damage.TryChangeDamage(ent.Owner, ent.Comp.LockDamage, true);
                _popup.PopupEntity(Loc.GetString("modsuit-springlock-locked"), ent, ent, PopupType.LargeCaution);

                _audio.PlayPredicted(new SoundPathSpecifier("/Audio/Items/snap.ogg"), user, null);
                _audio.PlayPredicted(new SoundPathSpecifier("/Audio/Effects/Fluids/splat.ogg"), user, null);

                _achievement.QueueAchievement(user, AchievementsEnum.Springlock);

                Timer.Spawn(TimeSpan.FromSeconds(4), () =>
                {
                    if (!Exists(user) || !TryComp<ActorComponent>(user, out var actor))
                        return;

                    _audio.PlayGlobal(new SoundPathSpecifier("/Audio/_Wega/Ambience/toreadormarch.ogg"), actor.PlayerSession);
                });
            }
        });
    }
}
