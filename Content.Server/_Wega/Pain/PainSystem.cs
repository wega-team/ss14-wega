using System.Linq;
using Content.Server.Chat.Systems;
using Content.Shared.Jittering;
using Content.Shared.Medical;
using Content.Shared.Movement.Systems;
using Content.Shared.Pain;
using Content.Shared.Pain.Components;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Traits.Assorted;
using Robust.Shared.Prototypes;

namespace Content.Server.Pain;

public sealed class PainSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _emoting = default!;
    [Dependency] private readonly SharedJitteringSystem _jittering = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly VomitSystem _vomit = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly MovementModStatusSystem _movementMod = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PainComponent, ComponentInit>(OnInit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<PainComponent>();
        while (query.MoveNext(out _, out var pain))
        {
            foreach (var (_, level) in pain.PainLevels)
            {
                level.CurrentLevel = Math.Max(0, level.CurrentLevel - level.DecayRate * frameTime);
            }
        }
    }

    private void OnInit(EntityUid uid, PainComponent component, ComponentInit args)
    {
        if (!_proto.TryIndex(component.Profile, out var profile))
            return;

        foreach (var (type, level) in profile.PainTypes)
        {
            component.PainLevels[type] = new PainLevel
            {
                Type = level.Type,
                DecayRate = level.DecayRate,
                Effects = level.Effects.ToList()
            };
        }
    }

    public bool EnsurePainType(EntityUid uid, string type, PainLevel? template = null, PainComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (!component.PainLevels.ContainsKey(type))
        {
            component.PainLevels[type] = template ?? new PainLevel { Type = type };
            return true;
        }
        return false;
    }

    public void AdjustPain(EntityUid uid, string type, float amount, PainComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        EnsurePainType(uid, type, null, component);

        var pain = component.PainLevels[type];
        pain.CurrentLevel = Math.Max(0, pain.CurrentLevel + amount);

        CheckPainEffects(uid, pain);
    }

    private void CheckPainEffects(EntityUid uid, PainLevel pain)
    {
        foreach (var effect in pain.Effects.OrderBy(e => e.Threshold))
        {
            if (pain.CurrentLevel >= effect.Threshold)
            {
                ApplyEffect(uid, effect);
            }
        }
    }

    private void ApplyEffect(EntityUid uid, PainEffect effect)
    {
        switch (effect.Effect)
        {
            case PainEffectType.Emote when effect.Message != null:
                if (!HasComp<PainNumbnessComponent>(uid))
                    _emoting.TryEmoteWithoutChat(uid, effect.Message);
                break;

            case PainEffectType.Popup when effect.Message != null:
                if (!HasComp<PainNumbnessComponent>(uid))
                    _popup.PopupEntity(Loc.GetString(effect.Message), uid, uid, PopupType.SmallCaution);
                break;

            case PainEffectType.MovementPenalty:
                _movementMod.TryUpdateMovementSpeedModDuration(uid, MovementModStatusSystem.Slowdown, TimeSpan.FromSeconds(effect.Duration), effect.SpeedMultiplier);
                break;

            case PainEffectType.DropItem:
                var dropEvent = new DropHandItemsEvent();
                RaiseLocalEvent(uid, ref dropEvent);
                break;

            case PainEffectType.Stun:
                _stun.TryKnockdown(uid, TimeSpan.FromSeconds(effect.Duration), true);
                break;

            case PainEffectType.Vomit:
                _vomit.Vomit(uid);
                break;

            case PainEffectType.Twitch:
                _jittering.DoJitter(uid, TimeSpan.FromSeconds(effect.Duration), true);
                break;
        }
    }
}
