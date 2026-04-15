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
using Content.Shared.Silicons.StationAi;
using Content.Shared.Silicons.Laws.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Veil.Cult;

public sealed partial class VeilCultSystem
{
    [Dependency] private readonly BloodstreamSystem _blood = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly EmpSystem _emp = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly EuiManager _euiMan = default!;
    [Dependency] private readonly FixtureSystem _fixtures = default!;
    [Dependency] private readonly FlashSystem _flash = default!;
    [Dependency] private readonly HallucinationsSystem _hallucinations = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly LoadoutSystem _loadout = default!;
    [Dependency] private readonly QuickDialogSystem _quickDialog = default!;
    [Dependency] private readonly SharedCuffableSystem _cuff = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffect = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly VisibilitySystem _visibility = default!;
    [Dependency] private readonly SharedStationAiSystem _stationAi = default!;

    private static readonly SoundPathSpecifier CultSpell = new SoundPathSpecifier("/Audio/_Wega/Effects/cult_spell.ogg");
	private readonly int EnergyPerOne = 100;

    private void InitializeVeilAbilities()
    {

        SubscribeLocalEvent<VeilCultistComponent, VeilCultMidasTouchGetHandEvent>(OnMidasTouch);
        SubscribeLocalEvent<MidasHandComponent, AfterInteractEvent>(OnInteract);
        SubscribeLocalEvent<MidasHandComponent, MidasTouchDoAfterEvent>(DoAfterInteract);
        // Abilities
    }


    #region Abilities

	public void OnMidasTouch(EntityUid cultist, VeilCultistComponent component, VeilCultMidasTouchGetHandEvent args)
	{
		var spellGear = new ProtoId<StartingGearPrototype>("VeilCultMidasTouchGear");

		var dropEvent = new DropHandItemsEvent();
		RaiseLocalEvent(cultist, ref dropEvent);
		List<ProtoId<StartingGearPrototype>> gear = new() { spellGear };
		_loadout.Equip(cultist, gear, null);
	}
	
    #endregion
	
	private void OnInteract(EntityUid uid, MidasHandComponent component, AfterInteractEvent args)
	{
	        var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, TimeSpan.FromSeconds(2),
            new MidasTouchDoAfterEvent(),
            eventTarget: uid,
			target: args.Target)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            MovementThreshold = 0.01f,
            NeedHand = false
        };

            _doAfterSystem.TryStartDoAfter(doAfterEventArgs);
	}
	
	private void DoAfterInteract(EntityUid uid, MidasHandComponent component, MidasTouchDoAfterEvent args)
	{
		if (args.Target != null)
		{
			if (TryComp<StackComponent>(args.Target, out var stack))
			{
				TransformMaterial(args.User, args.Target.Value, stack);
				QueueDel(uid);
				return;
			}
			
			if (TryComp<SiliconLawProviderComponent>(args.Target, out var laws))
			{
				ChangeBorgLaws(args.Target.Value, laws);
				QueueDel(uid);
				return;
			}
			
			if (TryComp<StationAiCoreComponent>(args.Target, out var core))
			{
				ChangeAiLaws(args.Target.Value, core);
				QueueDel(uid);
				return;
			}
		}
		
	}
	
    private void TransformMaterial(EntityUid user, EntityUid material, StackComponent stack)
    {
        if (!_prototypeManager.TryIndex(stack.StackTypeId, out var stackPrototype))
            return;

        if (stackPrototype.ID is not ("Steel" or "Plasteel" or "Brass"))
            return;

        var coords = Transform(material).Coordinates;

        if (stackPrototype.ID == "Steel")
        {
            TransformSteelToBrass(material, coords, stack.Count);
        }
        else if (stackPrototype.ID == "Plasteel")
        {
            TransformToChargedBrass(material, coords, stack.Count);
        }
        else if (stackPrototype.ID == "Brass")
        {
            TransformToChargedBrass(material, coords, stack.Count);
        }

        _audio.PlayPvs(CultSpell, user);
    }
	

    private void TransformSteelToBrass(EntityUid metalStack, EntityCoordinates coords, int count)
    {
		var brass = Spawn("SheetBrass1", coords);
		QueueDel(metalStack);
		
		if (TryComp<StackComponent>(brass, out var newStack))
			_stack.SetCount((brass, newStack), count);
    }

    private void TransformToChargedBrass(EntityUid metalStack, EntityCoordinates coords, int count)
    {
		
		var cult = _veilCult.GetActiveRule();
		if (cult == null)
			return;
		
		if (_veilCult.TryUseEnergy(count*EnergyPerOne))
		{
			var chargedBrass = Spawn("SheetChargedBrass1", coords);
			QueueDel(metalStack);

			if (TryComp<StackComponent>(chargedBrass, out var newStack))
				_stack.SetCount((chargedBrass, newStack), count);
		}
    }
	
	private void ChangeBorgLaws(EntityUid uid, SiliconLawProviderComponent comp)
	{
		var ev = new SiliconVeilCultHackedEvent();
		RaiseLocalEvent(uid, ref ev);
	}
	
	private void ChangeAiLaws(EntityUid uid, StationAiCoreComponent core)
	{
		
		if (_stationAi.TryGetHeld((uid, core), out var mind))
		{
			if (mind != null)
			{
				if (HasComp<SiliconLawProviderComponent>(mind.Value))
				{
					var ev = new SiliconVeilCultHackedEvent();
					RaiseLocalEvent(mind.Value, ref ev);
				}
			}
		}

	}
}
