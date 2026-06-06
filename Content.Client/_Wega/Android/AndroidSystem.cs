using Content.Client.Power.EntitySystems;
using Content.Shared.Alert;
using Content.Shared.Android;
using Content.Shared.PowerCell;

namespace Content.Client.Android;

public sealed partial class AndroidSystem : SharedAndroidSystem
{
    [Dependency] private PowerCellSystem _powerCell = default!;
    [Dependency] private BatterySystem _battery = default!;
    [Dependency] private AlertsSystem _alerts = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var androidsQuery = EntityQueryEnumerator<AndroidComponent>();
        while (androidsQuery.MoveNext(out var ent, out var component))
        {
            UpdateBatteryAlert((ent, component));
        }
    }

    private void UpdateBatteryAlert(Entity<AndroidComponent> ent)
    {
        if (!_powerCell.TryGetBatteryFromSlot(ent.Owner, out var battery))
        {
            _alerts.ShowAlert(ent.Owner, ent.Comp.NoBatteryAlert);
            return;
        }

        var chargeLevel = (short)MathF.Round(_battery.GetChargeLevel(battery.Value.AsNullable()) * 10f);

        if (chargeLevel == 0 && _powerCell.HasDrawCharge(ent.Owner))
        {
            chargeLevel = 1;
        }

        _alerts.ShowAlert(ent.Owner, ent.Comp.BatteryAlert, chargeLevel);
    }
}
