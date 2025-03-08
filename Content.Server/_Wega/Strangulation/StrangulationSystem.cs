using Content.Server.Body.Components;
using Content.Shared.DoAfter;
using Content.Shared.Verbs;
using Content.Shared.Damage;
using Content.Shared.Strangulation;
using Content.Shared.Mobs.Systems;
using Content.Shared.Hands.Components;
using Content.Shared.Popups;


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

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<RespiratorComponent, GetVerbsEvent<AlternativeVerb>>(AddStrangleVerb);
            SubscribeLocalEvent<RespiratorComponent, StrangulationActionEvent>(OnStrangle);  //пока пустышка
            SubscribeLocalEvent<RespiratorComponent, StrangulationDoAfterEvent>(StrangleDoAfter);
        }

        private void AddStrangleVerb(EntityUid uid, RespiratorComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanInteract || !args.CanAccess)
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

        //пока метод-пустышка
        private void OnStrangle(EntityUid strangler, RespiratorComponent component, StrangulationActionEvent args)
        {
            /*if (TryStrangle(strangler, component, args))
            {
                var doAfterDelay = TimeSpan.FromSeconds(3);
                var doAfterEventArgs = new DoAfterArgs(EntityManager, strangler, doAfterDelay,
                    new StrangulationDoAfterEvent(),
                    eventTarget: strangler,
                    target: args.Target,
                    used: args.Target)
                {
                    BreakOnMove = true,
                    BreakOnDamage = true,
                    MovementThreshold = 0.01f,
                    DistanceThreshold = 0.5f,
                    NeedHand = true
                };

                _doAfterSystem.TryStartDoAfter(doAfterEventArgs);
                _popupSystem.PopupEntity(Loc.GetString("strangle-start"), args.Target, args.Target);
            }*/
        }

        //пока метод-пустышка
        private bool TryStrangle(EntityUid strangler, RespiratorComponent component, StrangulationActionEvent args)
        {
            if (!_mobStateSystem.IsAlive(args.Target))
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
            if (args.Cancelled)
                return;

            // урон пока реализован так
            var damage = new DamageSpecifier { DamageDict = { { "Asphyxiation", 5 } } };
            _damageableSys.TryChangeDamage(args.Target, damage, false);

            args.Repeat = true;
        }

        //пока метод-пустышка
        public bool IsStrangled(StrangulationComponent component)
        {
            if (!component.IsStrangled)
                return false;
            return true;
        }

    }
}
