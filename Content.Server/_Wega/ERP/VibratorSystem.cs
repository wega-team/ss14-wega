using Content.Shared.DoAfter;
using Content.Shared.ERP.Components;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Popups;
using Content.Server.Popups;
using Robust.Shared.Player;

namespace Content.Server.Vibrator.System
{
    public sealed class VibratorUsageSystem : EntitySystem
    {
        [Dependency] private readonly IEntityManager _entManager = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
        [Dependency] private readonly InventorySystem _inventorySystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<VibratorComponent, AfterInteractEvent>(OnInteract);
            SubscribeLocalEvent<VibratorComponent, VibratorDoAfterEvent>(OnDoAfter);
        }

        private void OnInteract(Entity<VibratorComponent> entity, ref AfterInteractEvent args)
        {
            if (args.Handled)
                return;

            var user = args.User;

            if (!args.CanReach || args.Target is not { Valid: true } target)
                return;

            if (_entManager.TryGetComponent<ItemToggleComponent>(entity.Owner, out var toggle))
            {
                if (toggle.Activated)
                {
                    StartDoAfter(entity.Owner, user, target);
                }
                else
                {
                    var noToggleMessage = Loc.GetString("interaction-vibrator-off");
                    if (_entManager.TryGetComponent<ActorComponent>(user, out var actor))
                        _popupSystem.PopupEntity(noToggleMessage, user, actor.PlayerSession, PopupType.Small);
                }
            }

            args.Handled = true;
        }

        private void StartDoAfter(EntityUid vibratorEntity, EntityUid user, EntityUid target)
        {
            var requiredClothingSlots = new[] { "jumpsuit", "outerClothing", "underwearbottom" };

            if (TryComp<InventoryComponent>(target, out var inventory))
            {
                foreach (var slot in requiredClothingSlots)
                {
                    if (_inventorySystem.TryGetSlotEntity(target, slot, out var slotEntity, inventory))
                    {
                        var message = Loc.GetString("interaction-slot-occupied-message");
                        if (_entManager.TryGetComponent<ActorComponent>(user, out var actor))
                            _popupSystem.PopupEntity(message, user, actor.PlayerSession, PopupType.Small);
                        return;
                    }
                }
            }

            var doAfterEventArgs = new DoAfterArgs(EntityManager, user, 3f, new VibratorDoAfterEvent(), vibratorEntity, target: target)
            {
                BreakOnMove = true,
                BreakOnDamage = true,
                NeedHand = true,
            };

            _doAfter.TryStartDoAfter(doAfterEventArgs);
        }

        private void OnDoAfter(Entity<VibratorComponent> entity, ref VibratorDoAfterEvent args)
        {
            if (args.Handled || args.Cancelled)
                return;

            args.Handled = true;

            if (args.Args.Target is { Valid: true } target)
            {
                UseVibrator(entity.Owner, args.Args.User, target);
            }
        }

        private void UseVibrator(EntityUid vibratorEntity, EntityUid user, EntityUid target)
        {
            if (!_entManager.TryGetComponent<VibratorComponent>(vibratorEntity, out var vibratorComponent))
                return;

            string userName = "";
            if (_entManager.TryGetComponent<MetaDataComponent>(user, out var metaDataComponent))
                userName = metaDataComponent.EntityName;

            var noHumanoidMessage = Loc.GetString("interaction-impossible");
            var invalidGenderMessage = Loc.GetString("interaction-cant-do-this");
            var invalidSpeciesMessage = Loc.GetString("interaction-no-race");

            if (!_entManager.TryGetComponent<HumanoidAppearanceComponent>(target, out var targetAppearance))
            {
                if (_entManager.TryGetComponent<ActorComponent>(user, out var userActor))
                    _popupSystem.PopupEntity(noHumanoidMessage, user, userActor.PlayerSession, PopupType.Small);
                return;
            }

            string messageUser = "";
            string messageTarget = "";

            var random = new Random();

            var vibratorUserMessagesVox = new[]
            {
                Loc.GetString("interaction-vibrator-user-vox-1"),
                Loc.GetString("interaction-vibrator-user-vox-2"),
            };

            var vibratorTargetMessagesVox = new[]
            {
                Loc.GetString("interaction-vibrator-target-vox-1", ("user", userName)),
                Loc.GetString("interaction-vibrator-target-vox-2", ("user", userName)),
            };

            var vibratorUserMessagesMale = new[]
            {
                Loc.GetString("interaction-vibrator-user-anal-1"),
                Loc.GetString("interaction-vibrator-user-anal-2"),
                Loc.GetString("interaction-vibrator-user-dick-1"),
            };

            var vibratorTargetMessagesMale = new[]
            {
                Loc.GetString("interaction-vibrator-target-anal-1", ("user", userName)),
                Loc.GetString("interaction-vibrator-target-anal-2", ("user", userName)),
                Loc.GetString("interaction-vibrator-target-dick-1", ("user", userName)),
            };

            var vibratorUserMessagesFemale = new[]
            {
                Loc.GetString("interaction-vibrator-user-anal-1"),
                Loc.GetString("interaction-vibrator-user-anal-2"),
                Loc.GetString("interaction-vibrator-user-vagina-1"),
            };

            var vibratorTargetMessagesFemale = new[]
            {
                Loc.GetString("interaction-vibrator-target-anal-1", ("user", userName)),
                Loc.GetString("interaction-vibrator-target-anal-2", ("user", userName)),
                Loc.GetString("interaction-vibrator-target-vagina-1", ("user", userName)),
            };

            switch (targetAppearance.Species)
            {
                case "Vox":
                    messageUser = vibratorUserMessagesVox[random.Next(vibratorUserMessagesVox.Length)];
                    messageTarget = vibratorTargetMessagesVox[random.Next(vibratorTargetMessagesVox.Length)];
                    break;

                case "Diona":
                case "Arachnid":
                    if (_entManager.TryGetComponent<ActorComponent>(user, out var userActorSpecies))
                        _popupSystem.PopupEntity(invalidSpeciesMessage, user, userActorSpecies.PlayerSession, PopupType.Small);
                    return;

                default:
                    if (targetAppearance.Sex == Sex.Male)
                    {
                        messageUser = vibratorUserMessagesMale[random.Next(vibratorUserMessagesMale.Length)];
                        messageTarget = vibratorTargetMessagesMale[random.Next(vibratorTargetMessagesMale.Length)];
                    }
                    else if (targetAppearance.Sex == Sex.Female)
                    {
                        messageUser = vibratorUserMessagesFemale[random.Next(vibratorUserMessagesFemale.Length)];
                        messageTarget = vibratorTargetMessagesFemale[random.Next(vibratorTargetMessagesFemale.Length)];
                    }
                    else
                    {
                        if (_entManager.TryGetComponent<ActorComponent>(user, out var userActorGender))
                            _popupSystem.PopupEntity(invalidGenderMessage, user, userActorGender.PlayerSession, PopupType.Small);
                        return;
                    }
                    break;
            }

            if (_entManager.TryGetComponent<ActorComponent>(user, out var finalUserActor))
                _popupSystem.PopupEntity(messageUser, user, finalUserActor.PlayerSession, PopupType.Small);

            if (target != user && _entManager.TryGetComponent<ActorComponent>(target, out var finalTargetActor))
                _popupSystem.PopupEntity(messageTarget, target, finalTargetActor.PlayerSession, PopupType.Small);
        }
    }
}
