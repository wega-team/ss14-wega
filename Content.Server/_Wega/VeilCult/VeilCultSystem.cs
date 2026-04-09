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
    [Dependency] private readonly BloodCultRuleSystem _bloodCult = default!;
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


    public override void Initialize()
    {
        base.Initialize();

        InitializeVeilAbilities();
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


}