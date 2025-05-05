using Content.Server.Body.Components;
using Content.Shared.DoAfter;
using Content.Shared.Verbs;
using Content.Shared.Strangulation;
using Content.Shared.Mobs.Systems;
using Content.Shared.Hands.Components;
using Content.Shared.Popups;
using Content.Shared.Garrotte;
using Content.Server.Inventory;
using Content.Shared.Hands;
using Content.Shared.Throwing;
using Content.Shared.Inventory.VirtualItem;

namespace Content.Server.Strangulation
{
    public sealed class StrangulationSystem : EntitySystem
    {
        [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
        [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
        [Dependency] private readonly VirtualItemSystem _virtualItemSystem = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<RespiratorComponent, GetVerbsEvent<AlternativeVerb>>(AddStrangleVerb);
            SubscribeLocalEvent<RespiratorComponent, StrangulationDoAfterEvent>(StrangleDoAfter);
            SubscribeLocalEvent<StrangulationComponent, VirtualItemDeletedEvent>(OnVirtualItemDeleted);
            SubscribeLocalEvent<RespiratorComponent, BeforeThrowEvent>(OnThrow);
        }

        private void AddStrangleVerb(EntityUid uid, RespiratorComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanInteract || !args.CanAccess)
                return;

            if (!CanStrangle(args.User, uid, component))
                return;

            if (!_mobStateSystem.IsAlive(args.User))
                return;

            if (!CheckDistance(args.User, uid))
                return;

            AlternativeVerb verb = new()
            {
                Act = () =>
                {
                    StartStrangleDoAfter(args.User, uid, component);
                },
                Text = Loc.GetString("strangle-verb"),

            };
            args.Verbs.Add(verb);
        }

        private void StrangleDoAfter(EntityUid strangler, RespiratorComponent component, ref StrangulationDoAfterEvent args)
        {
            if (args.Handled)
                return;

            var target = args.Target ?? default;

            if (args.Cancelled)
            {
                StopStrangle(strangler, target);
                return;
            }
            args.Handled = true;
            args.Repeat = true;
        }

        private void OnVirtualItemDeleted(EntityUid uid, StrangulationComponent component, VirtualItemDeletedEvent args)
        {
            if (!HasComp<StrangulationComponent>(args.BlockingEntity))
                return;
            StopStrangle(args.User, args.BlockingEntity);
        }

        private void OnThrow(EntityUid uid, RespiratorComponent component, BeforeThrowEvent args)
        {
            if (!TryComp<VirtualItemComponent>(args.ItemUid, out var virtItem))
                return;

            StopStrangle(uid, args.ItemUid);
        }

        private bool CanStrangle(EntityUid strangler, EntityUid target, RespiratorComponent? component = null)
        {
            if (!Resolve(target, ref component, false))
                return false;

            if (HasComp<StrangulationComponent>(target)) //чтобы удушение не мог начать второй душитель во время идущего процесса
                return false;

            if (!TryComp<HandsComponent>(strangler, out var hands))
                return false;

            if (!CheckGarrotte(strangler, out var garrotteComp) && hands.CountFreeHands() < 2)
            {
                return false;
            }

            return true;
        }

        private void StartStrangleDoAfter(EntityUid strangler, EntityUid target, RespiratorComponent component)
        {
            _popupSystem.PopupEntity(Loc.GetString("strangle-start"), target, target, PopupType.LargeCaution);
            var doAfterDelay = TimeSpan.FromSeconds(3);
            var doAfterEventArgs = new DoAfterArgs(EntityManager, strangler, doAfterDelay,
                new StrangulationDoAfterEvent(),
                eventTarget: strangler,
                target: target,
                used: target)
            {
                //BreakOnMove = true,
                BreakOnDamage = true,
                MovementThreshold = 0.02f,
                //DistanceThreshold = 1f,
                NeedHand = true,
                BreakOnHandChange = true,
                BreakOnDropItem = true,
                RequireCanInteract = true
            };
            _doAfterSystem.TryStartDoAfter(doAfterEventArgs, out var doAfterId);
            Strangle(strangler, target, doAfterId);
        }

        private void Strangle(EntityUid strangler, EntityUid target, DoAfterId? DoAfterId)
        {
            EnsureComp<StrangulationComponent>(target, out var comp);
            comp.DoAfterId = DoAfterId;
            if (CheckGarrotte(strangler, out var garrotteComp))
            {
                comp.IsStrangledGarrotte = true;
                if (garrotteComp != null)
                    comp.Damage = garrotteComp.GarrotteDamage;
            }
            _virtualItemSystem.TrySpawnVirtualItemInHand(target, strangler);
            _virtualItemSystem.TrySpawnVirtualItemInHand(target, strangler);
        }

        private void StopStrangle(EntityUid strangler, EntityUid target)
        {
            var comp = Comp<StrangulationComponent>(target);
            _doAfterSystem.Cancel(comp.DoAfterId);
            comp.Cancelled = true;
            RemComp<StrangulationComponent>(target);
            _virtualItemSystem.DeleteInHandsMatching(strangler, target);
        }

        private bool CheckGarrotte(EntityUid strangler, out GarrotteComponent? garrotteComp)
        {
            if (TryComp<HandsComponent>(strangler, out var hands))
            {
                foreach (var hand in hands.Hands.Values)
                {
                    if (hand.HeldEntity is not EntityUid heldEntity)
                        continue;

                    if (TryComp<GarrotteComponent>(heldEntity, out var comp))
                    {
                        garrotteComp = comp;
                        return true;
                    }
                }
            }
            garrotteComp = null;
            return false;
        }

        private bool CheckDistance(EntityUid strangler, EntityUid target)
        {
            var stranglerPosition = _transform.GetWorldPosition(strangler);
            var targetPosition = _transform.GetWorldPosition(target);
            var distance = (stranglerPosition - targetPosition).Length();
            if (distance > 1f)
                return false;
            return true;
        }
    }
}
