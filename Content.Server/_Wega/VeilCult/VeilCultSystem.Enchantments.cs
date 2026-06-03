using System.Linq;
using System.Numerics;
using Content.Server.Surgery;
using Content.Shared.Body.Components;
using Content.Shared.Veil.Cult.Components;
using Content.Shared.Veil.Cult;
using Content.Shared.Clothing;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Doors.Components;
using Content.Shared.EnergyShield;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Administration;
using Content.Shared.Weapons.Reflect;
using Content.Shared.Weapons.Melee;
using Content.Shared.Stealth.Components;
using Content.Shared.Flash.Components;
using Content.Shared.Armor;
using Content.Server.Atmos.Components;
using Content.Shared.Weapons.Melee.Components;
using Content.Shared.Wieldable.Components;
using Content.Shared.Speech.Muting;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Android;
using Content.Shared.Movement.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Tag;
using Content.Shared.Emag.Systems;
using Content.Shared.Maps;
using Content.Shared.Doors.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Map.Components;

namespace Content.Server.Veil.Cult;

public sealed partial class VeilCultSystem
{
    [Dependency] private MovementSpeedModifierSystem _speed = default!;
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private SharedDoorSystem _door = default!;
    [Dependency] private SurgerySystem _surgery = default!;
    [Dependency] private TileSystem _tile = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private ITileDefinitionManager _tileDefinitionManager = default!;

    private static readonly ProtoId<TagPrototype> WallTag = "Wall";

    private void InitializeEnchantments()
    {
        // Activate Action at enchanted item
        SubscribeLocalEvent<EnchantedComponent, CrusherEnchantActionEvent>(OnActivateCrusher);
        SubscribeLocalEvent<EnchantedComponent, ConfusionEnchantActionEvent>(OnActivateConfusion);
        SubscribeLocalEvent<EnchantedComponent, KnockbackEnchantActionEvent>(OnActivateKnockback);
        SubscribeLocalEvent<EnchantedComponent, DismantlingEnchantActionEvent>(OnActivateDismantling);
        SubscribeLocalEvent<EnchantedComponent, SwordsmenEnchantActionEvent>(OnActivateSwordsmen);
        SubscribeLocalEvent<EnchantedComponent, BloodshedEnchantActionEvent>(OnActivateBloodShed);
        SubscribeLocalEvent<EnchantedComponent, HasteEnchantActionEvent>(OnActivateHaste);
        SubscribeLocalEvent<EnchantedComponent, ReflectionEnchantActionEvent>(OnActivateReflection);
        SubscribeLocalEvent<EnchantedComponent, CamouflageEnchantActionEvent>(OnActivateCamouflage);
        SubscribeLocalEvent<EnchantedComponent, AbsorbEnchantActionEvent>(OnActivateAbsorb);
        SubscribeLocalEvent<EnchantedComponent, SmokeEnchantActionEvent>(OnActivateSmoke);
        SubscribeLocalEvent<EnchantedComponent, HardenPlatesEnchantActionEvent>(OnActivateHardenPlates);
        SubscribeLocalEvent<EnchantedComponent, NorthStarEnchantActionEvent>(OnActivateNorthStar);
        SubscribeLocalEvent<EnchantedComponent, RedFlameEnchantActionEvent>(OnActivateRedFlame);

        // Enchants
        SubscribeLocalEvent<CrusherEnchantComponent, MeleeHitEvent>(CrusherOnMeleeHit);
        SubscribeLocalEvent<DismantlingEnchantComponent, MeleeHitEvent>(DismantlingOnMeleeHit);
        SubscribeLocalEvent<ConfusionEnchantComponent, MeleeHitEvent>(ConfusionOnMeleeHit);
        SubscribeLocalEvent<KnockbackEnchantComponent, MeleeHitEvent>(KnockbackOnMeleeHit);
        SubscribeLocalEvent<StunEnchantComponent, MeleeHitEvent>(StunOnMeleeHit);
        SubscribeLocalEvent<ForcePassageEnchantComponent, MeleeHitEvent>(ForcePassageOnMeleeHit);
        SubscribeLocalEvent<TerraformEnchantComponent, MeleeHitEvent>(TerraformOnMeleeHit);
        SubscribeLocalEvent<ElectricalTouchEnchantComponent, MeleeHitEvent>(ElectricalTouchOnMeleeHit);
        SubscribeLocalEvent<BloodshedEnchantComponent, MeleeHitEvent>(BloodshedOnMeleeHit);

        Subs.SubscribeWithRelay<ReflectionEnchantComponent, ProjectileReflectAttemptEvent>(OnReflectionProjecile, baseEvent: false);
        Subs.SubscribeWithRelay<ReflectionEnchantComponent, HitScanReflectAttemptEvent>(OnReflectionHitscan, baseEvent: false);

        SubscribeLocalEvent<ReconstructionEnchantComponent, UseInHandEvent>(ReconstructionOnUseInHand);
        SubscribeLocalEvent<EmpEnchantComponent, UseInHandEvent>(EmpOnUseInHand);
        SubscribeLocalEvent<TimeStopEnchantComponent, UseInHandEvent>(TimeStopOnUseInHand);
        SubscribeLocalEvent<HidingsClockEnchantComponent, UseInHandEvent>(HidingCloacksOnUseInHand);

        SubscribeLocalEvent<SealWoundsEnchantComponent, MeleeHitEvent>(SealWoundOnUse);

        SubscribeLocalEvent<EnchantableComponent, EnchantingDoAfterEvent>(EnchantDoAfter);
    }

    private void EnchantDoAfter(EntityUid uid, EnchantableComponent component, ref EnchantingDoAfterEvent args)
    {
        if (args.Cancelled || args.Target == null)
            return;

        if (_veilCult.TryUseEnergy(component.Cost))
        {
            var ent = Spawn(args.Entity, Transform(args.Target.Value).Coordinates);
            _hands.TryForcePickupAnyHand(args.Target.Value, ent);
            _audio.PlayPvs(CultSpell, args.Target.Value);
            QueueDel(uid);
        }
    }

    private void OnActivateCrusher(EntityUid uid, EnchantedComponent comp, CrusherEnchantActionEvent args)
    {
        EnsureComp<CrusherEnchantComponent>(uid);
        args.Handled = true;
    }

    private void OnActivateDismantling(EntityUid uid, EnchantedComponent comp, DismantlingEnchantActionEvent args)
    {
        EnsureComp<DismantlingEnchantComponent>(uid);
        args.Handled = true;
    }

    private void OnActivateKnockback(EntityUid uid, EnchantedComponent comp, KnockbackEnchantActionEvent args)
    {
        EnsureComp<KnockbackEnchantComponent>(uid, out var kb);
        EnsureComp<MeleeThrowOnHitComponent>(uid, out var throwOnHit);
        throwOnHit.Speed = kb.Speed;
        throwOnHit.Distance = kb.Distance;
        if (TryComp<StaminaDamageOnHitComponent>(uid, out var stam))
            stam.Damage *= 2.5f;

        args.Handled = true;
    }

    private void OnActivateConfusion(EntityUid uid, EnchantedComponent comp, ConfusionEnchantActionEvent args)
    {
        EnsureComp<ConfusionEnchantComponent>(uid);
        args.Handled = true;
    }

    private void OnActivateSwordsmen(EntityUid uid, EnchantedComponent comp, SwordsmenEnchantActionEvent args)
    {
        EnsureComp<SwordsmenEnchantComponent>(uid, out var enchant);
        if (TryComp<MeleeWeaponComponent>(uid, out var weapon))
        {
            var oldAttackRate = weapon.AttackRate;
            weapon.AttackRate = enchant.AttackRate;
            var oldDamage = weapon.Damage;
            var newDamage = new DamageSpecifier { DamageDict = { { "Slash", 9 } } };
            weapon.Damage = newDamage;
            Timer.Spawn(TimeSpan.FromSeconds(9), () =>
            {
                RemComp<EnchantedComponent>(uid);
                RemComp<SwordsmenEnchantComponent>(uid);
                weapon.AttackRate = oldAttackRate;
                weapon.Damage = oldDamage;
            });
        }

        args.Handled = true;
    }

    private void OnActivateBloodShed(EntityUid uid, EnchantedComponent comp, BloodshedEnchantActionEvent args)
    {
        EnsureComp<BloodshedEnchantComponent>(uid);
        args.Handled = true;
    }

    private void OnActivateHaste(EntityUid uid, EnchantedComponent comp, HasteEnchantActionEvent args)
    {
        EnsureComp<HasteEnchantComponent>(uid, out var haste);
        EnsureComp<ClothingSpeedModifierComponent>(uid, out var cloth);
        var oldWalk = cloth.WalkModifier;
        var oldSprint = cloth.SprintModifier;
        cloth.SprintModifier = haste.SprintModifier;
        cloth.WalkModifier = haste.WalkModifier;
        Dirty(uid, cloth);

        _speed.RefreshMovementSpeedModifiers(args.Performer);
        Timer.Spawn(haste.Time, () =>
        {
            RemComp<EnchantedComponent>(uid);
            RemComp<HasteEnchantComponent>(uid);
            cloth.SprintModifier = oldSprint;
            cloth.WalkModifier = oldWalk;
            Dirty(uid, cloth);
            _speed.RefreshMovementSpeedModifiers(args.Performer);
        });

        args.Handled = true;
    }

    private void OnActivateReflection(EntityUid uid, EnchantedComponent comp, ReflectionEnchantActionEvent args)
    {
        EnsureComp<ReflectComponent>(uid, out var refl);
        refl.ReflectingInHands = false;
        refl.ReflectProb = 1f;
        refl.InRightPlace = true;

        args.Handled = true;
    }

    private void OnActivateAbsorb(EntityUid uid, EnchantedComponent comp, AbsorbEnchantActionEvent args)
    {
        var user = args.Performer;
        var shield = EnsureComp<EnergyShieldOwnerComponent>(user);
        shield.ShieldEntity = Spawn("EnergyShieldEffect", Transform(user).Coordinates);
        shield.SustainingCount = 6;
        _transform.SetParent(shield.ShieldEntity.Value, user);
        RemComp<EnchantedComponent>(uid);

        args.Handled = true;
    }

    private void OnActivateCamouflage(EntityUid uid, EnchantedComponent comp, CamouflageEnchantActionEvent args)
    {
        EnsureComp<StealthComponent>(args.Performer, out var stealth);
        stealth.LastVisibility = 0.15f;
        Dirty(args.Performer, stealth);
        Timer.Spawn(TimeSpan.FromSeconds(10), () =>
        {
            RemComp<StealthComponent>(args.Performer);
            RemComp<CamouflageEnchantComponent>(uid);
            RemComp<EnchantedComponent>(uid);
        });

        args.Handled = true;
    }

    private void OnActivateSmoke(EntityUid uid, EnchantedComponent comp, SmokeEnchantActionEvent args)
    {
        var effect = _random.Prob(0.75f) ? "AdminInstantEffectSmoke3" : "AdminInstantEffectSmoke10";
        Spawn(effect, Transform(uid).Coordinates);
        
        RemComp<SmokeEnchantComponent>(uid);
        RemComp<EnchantedComponent>(uid);

        args.Handled = true;
    }

    private void OnActivateHardenPlates(EntityUid uid, EnchantedComponent comp, HardenPlatesEnchantActionEvent args)
    {
        EnsureComp<HardenPlatesEnchantComponent>(uid, out var plate);
        if (TryComp<ArmorComponent>(uid, out var armor))
        {
            var oldHeat = armor.Modifiers.Coefficients["Heat"];
            var oldPiercing = armor.Modifiers.Coefficients["Piercing"];
            var oldSlash = armor.Modifiers.Coefficients["Slash"];
            var oldBlunt = armor.Modifiers.Coefficients["Blunt"];
            armor.Modifiers.Coefficients["Blunt"] = 0.5f;
            armor.Modifiers.Coefficients["Slash"] = 0.5f;
            armor.Modifiers.Coefficients["Piercing"] = 0.35f;
            armor.Modifiers.Coefficients["Heat"] = 0.4f;
            Timer.Spawn(plate.Time, () =>
            {
                armor.Modifiers.Coefficients["Blunt"] = oldBlunt;
                armor.Modifiers.Coefficients["Slash"] = oldSlash;
                armor.Modifiers.Coefficients["Piercing"] = oldPiercing;
                armor.Modifiers.Coefficients["Heat"] = oldHeat;
                RemComp<HardenPlatesEnchantComponent>(uid);
                RemComp<EnchantedComponent>(uid);
            });
        }

        args.Handled = true;
    }

    private void OnActivateNorthStar(EntityUid uid, EnchantedComponent comp, NorthStarEnchantActionEvent args)
    {
        EnsureComp<NorthStarEnchantComponent>(uid, out var enchant);
        if (TryComp<MeleeWeaponComponent>(uid, out var weapon))
        {
            var oldRate = weapon.AttackRate;
            weapon.AttackRate = enchant.AttackRate;
            Timer.Spawn(TimeSpan.FromSeconds(7), () =>
            {
                weapon.AttackRate = oldRate;
                RemComp<EnchantedComponent>(uid);
                RemComp<NorthStarEnchantComponent>(uid);
            });
        }

        args.Handled = true;
    }

    private void OnActivateRedFlame(EntityUid uid, EnchantedComponent comp, RedFlameEnchantActionEvent args)
    {
        EnsureComp<RedFlameEnchantComponent>(uid, out var enchant);
        EnsureComp<IgniteOnMeleeHitComponent>(uid, out var flame);
        flame.FireStacks = 2;
        Timer.Spawn(enchant.Time, () =>
        {
            RemComp<RedFlameEnchantComponent>(uid);
            RemComp<EnchantedComponent>(uid);
            RemComp<IgniteOnMeleeHitComponent>(uid);
        });

        args.Handled = true;
    }

    private void KnockbackOnMeleeHit(EntityUid uid, KnockbackEnchantComponent comp, MeleeHitEvent args)
    {
        if (args.IsHit && args.HitEntities.Count > 0)
            comp.Uses--;

        if (comp.Uses <= 0)
        {
            RemComp<KnockbackEnchantComponent>(uid);
            RemComp<MeleeThrowOnHitComponent>(uid);
            RemComp<EnchantedComponent>(uid);
            if (TryComp<StaminaDamageOnHitComponent>(uid, out var stam))
                stam.Damage /= 2.5f;
        }
    }

    private void CrusherOnMeleeHit(EntityUid uid, CrusherEnchantComponent comp, MeleeHitEvent args)
    {
        if (TryComp<WieldableComponent>(uid, out var wield))
        {
            if (wield.Wielded && args.HitEntities != null)
            {
                args.BonusDamage += new DamageSpecifier { DamageDict = { { "Blunt", 30 } } };
                foreach (var target in args.HitEntities)
                {
                    var selectedInjury = _random.Pick(new[] { "OpenFracture", "ClosedFracture" });
                    _surgery.TryAddInternalDamage(target, selectedInjury);
                }
                RemComp<CrusherEnchantComponent>(uid);
                RemComp<EnchantedComponent>(uid);
            }
        }
    }
    
    private void DismantlingOnMeleeHit(EntityUid uid, DismantlingEnchantComponent comp, MeleeHitEvent args)
    {
        if (TryComp<WieldableComponent>(uid, out var wield))
        {
            if (wield.Wielded && args.HitEntities != null)
            {
                args.BonusDamage += new DamageSpecifier { DamageDict = { { "Structural", 800 } } };
                RemComp<DismantlingEnchantComponent>(uid);
                RemComp<EnchantedComponent>(uid);
            }
        }
    }

    private void ConfusionOnMeleeHit(EntityUid uid, ConfusionEnchantComponent comp, MeleeHitEvent args)
    {
        foreach (var target in args.HitEntities)
        {
            if (HasComp<InputMoverComponent>(target))
            {
                EnsureComp<ConfusionComponent>(target);
                Timer.Spawn(comp.Time, () => RemComp<ConfusionComponent>(target));
            }
        }

        RemComp<EnchantedComponent>(uid);
        RemComp<ConfusionEnchantComponent>(uid);
    }

    private void ElectricalTouchOnMeleeHit(EntityUid uid, ElectricalTouchEnchantComponent comp, MeleeHitEvent args)
    {
        if (TryComp<WieldableComponent>(uid, out var wield) && wield.Wielded)
        {
            foreach (var target in args.HitEntities)
            {
                if (!HasComp<HumanoidProfileComponent>(target))
                    _emp.EmpPulse(Transform(target).Coordinates, 1f, 75000f, TimeSpan.FromSeconds(8));
                else
                    _emp.EmpPulse(Transform(target).Coordinates, 1f, 3000f, TimeSpan.FromSeconds(3));
            }

            comp.Uses--;
            if (comp.Uses <= 0)
            {
                RemComp<ElectricalTouchEnchantComponent>(uid);
                RemComp<EnchantedComponent>(uid);
            }
        }
    }

    private void StunOnMeleeHit(EntityUid uid, StunEnchantComponent comp, MeleeHitEvent args)
    {
        if (args.HitEntities.Count > 0)
        {
            foreach (var target in args.HitEntities)
            {
                if (comp.Knockout)
                    _stun.TryKnockdown(target, comp.StunTime, true, true, true);
                else
                    _stun.TryUpdateParalyzeDuration(target, comp.StunTime);

                if (comp.Mute)
                {
                    if (!HasComp<MutedComponent>(target))
                    {
                        EnsureComp<MutedComponent>(target);
                        Timer.Spawn(comp.MuteTime, () => RemComp<MutedComponent>(target));
                    }
                }

                if (comp.EmpBorgs && HasComp<BorgChassisComponent>(target) || HasComp<AndroidComponent>(target))
                    _emp.EmpPulse(Transform(target).Coordinates, 1f, 75000f, TimeSpan.FromSeconds(8));
            }

            RemComp<StunEnchantComponent>(uid);
            RemComp<EnchantedComponent>(uid);
        }
    }

    private void TerraformOnMeleeHit(EntityUid uid, TerraformEnchantComponent comp, MeleeHitEvent args)
    {
        if (args.Direction != null || args.HitEntities == null)
            return;

        foreach (var target in args.HitEntities)
        {
            if (MetaData(target).EntityPrototype?.ID == "WallSolid")
            {
                Spawn("SolidSecretDoor", Transform(target).Coordinates);
                QueueDel(target);

                RemComp<EnchantedComponent>(uid);
                RemComp<TerraformEnchantComponent>(uid);
                break;
            }
        }
    }

    private void ForcePassageOnMeleeHit(EntityUid uid, ForcePassageEnchantComponent comp, MeleeHitEvent args)
    {
        if (args.Direction != null || args.HitEntities == null)
            return;

        foreach (var target in args.HitEntities)
        {
            if (HasComp<DoorComponent>(target))
            {
                var emaggedEvent = new GotEmaggedEvent(uid, EmagType.Access);
                RaiseLocalEvent(target, ref emaggedEvent);
                RemComp<EnchantedComponent>(uid);
                RemComp<ForcePassageEnchantComponent>(uid);
                _door.TryOpen(target);
                break;
            }
        }
    }

    private void BloodshedOnMeleeHit(EntityUid uid, BloodshedEnchantComponent comp, MeleeHitEvent args)
    {
        if (args.HitEntities != null)
        {
            foreach (var target in args.HitEntities)
            {
                _blood.TryBleedOut(target, 100);
                _surgery.TryAddInternalDamage(target, "ArterialBleeding");
            }

            RemComp<BloodshedEnchantComponent>(uid);
            RemComp<EnchantedComponent>(uid);
        }
    }

    private void OnReflectionHitscan(EntityUid uid, ReflectionEnchantComponent comp, HitScanReflectAttemptEvent args)
    {
        comp.Uses--;
        if (comp.Uses <= 0)
        {
            RemComp<EnchantedComponent>(uid);
            RemComp<ReflectionEnchantComponent>(uid);
            RemComp<ReflectComponent>(uid);
        }
    }

    private void OnReflectionProjecile(EntityUid uid, ReflectionEnchantComponent comp, ProjectileReflectAttemptEvent args)
    {
        comp.Uses--;
        if (comp.Uses <= 0)
        {
            RemComp<EnchantedComponent>(uid);
            RemComp<ReflectionEnchantComponent>(uid);
            RemComp<ReflectComponent>(uid);
        }
    }

    private void ReconstructionOnUseInHand(EntityUid uid, ReconstructionEnchantComponent comp, UseInHandEvent args)
    {
        var damage = new DamageSpecifier { DamageDict = { { "Blunt", -30 }, { "Slash", -30 }, { "Piercing", -40 }, { "Heat", -40 } } };
        var nearbyCultists = _entityLookup.GetEntitiesInRange<VeilCultistComponent>(Transform(uid).Coordinates, comp.Radius);
        foreach (var cultist in nearbyCultists)
        {
            _damage.TryChangeDamage(cultist.Owner, damage, true);
            if (TryComp<BloodstreamComponent>(cultist.Owner, out var bloodstream))
                _blood.TryModifyBleedAmount((cultist.Owner, bloodstream), -5f);
        }

        var nearbyConstruct = _entityLookup.GetEntitiesInRange<VeilCultConstructComponent>(Transform(uid).Coordinates, comp.Radius);
        foreach (var construct in nearbyConstruct)
        {
            _damage.TryChangeDamage(construct.Owner, damage, true);
            if (TryComp<BloodstreamComponent>(construct.Owner, out var bloodstream))
                _blood.TryModifyBleedAmount((construct.Owner, bloodstream), -5f);
        }

        var nearbyWalls = _entityLookup.GetEntitiesInRange<OccluderComponent>(Transform(uid).Coordinates, comp.Radius)
            .Where(target => _tag.HasTag(target.Owner, WallTag))
            .ToList();

        foreach (var wall in nearbyWalls)
        {
            if (!_random.Prob(0.7f))
                continue;

            var delay = TimeSpan.FromSeconds(_random.NextFloat(0.1f, 1f));
            Timer.Spawn(delay, () =>
            {
                if (!Exists(wall.Owner))
                    return;

                var wallCoords = Transform(wall.Owner).Coordinates;
                Spawn("WallClock", wallCoords);
                QueueDel(wall.Owner);
            });
        }

        var cultistPos = _transform.GetWorldPosition(args.User);
        var tileDef = (ContentTileDefinition)_tileDefinitionManager["FloorBrassFilled"];
        var gridUid = _transform.GetGrid(args.User);
        if (gridUid != null && TryComp<MapGridComponent>(gridUid.Value, out var grid))
        {
            var tiles = _map.GetTilesIntersecting(gridUid.Value, grid,
                Box2.CenteredAround(cultistPos, new Vector2(4, 4)), ignoreEmpty: true);

            foreach (var tile in tiles)
            {
                if (!_random.Prob(0.5f))
                    continue;

                var delay = TimeSpan.FromSeconds(_random.NextFloat(0.1f, 1f));
                Timer.Spawn(delay, () => _tile.ReplaceTile(tile, tileDef));
            }
        }

        _audio.PlayPvs(CultSpell, args.User);
        QueueDel(uid);
    }

    private void EmpOnUseInHand(EntityUid uid, EmpEnchantComponent comp, UseInHandEvent args)
    {
        _emp.EmpPulse(Transform(uid).Coordinates, comp.RadiusWeak, 2500f, TimeSpan.FromSeconds(3));
        _emp.EmpPulse(Transform(uid).Coordinates, comp.RadiusStrong, 75000f, TimeSpan.FromSeconds(8));
        QueueDel(uid);
    }

    private void TimeStopOnUseInHand(EntityUid uid, TimeStopEnchantComponent comp, UseInHandEvent args)
    {
        var nearbyCultists = _entityLookup.GetEntitiesInRange<VeilCultistComponent>(Transform(uid).Coordinates, 5f);
        foreach (var cultist in nearbyCultists)
        {
            EnsureComp<PacifiedComponent>(cultist);
            Timer.Spawn(TimeSpan.FromSeconds(4), () => RemComp<PacifiedComponent>(cultist));
        }

        Spawn("Chronofield", Transform(args.User).Coordinates);
        var nearbyTargets = _entityLookup.GetEntitiesInRange<MobStateComponent>(Transform(uid).Coordinates, 2.5f)
           .Where(target => !HasComp<VeilCultistComponent>(target.Owner) && !HasComp<VeilCultConstructComponent>(target.Owner))
           .ToList();

        foreach (var target in nearbyTargets)
        {
            EnsureComp<AdminFrozenComponent>(target);
            Timer.Spawn(comp.Time, () => RemComp<AdminFrozenComponent>(target));
        }

        QueueDel(uid);
    }

    private void HidingCloacksOnUseInHand(EntityUid uid, HidingsClockEnchantComponent comp, UseInHandEvent args)
    {
        var structures = _entityLookup.GetEntitiesInRange<VeilCultStructureComponent>(Transform(uid).Coordinates, comp.Radius);
        foreach (var structure in structures)
        {
            if (TryComp<VeilCultStructureComponent>(structure.Owner, out var cultStructure))
            {
                if (TryComp<VisibilityComponent>(structure.Owner, out var vis))
                {
                    var entity = new Entity<VisibilityComponent?>(structure.Owner, vis);
                    if (cultStructure.IsActive)
                        _visibility.SetLayer(entity, 7);
                    else
                        _visibility.SetLayer(entity, 1);
                }
                else
                {
                    var newVisibilityComp = AddComp<VisibilityComponent>(structure.Owner);
                    var entity = new Entity<VisibilityComponent?>(structure.Owner, newVisibilityComp);
                    if (cultStructure.IsActive)
                        _visibility.SetLayer(entity, 7);
                    else
                        _visibility.SetLayer(entity, 1);
                }
                cultStructure.IsActive = !cultStructure.IsActive;
            }
        }

        comp.Uses--;
        if (comp.Uses <= 0)
        {
            RemComp<HidingsClockEnchantComponent>(uid);
            RemComp<EnchantedComponent>(uid);
        }
    }

    private void SealWoundOnUse(EntityUid uid, SealWoundsEnchantComponent comp, MeleeHitEvent args)
    {
        if (args.HitEntities != null)
        {
            foreach (var target in args.HitEntities)
            {
                if (!HasComp<VeilCultistComponent>(target))
                    continue;
                
                var damage = new DamageSpecifier { DamageDict = { { "Blunt", -10 }, { "Slash", -10 }, { "Piercing", -15 }, { "Heat", -20 } } };
                _damage.TryChangeDamage(target, damage, true);

                if (TryComp<BloodstreamComponent>(target, out var bloodstream))
                    _blood.TryModifyBleedAmount((target, bloodstream), -5f);
            }
            RemComp<EnchantedComponent>(uid);
            RemComp<SealWoundsEnchantComponent>(uid);
        }
    }
}
