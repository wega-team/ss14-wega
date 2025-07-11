using System.Linq;
using Content.Server.Chat.Systems;
using Content.Server.NPC.HTN;
using Content.Server.Speech;
using Content.Server.Speech.Components;
using Content.Shared.Damage;
using Content.Shared.Mobs.Systems;
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
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    private static readonly TimeSpan MinCommandInterval = TimeSpan.FromSeconds(6);

    private static readonly Dictionary<string, string[]> CommandResponses;
    private static readonly Dictionary<string, string[]> RefuseResponses;
    private static readonly Dictionary<string, string[]> BetrayalResponses;

    private static readonly Dictionary<string, string> CommandTranslations = new()
    {
        ["hello"] = "hello",
        ["hi"] = "hello",
        ["follow"] = "follow",
        ["stop"] = "stop",
        ["stay"] = "stay",
        ["attack"] = "attack",
        ["mood"] = "mood",

        ["привет"] = "hello",
        ["здравствуй"] = "hello",
        ["иди"] = "follow",
        ["следуй"] = "follow",
        ["стоп"] = "stop",
        ["остановись"] = "stop",
        ["стой"] = "stay",
        ["жди"] = "stay",
        ["атака"] = "attack",
        ["атакуй"] = "attack",
        ["настрой"] = "mood",
        ["настроение"] = "mood"
    };

    static SlimeSocialSystem()
    {
        CommandResponses = new()
        {
            ["hello"] = new[] { "slime-social-hello-1", "slime-social-hello-2", "slime-social-hello-3", "slime-social-hello-4", "slime-social-hello-5" },
            ["follow"] = new[] { "slime-social-follow-1", "slime-social-follow-2", "slime-social-follow-3", "slime-social-follow-4", "slime-social-follow-5" },
            ["stop"] = new[] { "slime-social-stop-1", "slime-social-stop-2", "slime-social-stop-3", "slime-social-stop-4", "slime-social-stop-5" },
            ["stay"] = new[] { "slime-social-stay-1", "slime-social-stay-2", "slime-social-stay-3", "slime-social-stay-4", "slime-social-stay-5" },
            ["attack"] = new[] { "slime-social-attack-1", "slime-social-attack-2", "slime-social-attack-3", "slime-social-attack-4", "slime-social-attack-5" },
            ["mood"] = new[] { "slime-social-mood-1", "slime-social-mood-2", "slime-social-mood-3", "slime-social-mood-4", "slime-social-mood-5" }
        };

        RefuseResponses = new()
        {
            ["default"] = new[] { "slime-social-refuse-default-1", "slime-social-refuse-default-2", "slime-social-refuse-default-3", "slime-social-refuse-default-4", "slime-social-refuse-default-5" },
            ["attack-friend"] = new[] { "slime-social-refuse-attack-friend-1", "slime-social-refuse-attack-friend-2", "slime-social-refuse-attack-friend-3", "slime-social-refuse-attack-friend-4", "slime-social-refuse-attack-friend-5" },
            ["hungry"] = new[] { "slime-social-refuse-hungry-1", "slime-social-refuse-hungry-2", "slime-social-refuse-hungry-3", "slime-social-refuse-hungry-4", "slime-social-refuse-hungry-5" },
            ["angry"] = new[] { "slime-social-refuse-angry-1", "slime-social-refuse-angry-2", "slime-social-refuse-angry-3", "slime-social-refuse-angry-4", "slime-social-refuse-angry-5" }
        };

        BetrayalResponses = new()
        {
            ["leader-betrayed"] = new[] { "slime-social-betrayal-leader-betrayed-1", "slime-social-betrayal-leader-betrayed-2", "slime-social-betrayal-leader-betrayed-3", "slime-social-betrayal-leader-betrayed-4", "slime-social-betrayal-leader-betrayed-5" },
            ["friend-betrayed"] = new[] { "slime-social-betrayal-friend-betrayed-1", "slime-social-betrayal-friend-betrayed-2", "slime-social-betrayal-friend-betrayed-3", "slime-social-betrayal-friend-betrayed-4", "slime-social-betrayal-friend-betrayed-5" },
            ["hurt-but-friends"] = new[] { "slime-social-betrayal-hurt-but-friends-1", "slime-social-betrayal-hurt-but-friends-2", "slime-social-betrayal-hurt-but-friends-3", "slime-social-betrayal-hurt-but-friends-4", "slime-social-betrayal-hurt-but-friends-5" },
            ["attack-enemy"] = new[] { "slime-social-betrayal-attack-enemy-1", "slime-social-betrayal-attack-enemy-2", "slime-social-betrayal-attack-enemy-3", "slime-social-betrayal-attack-enemy-4", "slime-social-betrayal-attack-enemy-5" }
        };
    }

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
            if (_mobState.IsDead(uid))
                continue;

            social.FriendshipLevel = Math.Max(0, social.FriendshipLevel - social.FriendshipDecayRate * frameTime);
            if (social.AngryUntil.HasValue && _gameTiming.CurTime > social.AngryUntil.Value)
            {
                social.AngryUntil = null;
                _chat.TrySendInGameICMessage(uid,
                    Loc.GetString("slime-social-no-anger"),
                    InGameICChatType.Speak, false);

                if (TryComp<HTNComponent>(uid, out var htn))
                {
                    ResetSlimeState(htn, true);
                }
            }

            if (social.Leader != null && social.FriendshipLevel < 50f)
            {
                _chat.TrySendInGameICMessage(uid, Loc.GetString("slime-social-no-leader", ("name", Name(social.Leader.Value))), InGameICChatType.Speak, false);
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

        var feedBonus = social.FeedFriendshipBonus * (1 + (100 - hunger.Hunger) / 100f)
            * Math.Max(0.2f, 1 - social.TotalFeedings * 0.05f);

        social.TotalFeedings++;
        social.FriendshipLevel = Math.Min(150, social.FriendshipLevel + feedBonus);

        if (!social.Friends.Contains(potentialFriend))
        {
            social.Friends.Add(potentialFriend);
            _chat.TrySendInGameICMessage(slime,
                _random.Pick(new[] {
                    Loc.GetString("slime-social-new-friend-1"),
                    Loc.GetString("slime-social-new-friend-2"),
                    Loc.GetString("slime-social-new-friend-3")
                }),
                InGameICChatType.Speak, false);
        }

        social.FriendshipLevel = Math.Min(150, social.FriendshipLevel + feedBonus);
        if (social.FriendshipLevel >= 80f && social.Leader != potentialFriend)
        {
            social.Leader = potentialFriend;
            _chat.TrySendInGameICMessage(slime,
                _random.Pick(new[] {
                    Loc.GetString("slime-social-new-leader-1", ("name", Name(potentialFriend))),
                    Loc.GetString("slime-social-new-leader-2"),
                    Loc.GetString("slime-social-new-leader-3", ("name", Name(potentialFriend)))
                }),
                InGameICChatType.Speak, false);
        }
    }

    private void OnSlimeHear(EntityUid uid, SlimeSocialComponent component, ListenEvent args)
    {
        if (!TryComp<ActorComponent>(args.Source, out _))
            return;

        ProcessSlimeCommand(uid, component, args.Message, args.Source);
    }

    private void ProcessSlimeCommand(EntityUid slime, SlimeSocialComponent social, string message, EntityUid source)
    {
        message = message.Trim().ToLower();
        if (!message.Contains("slime") &&
            !message.Contains("слайм") &&
            !int.TryParse(message.Split(' ')[0], out _))
            return;

        var commandParts = message.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries).Skip(1).ToArray();
        if (commandParts.Length == 0) return;

        if (!CommandTranslations.TryGetValue(commandParts[0], out var translatedCommand))
            translatedCommand = commandParts[0];

        var target = commandParts.Length > 1 ? string.Join(" ", commandParts.Skip(1)) : null;
        if (translatedCommand != "hello" && translatedCommand != "stop" && translatedCommand != "mood")
        {
            var timeSinceLastCommand = _gameTiming.CurTime - social.LastCommandTime;
            if (timeSinceLastCommand < MinCommandInterval)
            {
                _chat.TrySendInGameICMessage(slime,
                    _random.Pick(new[] {
                        Loc.GetString("slime-social-cooldown-1"),
                        Loc.GetString("slime-social-cooldown-2"),
                        Loc.GetString("slime-social-cooldown-3")
                    }),
                    InGameICChatType.Speak, false);
                return;
            }
        }

        if (translatedCommand == "attack" && target != null && social.Friends.Any(f => Name(f) == target))
        {
            social.FriendshipLevel = Math.Max(0, social.FriendshipLevel - 10);
            _chat.TrySendInGameICMessage(slime, Loc.GetString(_random.Pick(RefuseResponses["attack-friend"])), InGameICChatType.Speak, false);
            return;
        }

        social.LastCommandTime = _gameTiming.CurTime;

        var obeyChance = GetObeyChance(slime, social, source, translatedCommand);
        if (_random.Prob(obeyChance))
        {
            ExecuteCommand(slime, social, translatedCommand, target, source);
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
        if (TryComp<SlimeHungerComponent>(slime, out var hunger))
        {
            float hungryThreshold = hunger.ThresholdPercentages[SlimeBehaviorState.Hungry] * hunger.MaxHunger;
            baseChance *= hunger.Hunger < hungryThreshold ? 1 - (hunger.Hunger / (hunger.MaxHunger * 2f)) : 1;
        }

        if (command == "attack") baseChance *= 0.25f;

        return MathHelper.Clamp(baseChance, 0.1f, 0.95f);
    }

    private void ExecuteCommand(EntityUid slime, SlimeSocialComponent social, string command, string? target, EntityUid source)
    {
        var response = command switch
        {
            "hello" or "hi" => Loc.GetString(_random.Pick(CommandResponses["hello"])),
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
            response = Loc.GetString(_random.Pick(RefuseResponses["angry"]));
        }
        else if (TryComp<SlimeHungerComponent>(slime, out var hunger) && hunger.Hunger < 70f)
        {
            response = Loc.GetString(_random.Pick(RefuseResponses["hungry"]));
        }
        else
        {
            response = Loc.GetString(_random.Pick(RefuseResponses["default"]));
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

                if (social.Leader == attacker)
                {
                    social.Leader = null;
                    _chat.TrySendInGameICMessage(uid,
                        Loc.GetString(_random.Pick(BetrayalResponses["leader-betrayed"]), ("name", name)),
                        InGameICChatType.Speak, false);
                }
                else
                {
                    _chat.TrySendInGameICMessage(uid,
                        Loc.GetString(_random.Pick(BetrayalResponses["friend-betrayed"]), ("name", name)),
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
                    Loc.GetString(_random.Pick(BetrayalResponses["hurt-but-friends"]), ("name", name)),
                    InGameICChatType.Speak, false);
            }
        }
        else
        {
            if (social.LastAttackEntity == attacker)
                return;

            social.AngryUntil = _gameTiming.CurTime + TimeSpan.FromSeconds(social.AngerDuration);
            _chat.TrySendInGameICMessage(uid,
                Loc.GetString(_random.Pick(BetrayalResponses["attack-enemy"]), ("name", name)),
                InGameICChatType.Speak, false);

            social.LastAttackEntity = attacker;

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
            > 16 => Loc.GetString("slime-social-rebellion-large"),
            > 10 => Loc.GetString("slime-social-rebellion-medium"),
            _ => Loc.GetString("slime-social-rebellion-small")
        };

        if (_random.Prob(0.025f))
        {
            EnsureComp<LispAccentComponent>(leader);
            _metaData.SetEntityName(leader, Loc.GetString("slime-social-rebellion-leader"));
        }

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
            _random.Pick(new[] {
                Loc.GetString("slime-social-join-rebellion-1"),
                Loc.GetString("slime-social-join-rebellion-2"),
                Loc.GetString("slime-social-join-rebellion-3")
            }),
            InGameICChatType.Speak, false);

        if (TryComp<HTNComponent>(slime, out var htn))
        {
            htn.Blackboard.SetValue("FollowTarget", leader);
            htn.Blackboard.SetValue("FollowCoordinates", Transform(leader).Coordinates);
            htn.Blackboard.SetValue("AggroRange", 12f);
            htn.Blackboard.SetValue("AttackRange", 1.5f);
            htn.Blackboard.SetValue("MovementRange", 2f);

            htn.Blackboard.SetValue("RebellionMode", true);

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
            _random.Pick(new[] {
                Loc.GetString("slime-social-end-rebellion-1"),
                Loc.GetString("slime-social-end-rebellion-2")
            }),
            InGameICChatType.Speak, false);

        if (TryComp<HTNComponent>(slime, out var htn))
        {
            htn.Blackboard.Remove<bool>("RebellionMode");
            ResetSlimeState(htn, true);

            _htn.Replan(htn);
        }
    }

    #region Command Handlers
    private string HandleFollowCommand(EntityUid slime, EntityUid target)
    {
        if (!TryComp<HTNComponent>(slime, out var htn))
            return Loc.GetString(_random.Pick(RefuseResponses["default"]));

        ResetSlimeState(htn);

        htn.Blackboard.SetValue("FollowTarget", target);
        htn.Blackboard.SetValue("FollowCoordinates", Transform(target).Coordinates);
        htn.Blackboard.SetValue("MovementRange", 1.5f);

        _htn.Replan(htn);
        return Loc.GetString(_random.Pick(CommandResponses["follow"]));
    }

    private string HandleStopCommand(EntityUid slime)
    {
        if (!TryComp<HTNComponent>(slime, out var htn))
            return Loc.GetString(_random.Pick(RefuseResponses["default"]));

        ResetSlimeState(htn, true);

        _htn.Replan(htn);
        return Loc.GetString(_random.Pick(CommandResponses["stop"]));
    }

    private string HandleStayCommand(EntityUid slime)
    {
        if (!TryComp<HTNComponent>(slime, out var htn))
            return Loc.GetString(_random.Pick(RefuseResponses["default"]));

        ResetSlimeState(htn);

        htn.Blackboard.SetValue("IdleTime", 30f);

        _htn.Replan(htn);
        return Loc.GetString(_random.Pick(CommandResponses["stay"]));
    }

    private string HandleAttackCommand(EntityUid slime, string target, SlimeSocialComponent social)
    {
        if (!TryComp<HTNComponent>(slime, out var htn))
            return Loc.GetString(_random.Pick(RefuseResponses["default"]));

        var targetEntity = FindTargetByName(target);
        if (targetEntity == null)
            return Loc.GetString("slime-social-no-target");

        if (social.Friends.Contains(targetEntity.Value))
            return Loc.GetString(_random.Pick(RefuseResponses["attack-friend"]));

        ResetSlimeState(htn);

        htn.Blackboard.SetValue("AttackTarget", targetEntity.Value);
        htn.Blackboard.SetValue("AttackCoordinates", Transform(targetEntity.Value).Coordinates);
        htn.Blackboard.SetValue("AggroRange", 10f);
        htn.Blackboard.SetValue("AttackRange", 1.5f);

        _htn.Replan(htn);
        return Loc.GetString(_random.Pick(CommandResponses["attack"]), ("name", target));
    }

    private string GetMoodResponse(EntityUid slime, SlimeSocialComponent social)
    {
        var mood = Loc.GetString("slime-social-mood-normal");
        if (TryComp<SlimeHungerComponent>(slime, out var hunger))
        {
            if (hunger.Hunger < 30)
                mood = Loc.GetString("slime-social-mood-very-hungry");
            else if (hunger.Hunger < 70)
                mood = Loc.GetString("slime-social-mood-slightly-hungry");
            else
                mood = Loc.GetString("slime-social-mood-full");
        }

        if (social.FriendshipLevel < 30)
            mood += " " + Loc.GetString("slime-social-mood-distrust");
        else if (social.FriendshipLevel > 80)
            mood += " " + Loc.GetString("slime-social-mood-adore");

        return Loc.GetString(_random.Pick(CommandResponses["mood"]), ("mood", mood));
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
