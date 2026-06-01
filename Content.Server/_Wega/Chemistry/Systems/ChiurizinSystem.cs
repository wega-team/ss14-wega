using Content.Server._Wega.Chemistry.Components;
using Content.Shared.Administration;
using System.Linq;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.DragDrop;
using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Pulling.Events;
using Content.Shared.Verbs;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server._Wega.Chemistry.Systems;

public sealed class ChiurizinSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedStealthSystem _stealth = default!;

    private static readonly DamageSpecifier StasisHealPerSecond = new()
    {
        DamageDict = new Dictionary<ProtoId<DamageTypePrototype>, FixedPoint2>
        {
            ["Blunt"] = FixedPoint2.New(-2),
            ["Slash"] = FixedPoint2.New(-2),
            ["Heat"] = FixedPoint2.New(-2),
            ["Piercing"] = FixedPoint2.New(-2),
            ["Poison"] = FixedPoint2.New(-2),
            ["Radiation"] = FixedPoint2.New(-2),
            ["Asphyxiation"] = FixedPoint2.New(-2),
            ["Bloodloss"] = FixedPoint2.New(-2),
            ["Cellular"] = FixedPoint2.New(-2),
        }
    };

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChiurizinStasisComponent, BeforeDamageChangedEvent>(OnBeforeDamage);
        SubscribeLocalEvent<ChiurizinStasisComponent, ActivateInWorldEvent>(OnStripActivate,
            before: new[] { typeof(Content.Shared.Strip.SharedStrippableSystem) });
        SubscribeLocalEvent<ChiurizinStasisComponent, DragDropDraggedEvent>(OnStripDragDrop,
            before: new[] { typeof(Content.Shared.Strip.SharedStrippableSystem) });
        SubscribeLocalEvent<ChiurizinStasisComponent, GetVerbsEvent<Verb>>(OnGetStripVerbs,
            after: new[] { typeof(Content.Shared.Strip.SharedStrippableSystem) });
        SubscribeLocalEvent<ChiurizinStasisComponent, GetVerbsEvent<ExamineVerb>>(OnGetStripExamineVerbs,
            after: new[] { typeof(Content.Shared.Strip.SharedStrippableSystem) });
        SubscribeLocalEvent<ChiurizinStasisComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<ChiurizinStasisComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<ChiurizinStasisComponent, BeingPulledAttemptEvent>(OnBeingPulled);
    }

    private void OnBeforeDamage(EntityUid uid, ChiurizinStasisComponent _, ref BeforeDamageChangedEvent args)
    {
        if (args.Damage.GetTotal() > FixedPoint2.Zero)
            args.Cancelled = true;
    }

    private static void OnInteractUsing(EntityUid _, ChiurizinStasisComponent __, InteractUsingEvent args)
    {
        args.Handled = true;
    }

    private static void OnInteractHand(EntityUid _, ChiurizinStasisComponent __, InteractHandEvent args)
    {
        args.Handled = true;
    }

    private static void OnBeingPulled(EntityUid _, ChiurizinStasisComponent __, BeingPulledAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnStripActivate(EntityUid uid, ChiurizinStasisComponent _, ActivateInWorldEvent args)
    {
        args.Handled = true;
    }

    private void OnStripDragDrop(EntityUid uid, ChiurizinStasisComponent _, ref DragDropDraggedEvent args)
    {
        args.Handled = true;
    }

    private void OnGetStripVerbs(EntityUid uid, ChiurizinStasisComponent _, GetVerbsEvent<Verb> args)
    {
        var stripText = Loc.GetString("strip-verb-get-data-text");
        foreach (var verb in args.Verbs.Where(v => v.Text == stripText).ToList())
            args.Verbs.Remove(verb);
    }

    private void OnGetStripExamineVerbs(EntityUid uid, ChiurizinStasisComponent _, GetVerbsEvent<ExamineVerb> args)
    {
        var stripText = Loc.GetString("strip-verb-get-data-text");
        foreach (var verb in args.Verbs.Where(v => v.Text == stripText).ToList())
            args.Verbs.Remove(verb);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ChiurizinStasisComponent>();
        while (query.MoveNext(out var uid, out var stasis))
        {
            stasis.Timer -= frameTime;

            stasis.HealAccumulator += frameTime;
            while (stasis.HealAccumulator >= 1f)
            {
                _damageable.TryChangeDamage(uid, StasisHealPerSecond, ignoreResistances: true);
                stasis.HealAccumulator -= 1f;
            }

            if (stasis.Timer > 0)
                continue;

            RemComp<AdminFrozenComponent>(uid);

            if (stasis.VisualEntity.HasValue)
                Del(stasis.VisualEntity.Value);

            if (stasis.AddedStealth)
                RemComp<StealthComponent>(uid);
            else
                _stealth.SetVisibility(uid, stasis.OriginalVisibility);

            if (TryComp<ChiurizinStateComponent>(uid, out var state) && state.SavedPosition.HasValue)
                _xform.SetMapCoordinates(uid, state.SavedPosition.Value);

            RemComp<ChiurizinStasisComponent>(uid);
        }
    }
}
