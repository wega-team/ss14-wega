using System.Linq;
using System.Text.RegularExpressions;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.ERP.Components;
using Content.Server.Chat.Systems;
using Content.Server.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Interaction.Panel
{
    public sealed class InteractionPanelSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IEntityManager _entManager = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly ChatSystem _chatSystem = default!;
        [Dependency] private readonly InventorySystem _inventorySystem = default!;

        private readonly Dictionary<NetEntity, DateTime> _lastInteractionTimes = new();
        private readonly Dictionary<NetEntity, int> _userPoints = new();

        public override void Initialize()
        {
            base.Initialize();
            SubscribeNetworkEvent<InteractionPressedEvent>(OnInteractionPressed);
        }

        private void OnInteractionPressed(InteractionPressedEvent ev)
        {
            HandleInteraction(ev.User, ev.Target, ev.InteractionId);
            HandlePoints(ev.User, ev.Target, ev.InteractionId);
        }

        public void HandleInteraction(NetEntity user, NetEntity? target, string interactionId)
        {
            if (target == null) return;

            var interactionPrototype = _prototypeManager.Index<InteractionPrototype>(interactionId);

            if (_lastInteractionTimes.TryGetValue(target.Value, out var lastInteractionTime))
            {
                if (DateTime.UtcNow - lastInteractionTime < interactionPrototype.UseDelay)
                {
                    var userEntity = _entManager.GetEntity(user);
                    var message = Loc.GetString("interaction-delay-message");

                    if (_entManager.TryGetComponent<ActorComponent>(userEntity, out var actor))
                        _popupSystem.PopupEntity(message, userEntity, actor.PlayerSession, PopupType.Small);
                    return;
                }
            }

            _lastInteractionTimes[target.Value] = DateTime.UtcNow;

            if (interactionPrototype.RequiredClothingSlots != null)
            {
                var userEntity = _entManager.GetEntity(user);
                if (TryComp<InventoryComponent>(userEntity, out var inventory))
                {
                    foreach (var slot in interactionPrototype.RequiredClothingSlots)
                    {
                        if (_inventorySystem.TryGetSlotEntity(userEntity, slot, out var slotEntity, inventory))
                        {
                            var message = Loc.GetString("interaction-hasclothing-message");
                            if (_entManager.TryGetComponent<ActorComponent>(userEntity, out var actor))
                                _popupSystem.PopupEntity(message, userEntity, actor.PlayerSession, PopupType.Small);
                            return;
                        }
                    }
                }

                var targetEntity = _entManager.GetEntity(target.Value);
                if (TryComp<InventoryComponent>(targetEntity, out var targetInventory))
                {
                    var requiredSlots = interactionPrototype.RequiredClothingSlots ?? Enumerable.Empty<string>();
                    var oneRequiredSlots = interactionPrototype.OneRequiredClothingSlots ?? Enumerable.Empty<string>();

                    var allSlots = requiredSlots.Concat(oneRequiredSlots);

                    foreach (var slot in allSlots)
                    {
                        if (_inventorySystem.TryGetSlotEntity(targetEntity, slot, out var slotEntity, targetInventory))
                        {
                            var targetEntityValue = _entManager.GetEntity(target.Value);
                            var messageForUser = Loc.GetString("interaction-target-hasclothing-message", ("target", Identity.Entity(targetEntityValue, _entManager)));

                            if (_entManager.TryGetComponent<ActorComponent>(userEntity, out var actor))
                                _popupSystem.PopupEntity(messageForUser, userEntity, actor.PlayerSession, PopupType.Small);
                            return;
                        }
                    }
                }
            }

            bool hasStrapon = true;
            if (interactionPrototype.RequiresStrapon)
            {
                var userEntity = _entManager.GetEntity(user);
                if (_entManager.TryGetComponent<InventoryComponent>(userEntity, out var inventory))
                {
                    if (!_inventorySystem.TryGetSlotEntity(userEntity, "belt", out var beltEntity, inventory) ||
                        !_entManager.TryGetComponent<StraponComponent>(beltEntity, out _))
                        hasStrapon = false;
                }
                else
                    hasStrapon = false;
            }

            if (!hasStrapon)
            {
                var userEntity = _entManager.GetEntity(user);
                var message = Loc.GetString("interaction-missing-strapon-message");
                if (_entManager.TryGetComponent<ActorComponent>(userEntity, out var actor))
                    _popupSystem.PopupEntity(message, userEntity, actor.PlayerSession, PopupType.Small);
                return;
            }

            if (interactionPrototype.DoAfterDelay > 0f)
            {
                TriggerDoAfter(user, target.Value, interactionId, interactionPrototype.DoAfterDelay);
            }
            else
            {
                ExecuteInteraction(user, target.Value, interactionPrototype);
            }
        }

        private void TriggerDoAfter(NetEntity user, NetEntity target, string interactionId, float delay)
        {

        }

        private void ExecuteInteraction(NetEntity user, NetEntity target, InteractionPrototype interactionPrototype)
        {
            int preferredIndex = GetRandomMessageIndex(interactionPrototype);

            var userEntity = _entManager.GetEntity(user);
            var targetEntityValue = _entManager.GetEntity(target);

            if (interactionPrototype.TargetMessages.Count > 0)
            {
                if (preferredIndex < 0 || preferredIndex >= interactionPrototype.TargetMessages.Count)
                    preferredIndex = 0;

                var targetMessage = Loc.GetString(interactionPrototype.TargetMessages[preferredIndex], ("user", Identity.Entity(userEntity, _entManager)));
                var otherMessage = Loc.GetString(interactionPrototype.OtherMessages.Count > 0 ? interactionPrototype.OtherMessages[preferredIndex] : "",
                    ("user", Identity.Entity(userEntity, _entManager)), ("target", Identity.Entity(targetEntityValue, _entManager)));

                if (_entManager.TryGetComponent<ActorComponent>(targetEntityValue, out var actor))
                    _popupSystem.PopupEntity(targetMessage, targetEntityValue, actor.PlayerSession, PopupType.Small);

                var filter = Filter.Local()
                    .AddAllPlayers()
                    .RemoveWhereAttachedEntity(uid => uid == userEntity)
                    .RemoveWhereAttachedEntity(uid => uid == targetEntityValue);

                _popupSystem.PopupEntity(otherMessage, userEntity, filter, false, PopupType.Small);

                PlayInteractionSound(interactionPrototype.InteractSound, targetEntityValue, interactionPrototype.SoundPerceivedByOthers);
            }

            if (interactionPrototype.UserMessages.Count > 0)
            {
                if (preferredIndex < 0 || preferredIndex >= interactionPrototype.UserMessages.Count)
                    preferredIndex = 0;

                var emoteCommand = Loc.GetString(interactionPrototype.UserMessages[preferredIndex], ("target", Identity.Entity(targetEntityValue, _entManager)));

                if (_entManager.TryGetComponent<ActorComponent>(userEntity, out var userActor))
                {
                    var playerSession = userActor.PlayerSession;

                    _chatSystem.TrySendInGameICMessage(
                        source: userEntity,
                        message: emoteCommand,
                        desiredType: InGameICChatType.Emote,
                        range: ChatTransmitRange.Normal,
                        hideLog: false,
                        player: playerSession
                    );
                }
            }
        }

        private int GetRandomMessageIndex(InteractionPrototype interactionPrototype)
        {
            var numberSuffixes = new List<int>();
            var numberPattern = new Regex(@"-(\d+)$");

            var allMessages = interactionPrototype.UserMessages
                .Concat(interactionPrototype.TargetMessages)
                .Concat(interactionPrototype.OtherMessages)
                .ToList();

            foreach (var message in allMessages)
            {
                var match = numberPattern.Match(message);
                if (match.Success)
                {
                    if (int.TryParse(match.Groups[1].Value, out var number))
                        numberSuffixes.Add(number);
                }
            }

            if (numberSuffixes.Count > 0)
            {
                var random = new Random();
                var randomIndex = random.Next(numberSuffixes.Min(), numberSuffixes.Max() + 1);
                return randomIndex - 1;
            }
            else
            {
                return allMessages.Count > 0 ? new Random().Next(allMessages.Count) : 0;
            }
        }

        private void HandlePoints(NetEntity user, NetEntity? target, string interactionId)
        {
            if (target == null) return;

            var userEntity = _entManager.GetEntity(user);

            if (!_entManager.TryGetComponent<HumanoidAppearanceComponent>(userEntity, out var appearanceComponent))
                return;

            if (appearanceComponent.Sex != Sex.Male)
                return;

            var interactionPrototype = _prototypeManager.Index<InteractionPrototype>(interactionId);

            if (_lastInteractionTimes.TryGetValue(user, out var lastInteractionTime) &&
                DateTime.UtcNow - lastInteractionTime < interactionPrototype.UseDelay)
            {
                return;
            }

            _lastInteractionTimes[user] = DateTime.UtcNow;

            int currentPoints = _userPoints.TryGetValue(user, out var points) ? points : 0;
            int pointsToAdd = interactionPrototype.Points;
            currentPoints += pointsToAdd;

            _userPoints[user] = currentPoints;

            if (currentPoints >= 100)
            {
                _userPoints[user] = 0;
                TriggerFluidEvent(userEntity);
            }
        }

        private void TriggerFluidEvent(EntityUid userEntity)
        {
            var coordinates = _entManager.GetComponent<TransformComponent>(userEntity).Coordinates;
            var puddleEnt = Spawn("PuddleCum", coordinates);

            var sound = new SoundPathSpecifier("/Audio/Effects/Fluids/splat.ogg");
            _audio.PlayPvs(sound, puddleEnt);

            var message = Loc.GetString("interaction-end-message");

            if (_entManager.TryGetComponent<ActorComponent>(userEntity, out var actor))
            {
                var playerSession = actor.PlayerSession;

                _chatSystem.TrySendInGameICMessage(
                    source: userEntity,
                    message: message,
                    desiredType: InGameICChatType.Emote,
                    range: ChatTransmitRange.Normal,
                    hideLog: false,
                    player: playerSession
                );
            }
        }

        private void PlayInteractionSound(SoundSpecifier? sound, EntityUid target, bool perceivedByOthers)
        {
            if (sound == null) return;

            /// Пока оставить так, на случай "Возможных" изменений в будущем
            if (perceivedByOthers)
            {
                _audio.PlayPvs(sound, target);
            }
            else
            {
                _audio.PlayEntity(sound, Filter.Entities(target), target, false);
            }
        }
    }
}
