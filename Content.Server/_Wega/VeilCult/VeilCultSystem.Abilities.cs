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

    private static readonly SoundPathSpecifier CultSpell = new SoundPathSpecifier("/Audio/_Wega/Effects/cult_spell.ogg");

    private void InitializeVeilAbilities()
    {

        // Abilities
        SubscribeLocalEvent<VeilCultistComponent, VeilCultMidasTouchActionEvent>(OnMidasTouch);
    }


    #region Abilities

    private void OnMidasTouch(EntityUid cultist, VeilCultistComponent component, VeilCultMidasTouchActionEvent args)
    {
        var spellGear = new ProtoId<StartingGearPrototype>("VeilCultMidasTouchGear");

        var dropEvent = new DropHandItemsEvent();
        RaiseLocalEvent(cultist, ref dropEvent);
        List<ProtoId<StartingGearPrototype>> gear = new() { spellGear };
        _loadout.Equip(cultist, gear, null);

        args.Handled = true;
    }
    #endregion
}
