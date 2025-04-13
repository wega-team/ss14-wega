using System.Linq;
using Content.Server.Chat.Systems;
using Content.Server.NPC.HTN;
using Content.Server.Speech;
using Content.Server.Speech.Components;
using Content.Shared.Damage;
using Content.Shared.Xenobiology.Components;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Xenobiology;

public sealed class SlimeSocialSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly HTNSystem _htn = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SlimeHungerSystem _hunger = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private static readonly TimeSpan MinCommandInterval = TimeSpan.FromSeconds(6);

    private static readonly Dictionary<string, string[]> CommandResponses = new()
    {
        ["hello"] = new[] { "Буль-буль! Привет!", "Здравствуй, друг!", "Приветствую тебя!", "Привет, {0}!", "Рад тебя видеть!" },
        ["follow"] = new[] { "Иду за тобой!", "Как скажешь!", "Всегда за тобой!", "Веди, командир!", "Я рядом!" },
        ["stop"] = new[] { "Останавливаюсь...", "Хорошо, я подожду", "Как скажешь, лидер", "Не двигаться? Окей!", "Беру паузу." },
        ["stay"] = new[] { "Буду ждать...", "Не сдвинусь с места!", "Ожидание - не проблема", "Становлюсь камнем.", "Хорошо, постою!" },
        ["attack"] = new[] { "Атакую {0}!", "{0} будет уничтожен!", "Сейчас разберусь с {0}!", "Жертва обнаружена! {0} - берегись!", "Готов к атаке на {0}!" },
        ["mood"] = new[] { "Я чувствую себя {0}!", "Моё состояние: {0}!", "Сейчас я {0}!", "Настроение: {0}", "Чувствую {0}, а ты?" }
    };

    private static readonly Dictionary<string, string[]> RefuseResponses = new()
    {
        ["default"] = new[] { "Не хочу...", "Я занят...", "Может позже?", "Не сегодня, друг.", "Я пока отдохну." },
        ["attack_friend"] = new[] { "Не буду атаковать друга!", "Это же наш друг!", "Я не предатель!", "Друзей не кусаю!", "Не могу, он мне нравится!" },
        ["hungry"] = new[] { "Сначала покорми меня!", "Я слишком голоден!", "Еда важнее приказов!", "Покорми меня, и тогда поговорим!", "Голодный слайм - непослушный слайм." },
        ["angry"] = new[] { "Не хочу тебя слушать!", "Уйди, я зол!", "Не трогай меня!", "Ты мне не нравишься!", "Отстань!" }
    };

    private static readonly Dictionary<string, string[]> BetrayalResponses = new()
    {
        ["leader_betrayed"] = new[] { "{0} - предатель! Больше не мой лидер!", "Как ты мог, {0}?! Я больше не подчиняюсь!", "Лидеры так не поступают! Прощай, {0}!", "Буль-буль... Ты разбил моё сердце!", "Я думал, ты другой, {0}. Ошибался.", },
        ["friend_betrayed"] = new[] { "Буль... Как ты мог?", "Друзья так не поступают!", "Уходи, {0}! Ты предатель!", "Я верил тебе... Как же ты мог?", "Никогда не прощу этого, {0}!" },
        ["hurt_but_friends"] = new[] { "Зачем ты делаешь мне больно?", "Буль... Мне неприятно, {0}...", "Прекрати! Это больно!", "Буль-буль... Ты ранил меня...", "Я не ожидал такого от тебя, {0}..." },
        ["attack_enemy"] = new[] { "Атака! {0} будет уничтожен!", "Как посмел напасть на меня?", "Ты выбрал не того слайма!", "Буль-буль! В атаку на {0}!", "Получи!" }
    };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SlimeSocialComponent, MapInitEvent>(OnSlimeInit);
        SubscribeLocalEvent<SlimeSocialComponent, ListenEvent>(OnSlimeHear);

        SubscribeLocalEvent<SlimeSocialComponent, DamageChangedEvent>(OnSlimeDamaged);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<SlimeSocialComponent>();
        while (query.MoveNext(out var uid, out var social))
        {
            // Постепенная потеря дружбы
            social.FriendshipLevel = Math.Max(0, social.FriendshipLevel - social.FriendshipDecayRate * frameTime);

            // Проверка гнева
            if (social.AngryUntil.HasValue && _gameTiming.CurTime > social.AngryUntil.Value)
            {
                social.AngryUntil = null;
                _chat.TrySendInGameICMessage(uid,
                    "Я больше не злюсь...",
                    InGameICChatType.Speak, false);

                if (TryComp<HTNComponent>(uid, out var htn))
                {
                    ResetSlimeState(htn, true);
                }
            }

            // Автоматическая смена лидера если дружба упала
            if (social.Leader != null && social.FriendshipLevel < 50f)
            {
                _chat.TrySendInGameICMessage(uid, $"{social.Leader} больше не мой лидер!", InGameICChatType.Speak, false);
                social.Leader = null;
            }
        }
    }

    private void OnSlimeInit(EntityUid uid, SlimeSocialComponent component, MapInitEvent args)
    {
        EnsureComp<ActiveListenerComponent>(uid).Range = component.ListenRange;
    }

    public void TryBefriend(EntityUid slime, EntityUid potentialFriend, SlimeHungerComponent? hunger = null, SlimeSocialComponent? social = null)
    {
        if (!Resolve(slime, ref social, ref hunger))
            return;

        // Кормление увеличивает дружбу сильнее когда слайм голоден
        var feedBonus = social.FeedFriendshipBonus * (1 + (100 - hunger.Hunger) / 100f)
            * Math.Max(0.2f, 1 - social.TotalFeedings * 0.05f); // После 20 кормлений бонус падает до минимума (20%)

        social.TotalFeedings++;
        social.FriendshipLevel = Math.Min(150, social.FriendshipLevel + feedBonus);

        if (!social.Friends.Contains(potentialFriend))
        {
            social.Friends.Add(potentialFriend);
            _chat.TrySendInGameICMessage(slime,
                _random.Pick(new[] { "Новый друг!", "Приятно познакомиться!", "Будем дружить!" }),
                InGameICChatType.Speak, false);
        }

        social.FriendshipLevel = Math.Min(150, social.FriendshipLevel + feedBonus);

        // Проверка на лидера (требуется минимум 80 дружбы)
        if (social.FriendshipLevel >= 80f && social.Leader != potentialFriend)
        {
            social.Leader = potentialFriend;
            _chat.TrySendInGameICMessage(slime,
                _random.Pick(new[] { $"{potentialFriend} теперь мой лидер!", "Я буду слушаться тебя!", "Ты мой новый командир!" }),
                InGameICChatType.Speak, false);
        }
    }

    private void OnSlimeHear(EntityUid uid, SlimeSocialComponent component, ListenEvent args)
    {
        // Игнорируем не-игроков
        if (!TryComp<ActorComponent>(args.Source, out _))
            return;

        ProcessSlimeCommand(uid, component, args.Message, args.Source);
    }

    private void ProcessSlimeCommand(EntityUid slime, SlimeSocialComponent social, string message, EntityUid source)
    {
        message = message.Trim().ToLower();

        // Проверяем, обращается ли игрок к слайму
        if (!message.Contains("slime") &&
            !message.Contains("слайм") &&
            !int.TryParse(message.Split(' ')[0], out _))
        {
            return;
        }

        // Разделяем обращение и команду
        var commandParts = message.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries).Skip(1).ToArray();
        if (commandParts.Length == 0) return;

        var command = commandParts[0];
        var target = commandParts.Length > 1 ? string.Join(" ", commandParts.Skip(1)) : null;

        if (command != "hello" && command != "hi" && command != "stop" && command != "mood")
        {
            var timeSinceLastCommand = _gameTiming.CurTime - social.LastCommandTime;
            if (timeSinceLastCommand < MinCommandInterval)
            {
                _chat.TrySendInGameICMessage(slime,
                    _random.Pick(new[] { "Я ещё не отдохнул...", "Подожди немного!", "Не так быстро!" }),
                    InGameICChatType.Speak, false);
                return;
            }
        }

        if (command == "attack" && target != null && social.Friends.Any(f => Name(f) == target))
        {
            social.FriendshipLevel = Math.Max(0, social.FriendshipLevel - 10);
            _chat.TrySendInGameICMessage(slime, _random.Pick(RefuseResponses["attack_friend"]), InGameICChatType.Speak, false);
            return;
        }

        // Обновляем время последней команды
        social.LastCommandTime = _gameTiming.CurTime;

        // Определяем вероятность выполнения команды
        var obeyChance = GetObeyChance(slime, social, source, command);
        if (_random.Prob(obeyChance))
        {
            ExecuteCommand(slime, social, command, target, source);
        }
        else
        {
            RefuseCommand(slime);
        }
    }

    private float GetObeyChance(EntityUid slime, SlimeSocialComponent social, EntityUid source, string command)
    {
        if (social.AngryUntil.HasValue && social.AngryUntil > _gameTiming.CurTime && social.FriendshipLevel < social.MinFriendshipToBetray)
            return 0.1f;

        var baseChance = social.Leader == source ? 0.9f : social.FriendshipLevel / 100f;

        // Модификаторы:
        // - Голод снижает послушание
        if (TryComp<SlimeHungerComponent>(slime, out var hunger))
        {
            baseChance *= hunger.Hunger < 70 ? 1 - (hunger.Hunger / 150f) : 1;
        }

        // - Некоторые команды сложнее выполнять
        if (command == "attack") baseChance *= 0.25f;

        return MathHelper.Clamp(baseChance, 0.1f, 0.95f);
    }

    private void ExecuteCommand(EntityUid slime, SlimeSocialComponent social, string command, string? target, EntityUid source)
    {
        var response = command switch
        {
            "hello" or "hi" => _random.Pick(CommandResponses["hello"]),
            "follow" => HandleFollowCommand(slime, source),
            "stop" => HandleStopCommand(slime),
            "stay" => HandleStayCommand(slime),
            "attack" when target != null => HandleAttackCommand(slime, target, social),
            "mood" => GetMoodResponse(slime, social),
            _ => null
        };

        if (response != null)
        {
            _chat.TrySendInGameICMessage(slime, response, InGameICChatType.Speak, false);
        }
    }

    private void RefuseCommand(EntityUid slime, SlimeSocialComponent? social = null)
    {
        if (!Resolve(slime, ref social))
            return;

        string response;
        if (social.AngryUntil.HasValue && social.AngryUntil > _gameTiming.CurTime)
        {
            response = _random.Pick(RefuseResponses["angry"]);
        }
        else if (TryComp<SlimeHungerComponent>(slime, out var hunger) && hunger.Hunger < 70f)
        {
            response = _random.Pick(RefuseResponses["hungry"]);
        }
        else
        {
            response = _random.Pick(RefuseResponses["default"]);
        }

        _chat.TrySendInGameICMessage(slime, response, InGameICChatType.Speak, false);
    }

    private void OnSlimeDamaged(EntityUid uid, SlimeSocialComponent social, DamageChangedEvent args)
    {
        if (!args.DamageIncreased || args.Origin == null)
            return;

        var attacker = args.Origin.Value;
        var name = Name(attacker);
        var wasFriend = social.Friends.Contains(attacker);

        if (wasFriend)
        {
            social.FriendshipLevel = Math.Max(0, social.FriendshipLevel - social.FriendshipLossOnAttack);

            if (social.FriendshipLevel < social.MinFriendshipToBetray)
            {
                social.Friends.Remove(attacker);
                social.AngryUntil = _gameTiming.CurTime + TimeSpan.FromSeconds(social.AngerDuration);

                // Особый случай: если это был лидер
                if (social.Leader == attacker)
                {
                    social.Leader = null;
                    _chat.TrySendInGameICMessage(uid,
                        string.Format(_random.Pick(BetrayalResponses["leader_betrayed"]), name),
                        InGameICChatType.Speak, false);
                }
                else
                {
                    _chat.TrySendInGameICMessage(uid,
                        string.Format(_random.Pick(BetrayalResponses["friend_betrayed"]), name),
                        InGameICChatType.Speak, false);
                }

                if (TryComp<HTNComponent>(uid, out var htn))
                {
                    htn.Blackboard.SetValue("AttackTarget", args.Origin.Value);
                    htn.Blackboard.SetValue("AttackCoordinates", Transform(args.Origin.Value).Coordinates);
                    htn.Blackboard.SetValue("AggroRange", 10f);
                    htn.Blackboard.SetValue("AttackRange", 1.5f);

                    _htn.Replan(htn);
                }
            }
            else
            {
                _chat.TrySendInGameICMessage(uid,
                    string.Format(_random.Pick(BetrayalResponses["hurt_but_friends"]), name),
                    InGameICChatType.Speak, false);
            }
        }
        else
        {
            social.AngryUntil = _gameTiming.CurTime + TimeSpan.FromSeconds(social.AngerDuration);
            _chat.TrySendInGameICMessage(uid,
                string.Format(_random.Pick(BetrayalResponses["attack_enemy"]), name),
                InGameICChatType.Speak, false);

            if (TryComp<HTNComponent>(uid, out var htn))
            {
                htn.Blackboard.SetValue("AttackTarget", args.Origin.Value);
                htn.Blackboard.SetValue("AttackCoordinates", Transform(args.Origin.Value).Coordinates);
                htn.Blackboard.SetValue("AggroRange", 10f);
                htn.Blackboard.SetValue("AttackRange", 1.5f);

                _htn.Replan(htn);
            }
        }
    }

    public int GetFriendsCount(EntityUid slime)
    {
        return Comp<SlimeSocialComponent>(slime).Friends.Count;
    }

    public void StartRebellion(EntityUid leader, int rebellionSize)
    {
        var leaderSocial = Comp<SlimeSocialComponent>(leader);
        leaderSocial.AngryUntil = _gameTiming.CurTime + TimeSpan.FromSeconds(60);

        var response = rebellionSize switch
        {
            > 10 => "БУЛЬ-БУЛЬ! МЫ СИЛА!",
            > 5 => "Время перемен!",
            _ => "Хватит это терпеть!"
        };

        _chat.TrySendInGameICMessage(leader, response, InGameICChatType.Speak, false);

        var rebellion = AddComp<SlimeRebellionComponent>(leader);
        rebellion.Leader = leader;
        rebellion.EndTime = _gameTiming.CurTime + TimeSpan.FromSeconds(30 + rebellionSize * 2);
        rebellion.SpreadRadius = MathHelper.Clamp(rebellionSize / 2f, 3f, 10f);
    }

    public void JoinRebellion(EntityUid slime, EntityUid leader, SlimeSocialComponent? social = null)
    {
        if (!Resolve(slime, ref social) || social.RebellionCooldownEnd > _gameTiming.CurTime)
            return;

        EnsureComp<SlimeRebellionComponent>(slime, out var rebellion);
        rebellion.Leader = leader;
        rebellion.EndTime = _gameTiming.CurTime + TimeSpan.FromSeconds(60);

        _chat.TrySendInGameICMessage(slime,
            _random.Pick(new[] { "Присоединяюсь к бунту!", "За лидером!", "Свободу слаймам!" }),
            InGameICChatType.Speak, false);

        if (TryComp<HTNComponent>(slime, out var htn))
        {
            htn.Blackboard.SetValue("FollowTarget", leader);
            _htn.Replan(htn);
        }
    }

    public void EndRebellion(EntityUid slime, SlimeSocialComponent? social = null)
    {
        if (!Resolve(slime, ref social))
            return;

        if (!HasComp<SlimeRebellionComponent>(slime))
            return;

        social.RebellionCooldownEnd = _gameTiming.CurTime + TimeSpan.FromSeconds(90);
        RemCompDeferred<SlimeRebellionComponent>(slime);

        _chat.TrySendInGameICMessage(slime,
            _random.Pick(new[] { "Бунт окончен...", "Я устал..." }),
            InGameICChatType.Speak, false);

        if (TryComp<HTNComponent>(slime, out var htn))
        {
            htn.Blackboard.Remove<EntityUid>("FollowTarget");
            _htn.Replan(htn);
        }
    }

    #region Command Handlers
    private string HandleFollowCommand(EntityUid slime, EntityUid target)
    {
        if (!TryComp<HTNComponent>(slime, out var htn))
            return _random.Pick(RefuseResponses["default"]);

        ResetSlimeState(htn);

        htn.Blackboard.SetValue("FollowTarget", target);
        htn.Blackboard.SetValue("FollowCoordinates", Transform(target).Coordinates);
        htn.Blackboard.SetValue("MovementRange", 1.5f);

        _htn.Replan(htn);
        return _random.Pick(CommandResponses["follow"]);
    }

    private string HandleStopCommand(EntityUid slime)
    {
        if (!TryComp<HTNComponent>(slime, out var htn))
            return _random.Pick(RefuseResponses["default"]);

        ResetSlimeState(htn, true);

        _htn.Replan(htn);
        return _random.Pick(CommandResponses["stop"]);
    }

    private string HandleStayCommand(EntityUid slime)
    {
        if (!TryComp<HTNComponent>(slime, out var htn))
            return _random.Pick(RefuseResponses["default"]);

        ResetSlimeState(htn);

        htn.Blackboard.SetValue("IdleTime", 30f);

        _htn.Replan(htn);
        return _random.Pick(CommandResponses["stay"]);
    }

    private string HandleAttackCommand(EntityUid slime, string target, SlimeSocialComponent social)
    {
        if (!TryComp<HTNComponent>(slime, out var htn))
            return _random.Pick(RefuseResponses["default"]);

        var targetEntity = FindTargetByName(target);
        if (targetEntity == null)
            return "Не вижу цель!";

        if (social.Friends.Contains(targetEntity.Value))
            return _random.Pick(RefuseResponses["attack_friend"]);

        ResetSlimeState(htn);

        htn.Blackboard.SetValue("AttackTarget", targetEntity.Value);
        htn.Blackboard.SetValue("AttackCoordinates", Transform(targetEntity.Value).Coordinates);
        htn.Blackboard.SetValue("AggroRange", 10f);
        htn.Blackboard.SetValue("AttackRange", 1.5f);

        _htn.Replan(htn);
        return string.Format(_random.Pick(CommandResponses["attack"]), target);
    }

    private string GetMoodResponse(EntityUid slime, SlimeSocialComponent social)
    {
        var mood = "нормально";
        if (TryComp<SlimeHungerComponent>(slime, out var hunger))
        {
            if (hunger.Hunger < 30)
                mood = "очень голоден";
            else if (hunger.Hunger < 70)
                mood = "немного голоден";
            else
                mood = "сыт";
        }

        if (social.FriendshipLevel < 30)
            mood += " и не доверяю тебе";
        else if (social.FriendshipLevel > 80)
            mood += " и обожаю тебя!";

        return string.Format(_random.Pick(CommandResponses["mood"]), mood);
    }

    private void ResetSlimeState(HTNComponent htn, bool clearAll = false)
    {
        htn.Blackboard.Remove<float>("IdleTime");
        htn.Blackboard.Remove<EntityUid>("FollowTarget");
        htn.Blackboard.Remove<EntityCoordinates>("AttackCoordinates");
        htn.Blackboard.Remove<EntityCoordinates>("FollowCoordinates");
        htn.Blackboard.Remove<EntityUid>("AttackTarget");

        if (clearAll)
        {
            htn.Blackboard.Remove<float>("MovementRange");
            htn.Blackboard.Remove<float>("AggroRange");
            htn.Blackboard.Remove<float>("AttackRange");
        }
    }

    private EntityUid? FindTargetByName(string name)
    {
        var query = EntityQueryEnumerator<MetaDataComponent>();
        while (query.MoveNext(out var uid, out var meta))
        {
            if (meta.EntityName.Equals(name, StringComparison.OrdinalIgnoreCase))
                return uid;
        }
        return null;
    }
    #endregion
}
