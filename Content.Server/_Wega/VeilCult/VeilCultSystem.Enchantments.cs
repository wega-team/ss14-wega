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
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Server.Audio;

namespace Content.Server.Veil.Cult;

public sealed partial class VeilCultSystem
{

    private void InitializeEnchantments()
    {
		
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
}