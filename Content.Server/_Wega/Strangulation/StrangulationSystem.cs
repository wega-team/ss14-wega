using Content.Server.Body.Components;
using Content.Shared.DoAfter;
using Content.Shared.Verbs;
using Content.Shared.Strangulation;
using Content.Shared.Mobs.Systems;
using Content.Shared.Hands;
using Content.Shared.Popups;
using Content.Shared.Garrotte;
using Content.Server.Inventory;
using Content.Shared.Throwing;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Standing;
using Content.Shared.Alert;
using Content.Shared.Speech.EntitySystems;
using Content.Shared.CombatMode;
using Robust.Shared.Input.Binding;
using Content.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Content.Shared.IdentityManagement;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Speech.Muting;
using Content.Shared.StatusEffectNew;

namespace Content.Server.Strangulation
{
    public sealed class StrangulationSystem : EntitySystem
    {
        [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
        [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
        [Dependency] private readonly VirtualItemSystem _virtualItemSystem = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;
        [Dependency] private readonly SharedHandsSystem _hands = default!;
        [Dependency] private readonly PullingSystem _pulling = default!;
        [Dependency] private readonly AlertsSystem _alerts = default!;
        [Dependency] private readonly SharedStutteringSystem _stutteringSystem = default!;
        [Dependency] private readonly SharedCombatModeSystem _combatModeSystem = default!;
        [Dependency] private readonly StatusEffectsSystem _statusEffect = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<RespiratorComponent, GetVerbsEvent<AlternativeVerb>>(AddStrangleVerb);
            SubscribeLocalEvent<RespiratorComponent, StrangulationDelayDoAfterEvent>(StrangleDelayDoAfter);
            SubscribeLocalEvent<RespiratorComponent, StrangulationDoAfterEvent>(StrangleDoAfter);
            SubscribeLocalEvent<StrangulationComponent, BreakFreeDoAfterEvent>(BreakFreeDoAfter);
            SubscribeLocalEvent<StrangulationComponent, VirtualItemDeletedEvent>(OnVirtualItemDeleted);
            SubscribeLocalEvent<StrangulationComponent, BeforeThrowEvent>(OnThrow);
            SubscribeLocalEvent<GarrotteComponent, GotUnequippedHandEvent>(OnThrowGarrotte);
            SubscribeLocalEvent<StrangulationComponent, BreakFreeStrangleAlertEvent>(OnBreakFreeStrangleAlert);

            CommandBinds.Builder
                .Bind(ContentKeyFunctions.Strangle, new PointerInputCmdHandler(BindStrangle))
                .Register<StrangulationSystem>();
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQueryEnumerator<StrangulationComponent>();
            while (query.MoveNext(out var uid, out _))
            {
                _stutteringSystem.DoStutter(uid, TimeSpan.FromSeconds(5), refresh: true);
            }
        }

        private void AddStrangleVerb(EntityUid uid, RespiratorComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanInteract || !args.CanAccess)
                return;
            // User = strangler, target = target, uid = target
            if (!CanStrangle(args.User, uid, component))
                return;

            AlternativeVerb verb = new()
            {
                Act = () =>
                {
                    TryStartStrangle(args.User, args.Target);
                },
                Text = Loc.GetString("strangle-verb"),

            };
            args.Verbs.Add(verb);
        }

        private void StrangleDelayDoAfter(EntityUid strangler, RespiratorComponent component, ref StrangulationDelayDoAfterEvent args)
        {
            if (args.Handled)
                return;

            var target = args.Target ?? default;

            if (args.Cancelled)
                return;

            StartStrangleDoAfter(args.User, target);
            args.Handled = true;
        }

        private void StrangleDoAfter(EntityUid strangler, RespiratorComponent component, ref StrangulationDoAfterEvent args)
        {
            if (args.Handled)
                return;

            var target = args.Target ?? default;
            _statusEffect.TryAddStatusEffectDuration(target, "Muted", TimeSpan.FromSeconds(3f));

            if (args.Cancelled)
            {
                StopStrangle(strangler, target);
                return;
            }
            args.Handled = true;
            args.Repeat = true;
        }

        private void BreakFreeDoAfter(EntityUid target, StrangulationComponent component, ref BreakFreeDoAfterEvent args)
        {
            if (args.Handled)
                return;

            if (args.DoAfter.Completed)
            {
                _doAfterSystem.Cancel(component.DoAfterId);
                return;
            }
            args.Handled = true;
        }

        private void OnVirtualItemDeleted(EntityUid uid, StrangulationComponent component, VirtualItemDeletedEvent args)
        {
            if (!HasComp<StrangulationComponent>(args.BlockingEntity))
                return;
            StopStrangle(args.User, args.BlockingEntity);
        }

        private void OnThrow(EntityUid uid, StrangulationComponent component, BeforeThrowEvent args)
        {
            if (!TryComp<VirtualItemComponent>(args.ItemUid, out var virtItem))
                return;

            StopStrangle(uid, args.ItemUid);
        }

        private void OnThrowGarrotte(Entity<GarrotteComponent> garrotte, ref GotUnequippedHandEvent args)
        {
            _doAfterSystem.Cancel(garrotte.Comp.DoAfterId);
            garrotte.Comp.DoAfterId = null;
        }

        private void OnBreakFreeStrangleAlert(EntityUid uid, StrangulationComponent component, ref BreakFreeStrangleAlertEvent args)
        {
            if (args.Handled)
                return;

            StartBreakFreeDoAfter(uid, component);
            args.Handled = true;
        }

        private bool BindStrangle(ICommonSession? playerSession, EntityCoordinates coordinates, EntityUid target)
        {
            var strangler = playerSession?.AttachedEntity ?? default;

            if (!CanStrangle(strangler, target))
                return false;

            TryStartStrangle(strangler, target);
            return false;
        }

        private void TryStartStrangle(EntityUid strangler, EntityUid target)
        {
            if (CheckGarrotte(strangler, out _))
                StartStrangleDoAfter(strangler, target);
            else
                StartStrangulationDelayDoAfter(strangler, target); //задержка перед удушением руками
        }

        private bool CanStrangle(EntityUid strangler, EntityUid target, RespiratorComponent? component = null)
        {
            if (!Resolve(target, ref component, false))
                return false;

            if (HasComp<StrangulationComponent>(target)) //чтобы удушение не мог начать второй душитель во время идущего процесса
                return false;

            if (!_mobStateSystem.IsAlive(strangler))
                return false;

            if (!CheckDistance(strangler, target))
                return false;

            TryComp<StranglerComponent>(target, out var stranglerComp); //чтобы цель не могла начать душить душителя...
            if (stranglerComp != null && strangler == stranglerComp.Target)
                return false;

            if (!CheckGarrotte(strangler, out _) && _hands.CountFreeHands(strangler) <= 1)
                return false;

            return true;
        }

        private void StartStrangulationDelayDoAfter(EntityUid strangler, EntityUid target) //задержка перед удушением руками
        {
            if (strangler == target)
                _popupSystem.PopupEntity(Loc.GetString("strangle-delay-start-self"), target, target, PopupType.Medium);
            else
                _popupSystem.PopupEntity(Loc.GetString("strangle-delay-start"), target, target, PopupType.LargeCaution);
            var doAfterDelay = TimeSpan.FromSeconds(1.5);
            var doAfterEventArgs = new DoAfterArgs(EntityManager, strangler, doAfterDelay,
                new StrangulationDelayDoAfterEvent(),
                eventTarget: strangler,
                target: target,
                used: target)
            {
                BreakOnMove = true,
                NeedHand = true
            };
            _doAfterSystem.TryStartDoAfter(doAfterEventArgs, out var doAfterId);
        }

        private void StartStrangleDoAfter(EntityUid strangler, EntityUid target)
        {
            if (strangler == target)
            {
                _popupSystem.PopupEntity(Loc.GetString("strangle-start-self-internal"),
                    target, target, PopupType.Medium);
                _popupSystem.PopupEntity(Loc.GetString("strangle-start-self-external", ("target", Identity.Entity(target, EntityManager))),
                    strangler, Filter.PvsExcept(strangler), true, PopupType.Medium);
            }
            else
            {
                _popupSystem.PopupEntity(Loc.GetString("strangle-start-internal"),
                    target, target, PopupType.LargeCaution);
                _popupSystem.PopupEntity(Loc.GetString("strangle-start-external", ("strangler", Identity.Entity(strangler, EntityManager)), ("target", Identity.Entity(target, EntityManager))),
                    target, Filter.PvsExcept(target), true, PopupType.MediumCaution);
            }
            var doAfterDelay = TimeSpan.FromSeconds(3);
            var doAfterEventArgs = new DoAfterArgs(EntityManager, strangler, doAfterDelay,
                new StrangulationDoAfterEvent(),
                eventTarget: strangler,
                target: target,
                used: target)
            {
                MovementThreshold = 0.02f,
                RequireCanInteract = true
            };
            _doAfterSystem.TryStartDoAfter(doAfterEventArgs, out var doAfterId);
            Strangle(strangler, target, doAfterId);
        }

        private void StartBreakFreeDoAfter(EntityUid user, StrangulationComponent component)
        {
            _popupSystem.PopupEntity(Loc.GetString("strangle-break-free", ("name", user)), user, PopupType.Medium);
            var doAfterDelay = component.IsStrangledGarrotte == false ? TimeSpan.FromSeconds(5) : TimeSpan.FromSeconds(15);
            var doAfterEventArgs = new DoAfterArgs(EntityManager, user, doAfterDelay,
                new BreakFreeDoAfterEvent(),
                eventTarget: user)
            {
                MovementThreshold = 0.02f,
                NeedHand = true,
                RequireCanInteract = true
            };
            _doAfterSystem.TryStartDoAfter(doAfterEventArgs, out var doAfterId);
            component.BreakFreeDoAfterId = doAfterId;
        }

        private void Strangle(EntityUid strangler, EntityUid target, DoAfterId? doAfterId)
        {
            EnsureComp<StranglerComponent>(strangler, out var stranglerComp);
            EnsureComp<StrangulationComponent>(target, out var strangulationComp);
            stranglerComp.Target = target;
            strangulationComp.DoAfterId = doAfterId;
            strangulationComp.Strangler = strangler;
            if (CheckGarrotte(strangler, out var garrotteComp))
            {
                strangulationComp.IsStrangledGarrotte = true;
                if (garrotteComp != null)
                {
                    strangulationComp.Damage = garrotteComp.GarrotteDamage;
                    garrotteComp.DoAfterId = doAfterId;
                }
            }
            if (target != strangler) //чтобы гаррота не выбрасывалась из рук, если душишь себя
            {
                var dropEvent = new DropHandItemsEvent();
                RaiseLocalEvent(target, ref dropEvent);
            }
            _combatModeSystem.SetDisarmFailChance(target, 0.9f); //баланс: чтобы жертва не могла сразу же выбраться из удушения
            _pulling.TryStartPull(strangler, target); //интересно же утащить жертву в техи
            _virtualItemSystem.TrySpawnVirtualItemInHand(target, strangler);
            _virtualItemSystem.TrySpawnVirtualItemInHand(target, strangler);
            _alerts.ShowAlert(target, strangulationComp.StrangledAlert);
        }

        private void StopStrangle(EntityUid strangler, EntityUid target)
        {
            var comp = Comp<StrangulationComponent>(target);
            _doAfterSystem.Cancel(comp.DoAfterId);
            _doAfterSystem.Cancel(comp.BreakFreeDoAfterId);
            comp.Cancelled = true;
            _alerts.ClearAlert(target, comp.StrangledAlert);
            _stutteringSystem.DoRemoveStutterTime(target, TimeSpan.FromSeconds(5));
            _combatModeSystem.SetDisarmFailChance(target, 0.75f);
            RemComp<StranglerComponent>(strangler);
            RemComp<StrangulationComponent>(target);
            _virtualItemSystem.DeleteInHandsMatching(strangler, target);
        }

        private bool CheckGarrotte(EntityUid strangler, out GarrotteComponent? garrotteComp)
        {
            var heldEntity = _hands.GetActiveItem(strangler);
            if (TryComp<GarrotteComponent>(heldEntity, out var comp))
            {
                garrotteComp = comp;
                return true;
            }

            garrotteComp = null;
            return false;
        }

        private bool CheckDistance(EntityUid strangler, EntityUid target)
        {
            var stranglerPosition = _transform.GetWorldPosition(strangler);
            var targetPosition = _transform.GetWorldPosition(target);
            var distance = (stranglerPosition - targetPosition).Length();
            if (distance > 0.7f)
                return false;
            return true;
        }
    }
}
