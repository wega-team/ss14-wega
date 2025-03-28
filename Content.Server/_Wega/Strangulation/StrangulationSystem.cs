using Content.Server.Body.Components;
using Content.Shared.DoAfter;
using Content.Shared.Verbs;
using Content.Shared.Damage;
using Content.Shared.Strangulation;
using Content.Shared.Mobs.Systems;
using Content.Shared.Hands.Components;
using Content.Shared.Popups;
using Content.Server.Chat.Systems;
using System;
using Robust.Shared.Timing;
using System.Threading;
using Content.Shared.NullRod.Components;
using Content.Shared.Garrotte;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Content.Server.Inventory;
using Content.Server.Carrying;
using Content.Shared.Hands;
using Content.Shared.Throwing;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.ActionBlocker;


/// Система пока сырая. Рабочий, но надо много еще сделать: ограничения "душителя" и "жертвы", базовые ограничения
/// Есть несколько неиспользуемых методов - остатки от того, как я пытался реализовать. Может быть понадобятся
/// По идее надо было связать с компонентом <see cref="RespiratorComponent"/>, но пока не сделано
/// DoAfter - вроде ок

namespace Content.Server.Strangulation
{
    public sealed class StrangulationSystem : EntitySystem
    {
        [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
        [Dependency] private readonly DamageableSystem _damageableSys = default!;
        [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
        [Dependency] private readonly ChatSystem _chat = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly VirtualItemSystem _virtualItemSystem = default!;
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;

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
            //Strangle(args.User, target, component);

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

            if (!CheckGarrotte(strangler) && hands.CountFreeHands() < 2)
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
                BreakOnMove = true,
                BreakOnDamage = true,
                MovementThreshold = 0.01f,
                DistanceThreshold = 0.5f,
                NeedHand = true,
                BreakOnHandChange = true,
                BreakOnDropItem = true,
                RequireCanInteract = true
            };
            _doAfterSystem.TryStartDoAfter(doAfterEventArgs);
            Strangle(strangler, target, component);
        }

        private void Strangle(EntityUid strangler, EntityUid target, RespiratorComponent component)
        {
            EnsureComp<StrangulationComponent>(target);
            _virtualItemSystem.TrySpawnVirtualItemInHand(target, strangler);
            _virtualItemSystem.TrySpawnVirtualItemInHand(target, strangler);
            _actionBlockerSystem.UpdateCanMove(target);
        }

        private void StopStrangle(EntityUid strangler, EntityUid target)
        {
            RemComp<StrangulationComponent>(target);
            _virtualItemSystem.DeleteInHandsMatching(strangler, target);
            _actionBlockerSystem.UpdateCanMove(target);
        }

        /*private bool CheckGarrotte(EntityUid strangler, StrangulationComponent component)
        {
            if (TryComp<HandsComponent>(strangler, out var hands))
            {
                foreach (var hand in hands.Hands.Values)
                {
                    if (hand.HeldEntity is not EntityUid heldEntity)
                        continue;

                    if (TryComp<GarrotteComponent>(heldEntity, out var garrotteComp))
                    {
                        component.IsStrangledGarrotte = true;

                        return true;
                    }
                }
            }
            return false;
        }*/

        private bool CheckGarrotte(EntityUid strangler)
        {
            if (TryComp<HandsComponent>(strangler, out var hands))
            {
                foreach (var hand in hands.Hands.Values)
                {
                    if (hand.HeldEntity is not EntityUid heldEntity)
                        continue;

                    if (TryComp<GarrotteComponent>(heldEntity, out var garrotteComp))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
