using System.Linq;
using Content.Shared.DoAfter;
using Content.Shared.ERP.Components;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Server.Popups;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Player;

namespace Content.Server.SexToy.System
{
    public class SexToyUsageSystem : EntitySystem
    {
        [Dependency] private readonly IEntityManager _entManager = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
        [Dependency] private readonly InventorySystem _inventorySystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SexToyComponent, AfterInteractEvent>(OnInteract);
            SubscribeLocalEvent<SexToyComponent, SexToyDoAfterEvent>(OnDoAfter);
        }

        private void OnInteract(Entity<SexToyComponent> entity, ref AfterInteractEvent args)
        {
            if (args.Handled)
                return;

            var user = args.User;

            if (!args.CanReach || args.Target is not { Valid: true } target)
                return;

            StartDoAfter(entity.Owner, user, target);
            args.Handled = true;
        }

        private void StartDoAfter(EntityUid toyEntity, EntityUid user, EntityUid target)
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

            var doAfterEventArgs = new DoAfterArgs(EntityManager, user, 3f, new SexToyDoAfterEvent(), toyEntity, target: target)
            {
                BreakOnMove = true,
                BreakOnDamage = true,
                NeedHand = true,
            };

            _doAfter.TryStartDoAfter(doAfterEventArgs);
        }

        private void OnDoAfter(Entity<SexToyComponent> entity, ref SexToyDoAfterEvent args)
        {
            if (args.Handled || args.Cancelled)
                return;

            args.Handled = true;

            UseSexToy(entity.Owner, args.Args.User, args.Args.Target);
        }

        private void UseSexToy(EntityUid toyEntity, EntityUid user, EntityUid? target)
        {
            if (!_entManager.TryGetComponent<SexToyComponent>(toyEntity, out var SexToyComponent))
                return;

            var noHumanoidMessage = Loc.GetString("interaction-impossible");
            var invalidGenderMessage = Loc.GetString("interaction-cant-do-this");
            var invalidSpeciesMessage = Loc.GetString("interaction-no-race");

            string userName = "";
            if (_entManager.TryGetComponent<MetaDataComponent>(user, out var metaDataComponent))
                userName = metaDataComponent.EntityName;

            if (target != null && _entManager.TryGetComponent<HumanoidAppearanceComponent>((EntityUid)target, out var targetAppearance))
            {
                var isValid = true;
                string messageUser = "";
                string messageTarget = "";

                var random = new Random();

                var dildoUserMessagesVox = new[]
                {
                    Loc.GetString("interaction-dildo-user-vox-1"),
                    Loc.GetString("interaction-dildo-user-vox-2"),
                    Loc.GetString("interaction-dildo-user-vox-3")
                };

                var dildoTargetMessagesVox = new[]
                {
                    Loc.GetString("interaction-dildo-target-vox-1", ("user", userName)),
                    Loc.GetString("interaction-dildo-target-vox-2", ("user", userName)),
                    Loc.GetString("interaction-dildo-target-vox-3", ("user", userName))
                };

                var dildoUserMessagesMale = new[]
                {
                    Loc.GetString("interaction-dildo-user-anal-1"),
                    Loc.GetString("interaction-dildo-user-anal-2"),
                    Loc.GetString("interaction-dildo-user-anal-3")
                };

                var dildoTargetMessagesMale = new[]
                {
                    Loc.GetString("interaction-dildo-target-anal-1", ("user", userName)),
                    Loc.GetString("interaction-dildo-target-anal-2", ("user", userName)),
                    Loc.GetString("interaction-dildo-target-anal-3", ("user", userName))
                };

                var dildoUserMessagesFemale = new[]
                {
                    Loc.GetString("interaction-dildo-user-anal-1"),
                    Loc.GetString("interaction-dildo-user-anal-2"),
                    Loc.GetString("interaction-dildo-user-anal-3"),
                    Loc.GetString("interaction-dildo-user-vagina-1"),
                    Loc.GetString("interaction-dildo-user-vagina-2")
                };

                var dildoTargetMessagesFemale = new[]
                {
                    Loc.GetString("interaction-dildo-target-anal-1", ("user", userName)),
                    Loc.GetString("interaction-dildo-target-anal-2", ("user", userName)),
                    Loc.GetString("interaction-dildo-target-anal-3", ("user", userName)),
                    Loc.GetString("interaction-dildo-target-vagina-1", ("user", userName)),
                    Loc.GetString("interaction-dildo-target-vagina-2", ("user", userName))
                };

                switch (SexToyComponent.Prototype.FirstOrDefault())
                {
                    case "dildo":
                        if (targetAppearance.Species == "Vox")
                        {
                            messageUser = dildoUserMessagesVox[0];
                            messageTarget = dildoTargetMessagesVox[0];
                        }
                        else if (targetAppearance.Sex == Sex.Male)
                        {
                            messageUser = dildoUserMessagesMale[0];
                            messageTarget = dildoTargetMessagesMale[0];
                        }
                        else if (targetAppearance.Sex == Sex.Female)
                        {
                            messageUser = dildoUserMessagesFemale[0];
                            messageTarget = dildoTargetMessagesFemale[0];
                        }
                        else
                            isValid = false;
                        break;

                    case "fleshlight":
                        if (targetAppearance.Sex == Sex.Male)
                        {
                            messageUser = Loc.GetString("interaction-fleshlight");
                            messageTarget = Loc.GetString("interaction-fleshlight-target", ("user", userName));
                        }
                        else
                            isValid = false;
                        break;

                    case "condom":
                        if (targetAppearance.Sex == Sex.Male)
                        {
                            messageUser = Loc.GetString("interaction-condom");
                            messageTarget = Loc.GetString("interaction-condom-target", ("user", userName));
                            _entManager.DeleteEntity(toyEntity);
                        }
                        else
                            isValid = false;
                        break;

                    default:
                        messageUser = Loc.GetString("interaction-nothing");
                        break;
                }

                if (isValid)
                {
                    var species = targetAppearance.Species;

                    if (species == "Diona" || species == "Arachnid")
                    {
                        if (_entManager.TryGetComponent<ActorComponent>(user, out var actor))
                            _popupSystem.PopupEntity(invalidSpeciesMessage, user, actor.PlayerSession, PopupType.Small);
                        return;
                    }
                    else
                    {
                        if (_entManager.TryGetComponent<ActorComponent>(user, out var actor))
                            _popupSystem.PopupEntity(messageUser, user, actor.PlayerSession, PopupType.Small);

                        if (target != user && _entManager.TryGetComponent<ActorComponent>((EntityUid)target, out var targetActor))
                            _popupSystem.PopupEntity(messageTarget, (EntityUid)target, targetActor.PlayerSession, PopupType.Small);
                    }
                }
                else
                {
                    if (_entManager.TryGetComponent<ActorComponent>(user, out var actor))
                        _popupSystem.PopupEntity(invalidGenderMessage, user, actor.PlayerSession, PopupType.Small);
                    return;
                }
            }
            else
            {
                if (_entManager.TryGetComponent<ActorComponent>(user, out var actor))
                    _popupSystem.PopupEntity(noHumanoidMessage, user, actor.PlayerSession, PopupType.Small);
            }
        }
    }
}
