using Content.Shared.Alert;
using Content.Shared.Body;
using Content.Shared.Body.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Coordinates;
using Content.Shared.Mobs.Systems;
using Content.Shared.Whitelist;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Robust.Shared.Timing;

namespace Content.Server.Damage.Components;

public sealed partial class MedicalAuraSystem : EntitySystem
{
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private AlertsSystem _alert = default!;
    [Dependency] private EntityLookupSystem _entityLookup = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private BloodstreamSystem _blood = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _gameTiming.CurTime;
        var pylonQuery = EntityQueryEnumerator<MedicalAuraComponent>();
        while (pylonQuery.MoveNext(out var uid, out var comp))
        {
            if (comp.NextTimeTick <= curTime)
            {
				comp.NextTimeTick = curTime + comp.Interval;
				foreach (var ent in _entityLookup.GetEntitiesInRange(uid, comp.Radius))
                {
					if (_whitelistSystem.IsWhitelistFail(comp.Whitelist, ent) || _whitelistSystem.IsWhitelistPass(comp.Blacklist, ent))
						continue;

					if (_mobState.IsDead(ent))
						continue;

					if (comp.Heal == null)
						continue;

                    _damageable.TryChangeDamage(ent, comp.Heal, true);

                    _blood.TryModifyBloodLevel(ent, comp.Blood);
					_blood.TryModifyBleedAmount(ent, comp.Bleed);


					if (comp.Alert != null && HasComp<AlertsComponent>(ent))
					{
						var endTime = curTime + comp.Interval;
						_alert.ShowAlert(ent, comp.Alert.Value, cooldown: (curTime, endTime), autoRemove: true, showCooldown: false);
					}
                }
            }
        }
	}
}