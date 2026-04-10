using System.Linq;
using System.Numerics;
using Content.Server.Audio;
using Content.Server.GameTicking.Rules;
using Content.Server.RoundEnd;
using Content.Shared.Actions;
using Content.Shared.Veil.Cult;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.EnergyShield;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Veil.Cult.Components;
using Content.Shared.PowerCell;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Power.Components;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;


namespace Content.Server.Veil.Cult;

public sealed partial class VeilCultSystem : SharedVeilCultSystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly VeilCultRuleSystem _veilCult = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
    [Dependency] private readonly ServerGlobalSoundSystem _sound = default!;
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly SharedBatterySystem _battery = default!;


    public override void Initialize()
    {
        base.Initialize();

        InitializeVeilAbilities();
		
		SubscribeLocalEvent<VeilCultistHandsComponent, ExaminedEvent>(OnCultistHandsExamined);
		SubscribeLocalEvent<VeilCultBeaconComponent, ComponentInit>(OnBeaconSpawn);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var beaconQuery = EntityQueryEnumerator<VeilCultBeaconComponent>();
        while (beaconQuery.MoveNext(out var beacon, out var beaconQueryComponent))
        {
            if (beaconQueryComponent.NextTimeTick <= 0)
            {
                beaconQueryComponent.NextTimeTick = 5;
                var nearbyCultists = _entityLookup.GetEntitiesInRange<VeilCultistComponent>(Transform(beacon).Coordinates, 11f)
                    .Where(cultist => !_mobState.IsDead(cultist))
                    .ToList();

                var nearbyConstruct = _entityLookup.GetEntitiesInRange<VeilCultConstructComponent>(Transform(beacon).Coordinates, 11f)
                    .Where(cultist => !_mobState.IsDead(cultist))
                    .ToList();

                foreach (var target in nearbyCultists)
                {
                    var heal = new DamageSpecifier { DamageDict = { { "Blunt", -5 }, { "Slash", -5 }, { "Piercing", -10 }, { "Heat", -10 } } };
                    _damage.TryChangeDamage(target.Owner, heal, true);

                    _blood.TryModifyBloodLevel(target.Owner, +5);
                }

                foreach (var target in nearbyConstruct)
                {
                    var heal = new DamageSpecifier { DamageDict = { { "Blunt", -5 }, { "Slash", -5 }, { "Piercing", -10 }, { "Heat", -10 } } };
                    _damage.TryChangeDamage(target.Owner, heal, true);

                    _blood.TryModifyBloodLevel(target.Owner, +5);
                }
				
				var cult = _veilCult.GetActiveRule();
				if (cult != null)
				{
					cult.EnergyCount += 10;
				}
            }
			
            beaconQueryComponent.NextTimeTick -= frameTime;
     	 }
		 
		var cogQuery = EntityQueryEnumerator<InteractionCogInfectedComponent>();
        while (cogQuery.MoveNext(out var cog, out var cogQueryComponent))
        {
            if (cogQueryComponent.NextTimeTick <= 0)
            {
                cogQueryComponent.NextTimeTick = 5;
				if (TryComp<BatteryComponent>(Transform(cog).ParentUid, out var battery))
				{
					if (_battery.TryUseCharge((Transform(cog).ParentUid, battery), cogQueryComponent.PowerRate))
					{
						_audio.PlayPvs(_audio.ResolveSound(cogQueryComponent.Sound), Transform(cog).ParentUid);
						var cult = _veilCult.GetActiveRule();
						if (cult != null)
						{
							cult.EnergyCount += 10;
						}
					}
				}
			}
			else
				cogQueryComponent.NextTimeTick -= frameTime;
		}
	}

    private void OnCultistHandsExamined(EntityUid uid, VeilCultistHandsComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

		if (TryComp<InventoryComponent>(uid, out var inventory))
		{
			if (!_inventory.TryGetSlotEntity(uid, "gloves", out _, inventory))
				args.PushMarkup(Loc.GetString("veil-cultist-hands-glow-examined"));
		}
	}
	
	private void OnBeaconSpawn(EntityUid uid, VeilCultBeaconComponent component, ComponentInit args)
    {
		if (TryComp<TransformComponent>(uid, out var transform))
		{
			var beacons = _entityLookup.GetEntitiesInRange<VeilCultBeaconComponent>(
				Transform(uid).Coordinates, 20f);

			if (beacons.Count > 1)
			{
				Spawn("SheetBrass6", Transform(uid).Coordinates);
				QueueDel(uid);
			}
		}
	}
}