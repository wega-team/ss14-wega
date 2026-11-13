using Content.Shared.Lavaland.Components;
using Content.Shared.Modular.Suit;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Robust.Shared.Timing;

namespace Content.Server.Modular.Suit;

public sealed partial class AffectedModuleAshAccretionEngageSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private MovementSpeedModifierSystem _speed = default!;

    /// <remarks>
    /// Плохо by design: мод сьют не рассылает событие при запечатывании,
    /// а смену карты получает носитель, а не костюм. Поэтому модуль
    /// включается и выключается с задержкой до ~1 секунды.
    /// </remarks>
    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);
    private TimeSpan _nextUpdate;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextUpdate)
            return;

        _nextUpdate = _timing.CurTime + UpdateInterval;

        var query = EntityQueryEnumerator<AffectedModuleAshAccretionComponent, ModularSuitComponent>();
        while (query.MoveNext(out var uid, out var accretion, out var suit))
        {
            var engaged = ShouldEngage(suit);
            if (accretion.Engaged == engaged)
                continue;

            accretion.Engaged = engaged;
            Dirty(uid, accretion);

            if (suit.Wearer is not { } wearer || TerminatingOrDeleted(wearer))
                continue;

            _speed.RefreshMovementSpeedModifiers(wearer);
            _popup.PopupEntity(Loc.GetString(engaged
                ? "modsuit-ash-accretion-engaged"
                : "modsuit-ash-accretion-disengaged"), wearer, wearer);
        }
    }

    private bool ShouldEngage(ModularSuitComponent suit)
    {
        if (!suit.Active || suit.Wearer is not { } wearer)
            return false;

        if (!TryComp(wearer, out TransformComponent? xform))
            return false;

        return HasComp<LavalandComponent>(xform.MapUid);
    }
}
