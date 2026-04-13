using System.Linq;
using Content.Server.Administration;
using Content.Server.Body.Systems;
using Content.Server.Chat.Systems;
using Content.Server.Emp;
using Content.Server.EUI;
using Content.Server.Flash;
using Content.Server.Hallucinations;
using Content.Shared.Bed.Sleep;
using Content.Shared.Veil.Cult;
using Content.Shared.Veil.Cult.Components;
using Content.Shared.Body.Components;
using Content.Shared.Card.Tarot;
using Content.Shared.Card.Tarot.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Clothing;
using Content.Shared.Cuffs;
using Content.Shared.Cuffs.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Doors.Components;
using Content.Shared.EnergyShield;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;
using Content.Shared.Humanoid;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.NullRod.Components;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Content.Shared.Stacks;
using Content.Shared.Standing;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Content.Shared.Timing;
using Content.Server.Audio;
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
using Content.Shared.Access.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Movement.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Veil.Cult;

public sealed partial class VeilCultSystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _speed = default!;

    private void InitializeEnchantments()
    {
		
		// Activate Action at enchanted item
		SubscribeLocalEvent<EnchantedComponent, CrusherEnchantActionEvent>(OnActivateCrusher);
		SubscribeLocalEvent<EnchantedComponent, ConfusionEnchantActionEvent>(OnActivateConfusion);
		SubscribeLocalEvent<EnchantedComponent, KnockbackEnchantActionEvent>(OnActivateKnockback);
		SubscribeLocalEvent<EnchantedComponent, SwordsmenEnchantActionEvent>(OnActivateSwordsmen);
		SubscribeLocalEvent<EnchantedComponent, BloodshedEnchantActionEvent>(OnActivateBloodShed);
		SubscribeLocalEvent<EnchantedComponent, HasteEnchantActionEvent>(OnActivateHaste);
		SubscribeLocalEvent<EnchantedComponent, ReflectionEnchantActionEvent>(OnActivateReflection);
		SubscribeLocalEvent<EnchantedComponent, CamouflageEnchantActionEvent>(OnActivateCamouflage);
		SubscribeLocalEvent<EnchantedComponent, AbsorbEnchantActionEvent>(OnActivateAbsorb);
		SubscribeLocalEvent<EnchantedComponent, FlashEnchantActionEvent>(OnActivateFlash);
		SubscribeLocalEvent<EnchantedComponent, HardenPlatesEnchantActionEvent>(OnActivateHardenPlates);
		SubscribeLocalEvent<EnchantedComponent, NorthStarEnchantActionEvent>(OnActivateNorthStar);
		SubscribeLocalEvent<EnchantedComponent, RedFlameEnchantActionEvent>(OnActivateRedFlame);
		
		// Enchants
		SubscribeLocalEvent<CrusherEnchantComponent, MeleeHitEvent>(CrusherOnMeleeHit);
		SubscribeLocalEvent<ConfusionEnchantComponent, MeleeHitEvent>(ConfusionOnMeleeHit);
		SubscribeLocalEvent<KnockbackEnchantComponent, MeleeHitEvent>(KnockbackOnMeleeHit);
		SubscribeLocalEvent<StunEnchantComponent, MeleeHitEvent>(StunOnMeleeHit);
		SubscribeLocalEvent<ForcePassageEnchantComponent, LightAttackEvent>(ForcePassageOnLightAttack);
		SubscribeLocalEvent<TerraformEnchantComponent, LightAttackEvent>(TerraformOnLightAttack);
		SubscribeLocalEvent<ElectricalTouchEnchantComponent, MeleeHitEvent>(ElectricalTouchOnMeleeHit);
		SubscribeLocalEvent<BloodshedEnchantComponent, MeleeHitEvent>(BloodshedOnMeleeHit);
		
		SubscribeLocalEvent<ReflectionEnchantComponent, ProjectileReflectAttemptEvent>(OnReflectionProjecile);
		SubscribeLocalEvent<ReflectionEnchantComponent, HitScanReflectAttemptEvent>(OnReflectionHitscan);
		
		SubscribeLocalEvent<ReconstructionEnchantComponent, UseInHandEvent>(ReconstructionOnUseInHand);
		SubscribeLocalEvent<EmpEnchantComponent, UseInHandEvent>(EmpOnUseInHand);
		SubscribeLocalEvent<TimeStopEnchantComponent, UseInHandEvent>(TimeStopOnUseInHand);
		SubscribeLocalEvent<HidingsClockEnchantComponent, UseInHandEvent>(HidingCloacksOnUseInHand);
		SubscribeLocalEvent<TeleportationEnchantComponent, UseInHandEvent>(TeleportOnUseInHand);
		
		SubscribeLocalEvent<SealWoundsEnchantComponent, AfterInteractEvent>(SealWoundOnUse);
		
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
	}
	
	private void OnActivateKnockback(EntityUid uid, EnchantedComponent comp, KnockbackEnchantActionEvent args)
	{
		EnsureComp<KnockbackEnchantComponent>(uid, out var kb);
		EnsureComp<MeleeThrowOnHitComponent>(uid, out var throwOnHit);
		throwOnHit.Speed = kb.Speed;
		throwOnHit.Distance = kb.Distance;
	}
	
	private void OnActivateConfusion(EntityUid uid, EnchantedComponent comp, ConfusionEnchantActionEvent args)
	{
		EnsureComp<ConfusionEnchantComponent>(uid);
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
	}
	
	private void OnActivateBloodShed(EntityUid uid, EnchantedComponent comp, BloodshedEnchantActionEvent args)
	{
		EnsureComp<BloodshedEnchantComponent>(uid);
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
		
	}
	
	private void OnActivateReflection(EntityUid uid, EnchantedComponent comp, ReflectionEnchantActionEvent args)
	{
		EnsureComp<ReflectComponent>(uid, out var refl);
		refl.ReflectingInHands = false;
		refl.ReflectProb = 1f;
	}
	
	private void OnActivateAbsorb(EntityUid uid, EnchantedComponent comp, AbsorbEnchantActionEvent args)
	{
        var user = args.Performer;
        var shield = EnsureComp<EnergyShieldOwnerComponent>(user);
        shield.ShieldEntity = Spawn("EnergyShieldEffect", Transform(user).Coordinates);
        shield.SustainingCount = 5;	
	}
			
	
	private void OnActivateCamouflage(EntityUid uid, EnchantedComponent comp, CamouflageEnchantActionEvent args)
	{
		EnsureComp<StealthComponent>(args.Performer, out var stealth);
		stealth.LastVisibility = 0.3f;
		Dirty(args.Performer, stealth);
		Timer.Spawn(TimeSpan.FromSeconds(10), () => 
		{
			RemComp<StealthComponent>(args.Performer);
			RemComp<CamouflageEnchantComponent>(uid);
			RemComp<EnchantedComponent>(uid);
		});
	}
	
	private void OnActivateFlash(EntityUid uid, EnchantedComponent comp, FlashEnchantActionEvent args)
	{
        var nearbyCultists = _entityLookup.GetEntitiesInRange<VeilCultistComponent>(Transform(uid).Coordinates, 10f);
		foreach (var cultist in nearbyCultists)
		{
			EnsureComp<FlashImmunityComponent>(cultist.Owner);
			Timer.Spawn(TimeSpan.FromSeconds(1), () => RemComp<FlashImmunityComponent>(cultist.Owner));
		}
        var nearbyConstruct = _entityLookup.GetEntitiesInRange<VeilCultConstructComponent>(Transform(uid).Coordinates, 10f);
		foreach (var construct in nearbyConstruct)
		{
			EnsureComp<FlashImmunityComponent>(construct.Owner);
			Timer.Spawn(TimeSpan.FromSeconds(1), () => RemComp<FlashImmunityComponent>(construct.Owner));
		}
		_flash.FlashArea(uid, args.Performer, 10f, TimeSpan.FromSeconds(3));
		RemComp<FlashEnchantComponent>(uid);
		RemComp<EnchantedComponent>(uid);
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
			armor.Modifiers.Coefficients["Blunt"] = 0.3f;
			armor.Modifiers.Coefficients["Slash"] = 0.3f;
			armor.Modifiers.Coefficients["Piercing"] = 0.4f;
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
	}
	
	private void OnActivateNorthStar(EntityUid uid, EnchantedComponent comp, NorthStarEnchantActionEvent args)
	{
		EnsureComp<NorthStarEnchantComponent>(uid, out var enchant);
		if (TryComp<MeleeWeaponComponent>(uid, out var weapon))
		{
			var oldRate = weapon.AttackRate;
			weapon.AttackRate = enchant.AttackRate;
			Timer.Spawn(TimeSpan.FromSeconds(6), () =>
			{
				weapon.AttackRate = oldRate;
				RemComp<EnchantedComponent>(uid);
				RemComp<NorthStarEnchantComponent>(uid);
			});
		}
	}
	
	private void OnActivateRedFlame(EntityUid uid, EnchantedComponent comp, RedFlameEnchantActionEvent args)
	{
		EnsureComp<RedFlameEnchantComponent>(uid, out var enchant);
		EnsureComp<IgniteOnMeleeHitComponent>(uid, out var flame);
		flame.FireStacks = 1;
		Timer.Spawn(enchant.Time, () =>
		{
			RemComp<RedFlameEnchantComponent>(uid);
			RemComp<EnchantedComponent>(uid);
			RemComp<IgniteOnMeleeHitComponent>(uid);
		});
	}
	
	private void KnockbackOnMeleeHit(EntityUid uid, KnockbackEnchantComponent comp, MeleeHitEvent args)
	{
		if (args.IsHit && args.HitEntities.Count > 0)
		{
			comp.Uses -= 1;
		}
		if (comp.Uses <= 0)
		{
			RemComp<KnockbackEnchantComponent>(uid);
			RemComp<MeleeThrowOnHitComponent>(uid);
			RemComp<EnchantedComponent>(uid);
		}
	}
	
	private void CrusherOnMeleeHit(EntityUid uid, CrusherEnchantComponent comp, MeleeHitEvent args)
	{
		if (TryComp<WieldableComponent>(uid, out var wield))
		{
			if (wield.Wielded)
			{
				args.BonusDamage += new DamageSpecifier { DamageDict = { { "Blunt", 30 } } }; // Тут должна быть логика перелома, но у нас умерла хирургия.
			}
		}
		RemComp<CrusherEnchantComponent>(uid);
		RemComp<EnchantedComponent>(uid);
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
			comp.Uses -= 1;
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
				if (HasComp<StaminaComponent>(target))
					_stun.TryUpdateParalyzeDuration(target, comp.StunTime);
				if (comp.Mute)
				{
					if (HasComp<MutedComponent>(target))
						continue;
					EnsureComp<MutedComponent>(target);
					Timer.Spawn(comp.MuteTime, () => RemComp<MutedComponent>(target));
				}
				if (comp.EmpBorgs && HasComp<BorgChassisComponent>(target) || HasComp<AndroidComponent>(target))
					_emp.EmpPulse(Transform(target).Coordinates, 1f, 75000f, TimeSpan.FromSeconds(8));
			}
				RemComp<StunEnchantComponent>(uid);
				RemComp<EnchantedComponent>(uid);
		}
			
	}
	
	private void TerraformOnLightAttack(EntityUid uid, TerraformEnchantComponent comp, LightAttackEvent args)
	{
		if (args.Target == null)
			return;
		
		var target = GetEntity(args.Target.Value);
		
		if (MetaData(target).EntityPrototype?.ID == "WallSolid")
		{
			Spawn("SolidSecretDoor", Transform(target).Coordinates);
			QueueDel(target);
			RemComp<EnchantedComponent>(uid);
			RemComp<TerraformEnchantComponent>(uid);
		}
	}
	
	private void ForcePassageOnLightAttack(EntityUid uid, ForcePassageEnchantComponent comp, LightAttackEvent args)
	{
		if (args.Target == null)
			return;
		
		var target = GetEntity(args.Target.Value);

		if (HasComp<AirlockComponent>(target) && HasComp<AccessReaderComponent>(target))
		{
			RemComp<AccessReaderComponent>(target);
			RemComp<EnchantedComponent>(uid);
			RemComp<ForcePassageEnchantComponent>(uid);
		}
	}
	
	private void BloodshedOnMeleeHit(EntityUid uid, BloodshedEnchantComponent comp, MeleeHitEvent args)
	{
		if (args.HitEntities != null)
		{
			foreach (var target in args.HitEntities)
			{
				_blood.TryBleedOut(target, 100);
				// должна быть логика на внутренний кровотек, но хирургии гг.
			}
			RemComp<BloodshedEnchantComponent>(uid);
			RemComp<EnchantedComponent>(uid);
		}
	}
	
	private void OnReflectionHitscan(EntityUid uid, ReflectionEnchantComponent comp, HitScanReflectAttemptEvent args)
	{
		if (args.Reflected)
			return;
		
		comp.Uses -= 1;
		if (comp.Uses <= 0)
		{
			RemComp<EnchantedComponent>(uid);
			RemComp<ReflectionEnchantComponent>(uid);
			RemComp<ReflectComponent>(uid);
		}
	}
	
	private void OnReflectionProjecile(EntityUid uid, ReflectionEnchantComponent comp, ProjectileReflectAttemptEvent args)
	{
		if (args.Cancelled)
			return;
		
		comp.Uses -= 1;
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
		var nearbyCultists = _entityLookup.GetEntitiesInRange<VeilCultistComponent>(Transform(uid).Coordinates, 3f);
		foreach (var cultist in nearbyCultists)
		{
			_damage.TryChangeDamage(cultist.Owner, damage, true);
		}
        var nearbyConstruct = _entityLookup.GetEntitiesInRange<VeilCultConstructComponent>(Transform(uid).Coordinates, 3f);
		foreach (var construct in nearbyConstruct)
		{
			_damage.TryChangeDamage(construct.Owner, damage, true);
		}
		QueueDel(uid);
	}
	
	private void EmpOnUseInHand(EntityUid uid, EmpEnchantComponent comp, UseInHandEvent args)
	{
		_emp.EmpPulse(Transform(uid).Coordinates, comp.RadiusWeak, 2500f, TimeSpan.FromSeconds(3));
		_emp.EmpPulse(Transform(uid).Coordinates, comp.RadiusStrong, 75000f, TimeSpan.FromSeconds(8));
		QueueDel(uid);
	}
	
	private void TeleportOnUseInHand(EntityUid uid, TeleportationEnchantComponent comp, UseInHandEvent args)
	{
        var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, TimeSpan.FromSeconds(4),
            new VeilCultTeleportDoAfterEvent(),
            eventTarget: args.User,
			target: args.User)
        {
            BreakOnMove = false,
            BreakOnDamage = true,
            NeedHand = false
        };

            _doAfterSystem.TryStartDoAfter(doAfterEventArgs);
	}
	
	private void OnTeleportSuccess(EntityUid uid, VeilCultistComponent comp, VeilCultTeleportDoAfterEvent args)
	{
        var beacons = new List<EntityUid>();
		var beaconQuery = EntityQueryEnumerator<VeilCultBeaconComponent>();
        while (beaconQuery.MoveNext(out var beaconUid, out var beaconCompQ))
                beacons.Add(beaconUid);	

		var randomBeacon = beacons[_random.Next(beacons.Count)];
		var coordinates = Transform(randomBeacon).Coordinates;
		
		Spawn("BloodCultOutEffect", Transform(uid).Coordinates);
        _transform.SetCoordinates(uid, coordinates);
        Spawn("BloodCultInEffect", coordinates);
        QueueDel(args.Used);
	}
	
	private void TimeStopOnUseInHand(EntityUid uid, TimeStopEnchantComponent comp, UseInHandEvent args)
	{
        var nearbyTargets = _entityLookup.GetEntitiesInRange<MobStateComponent>(Transform(uid).Coordinates, 3f)
           .Where(target => !HasComp<VeilCultistComponent>(target.Owner))
           .Where(target => !HasComp<VeilCultConstructComponent>(target.Owner))
           .ToList();
		foreach (var target in nearbyTargets)
		{
			EnsureComp<AdminFrozenComponent>(target);
			Timer.Spawn(TimeSpan.FromSeconds(5), () => RemComp<AdminFrozenComponent>(target));
		}
		   
	}
	
	private void HidingCloacksOnUseInHand(EntityUid uid, HidingsClockEnchantComponent comp, UseInHandEvent args)
	{
		var structures = _entityLookup.GetEntitiesInRange<VeilCultStructureComponent>(Transform(uid).Coordinates, 5f);
		foreach (var structure in structures)
		{
			if (TryComp<VeilCultStructureComponent>(structure.Owner, out var cultStructure))
			{
				if (TryComp<VisibilityComponent>(structure.Owner, out var vis))
				{
					var entity = new Entity<VisibilityComponent?>(structure.Owner, vis);
					if (cultStructure.IsActive)
                        _visibility.SetLayer(entity, 6);
                    else
                        _visibility.SetLayer(entity, 1);
						
				}
                else
                {
                    var newVisibilityComp = AddComp<VisibilityComponent>(structure.Owner);
                    var entity = new Entity<VisibilityComponent?>(structure.Owner, newVisibilityComp);
                    if (cultStructure.IsActive)
                        _visibility.SetLayer(entity, 6);
                    else
                        _visibility.SetLayer(entity, 1);
                }
				cultStructure.IsActive = !cultStructure.IsActive;
			}
		}
	}
	
	private void SealWoundOnUse(EntityUid uid, SealWoundsEnchantComponent comp, AfterInteractEvent args)
	{
		if (args.Target != null)
		{
			if (HasComp<VeilCultistComponent>(args.Target.Value))
			{
				var damage = new DamageSpecifier { DamageDict = { { "Blunt", -15 }, { "Slash", -15 }, { "Piercing", -20 }, { "Heat", -30 } } };
				_damage.TryChangeDamage(args.Target.Value, damage, true);
				RemComp<EnchantedComponent>(args.Used);
				RemComp<SealWoundsEnchantComponent>(args.Used);
			}
		}
	}
}