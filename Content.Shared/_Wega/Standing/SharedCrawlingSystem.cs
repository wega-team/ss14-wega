using Content.Shared.DoAfter;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Body.Components;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;
using Content.Shared.Standing;
using Content.Shared.Input;
using Content.Shared.Carrying;
using Content.Shared.Body.Part;
using Content.Shared.Buckle.Components;
using Content.Shared.Surgery.Components;

namespace Content.Shared.Crawling;

public sealed class SharedCrawlingSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speed = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CrawlingComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<CrawlingComponent, ComponentShutdown>(OnComponentShutdown);

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.ToggleCrawling,
                InputCmdHandler.FromDelegate(HandleToggleCrawling, handle: false))
            .Register<SharedCrawlingSystem>();

        SubscribeLocalEvent<StandingStateComponent, CrawlingStandUpDoAfterEvent>(OnStandingUpDoAfter);

        SubscribeLocalEvent<CrawlingComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeed);
        SubscribeLocalEvent<CrawlingComponent, BodyPartRemovedEvent>(OnBodyPartRemoved);
        SubscribeLocalEvent<CrawlingComponent, BuckleAttemptEvent>(OnBuckleAttempt);
        SubscribeLocalEvent<CrawlingComponent, UnbuckledEvent>(OnUnbuckled);
    }

    private void OnComponentStartup(Entity<CrawlingComponent> ent, ref ComponentStartup args)
    {
        _speed.RefreshMovementSpeedModifiers(ent);
    }

    private void OnComponentShutdown(Entity<CrawlingComponent> ent, ref ComponentShutdown args)
    {
        _standing.Stand(ent);
        _speed.RefreshMovementSpeedModifiers(ent);
    }

    private void OnStandingUpDoAfter(EntityUid uid, StandingStateComponent component, CrawlingStandUpDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (_standing.Stand(uid))
        {
            _speed.RefreshMovementSpeedModifiers(uid);
            args.Handled = true;
        }
    }

    private void OnRefreshMovementSpeed(Entity<CrawlingComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (_standing.IsDown(ent))
        {
            args.ModifySpeed(ent.Comp.CrawlingSpeedModifier, ent.Comp.CrawlingSpeedModifier);
        }
    }

    private void OnBodyPartRemoved(Entity<CrawlingComponent> ent, ref BodyPartRemovedEvent args)
    {
        if (args.Part.Comp.PartType == BodyPartType.Leg)
        {
            if (!TryComp<BodyComponent>(ent, out var body) || body.LegEntities.Count < body.RequiredLegs)
                TryCrawl(ent, ent.Comp);
        }
    }

    private void OnBuckleAttempt(Entity<CrawlingComponent> ent, ref BuckleAttemptEvent args)
    {
        if (ent.Comp.IsCrawling && !HasComp<OperatingTableComponent>(args.Strap))
            args.Cancelled = true;
    }

    private void OnUnbuckled(Entity<CrawlingComponent> ent, ref UnbuckledEvent args)
    {
        if (TryComp<BodyComponent>(ent, out var body) && body.LegEntities.Count < body.RequiredLegs)
            TryCrawl(ent, ent.Comp);
    }

    private void HandleToggleCrawling(ICommonSession? session)
    {
        if (session?.AttachedEntity is not { Valid: true } uid)
            return;

        ToggleCrawlingState(uid);
    }

    public bool TryStandUp(EntityUid uid, CrawlingComponent? crawling = null, StandingStateComponent? standing = null)
    {
        if (!Resolve(uid, ref standing, false) || !Resolve(uid, ref crawling, false))
            return false;

        if (!_standing.IsDown(uid, standing) || !_mobState.IsAlive(uid) || TerminatingOrDeleted(uid) ||
            !TryComp<BodyComponent>(uid, out var body) || body.LegEntities.Count < body.RequiredLegs)
            return false;

        var args = new DoAfterArgs(EntityManager, uid, crawling.StandUpTime, new CrawlingStandUpDoAfterEvent(), uid)
        {
            BreakOnHandChange = false,
            RequireCanInteract = false
        };

        if (!_doAfter.TryStartDoAfter(args))
            return false;

        Dirty(uid, standing);
        Dirty(uid, crawling);

        return true;
    }

    public bool TryCrawl(EntityUid uid, CrawlingComponent? crawling = null, StandingStateComponent? standing = null)
    {
        if (!Resolve(uid, ref standing, false) || !Resolve(uid, ref crawling, false) || _standing.IsDown(uid, standing))
            return false;

        crawling.IsCrawling = true;

        return _standing.Down(uid, standingState: standing);
    }

    private void ToggleCrawlingState(EntityUid uid, CrawlingComponent? crawling = null, StandingStateComponent? standing = null)
    {
        if (!Resolve(uid, ref standing, false) || !Resolve(uid, ref crawling, false))
            return;

        if (!_mobState.IsAlive(uid) && !_mobState.IsPreCritical(uid) || HasComp<BeingCarriedComponent>(uid))
            return;

        if (TryComp<BuckleComponent>(uid, out var buckle) && buckle.BuckledTo != null)
            return;

        if (_standing.IsDown(uid, standing))
            TryStandUp(uid, crawling, standing);
        else
        {
            if (TryCrawl(uid, crawling, standing))
                _speed.RefreshMovementSpeedModifiers(uid);
        }
    }
}
