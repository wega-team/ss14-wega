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

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<RespiratorComponent, GetVerbsEvent<AlternativeVerb>>(AddStrangleVerb);
            SubscribeLocalEvent<RespiratorComponent, StrangulationDoAfterEvent>(StrangleDoAfter);
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

        private bool CanStrangle(EntityUid strangler, EntityUid target, RespiratorComponent? component = null)
        {
            if (!Resolve(target, ref component, false))
                return false;

            if (HasComp<StrangulationComponent>(target))
                return false;

            if (!TryComp<HandsComponent>(strangler, out var hands))
                return false;

            if (hands.CountFreeHands() < 2)
                return false;

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
                NeedHand = true
            };
            _doAfterSystem.TryStartDoAfter(doAfterEventArgs);
        }

        private void StrangleDoAfter(EntityUid strangler, RespiratorComponent component, ref StrangulationDoAfterEvent args)
        {
            if (args.Handled)
                return;

            var target = args.Target ?? default;
            Strangle(args.User, target, component);

            if (args.Cancelled)
            {
                RemComp<StrangulationComponent>(target);
                return;
            }

            args.Handled = true;
            args.Repeat = true;
        }

        //пока метод-пустышка
        public bool IsStrangled(StrangulationComponent component)
        {
            if (!component.IsStrangled)
                return false;
            return true;
        }

        private void Strangle(EntityUid strangler, EntityUid target, RespiratorComponent component)
        {
            EnsureComp<StrangulationComponent>(target);
            /*if (_gameTiming.CurTime >= component.LastGaspEmoteTime + component.GaspEmoteCooldown)
            {
                component.LastGaspEmoteTime = _gameTiming.CurTime;
                _chat.TryEmoteWithChat(target, "Gasp", ChatTransmitRange.HideChat, ignoreActionBlocker: true);
            }

            DamageSpecifier damage = new DamageSpecifier { DamageDict = { { "Asphyxiation", 5 } } };
            _damageableSys.TryChangeDamage(target, damage, false);*/
        }
    }
}
