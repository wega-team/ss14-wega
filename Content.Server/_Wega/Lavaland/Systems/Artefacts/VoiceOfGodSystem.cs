using System.Collections.Frozen;
using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Systems;
using Content.Server.Chat.Systems;
using Content.Shared.Atmos.Components;
using Content.Shared.Bed.Sleep;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Implants;
using Content.Shared.Lavaland.Artefacts.Components;
using Content.Shared.Medical;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Speech.Muting;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Lavaland.Artefacts.Systems;

public sealed class VoiceOfGodSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _admin = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly FlammableSystem _flammable = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;
    [Dependency] private readonly SharedBuckleSystem _buckle = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly SleepingSystem _sleeping = default!;
    [Dependency] private readonly VomitSystem _vomit = default!;

    // TODO: Make loc if you not lazy as me.
    private static readonly Dictionary<string, VoiceOfGodCommand> Commands = new()
    {
        { "stop", new("stop", "stop", VoiceOfGodEffect.Stun, 120f) },
        { "wait", new("wait", "stop", VoiceOfGodEffect.Stun, 120f) },
        { "stand still", new("stand still", "stop", VoiceOfGodEffect.Stun, 120f) },
        { "hold on", new("hold on", "stop", VoiceOfGodEffect.Stun, 120f) },
        { "halt", new("halt", "stop", VoiceOfGodEffect.Stun, 120f) },

        { "drop", new("drop", "drop", VoiceOfGodEffect.Weaken, 120f) },
        { "fall", new("fall", "drop", VoiceOfGodEffect.Weaken, 120f) },
        { "trip", new("trip", "drop", VoiceOfGodEffect.Weaken, 120f) },
        { "weaken", new("weaken", "drop", VoiceOfGodEffect.Weaken, 120f) },

        { "sleep", new("sleep", "sleep", VoiceOfGodEffect.Sleep, 120f) },
        { "slumber", new("slumber", "sleep", VoiceOfGodEffect.Sleep, 120f) },

        { "vomit", new("vomit", "vomit", VoiceOfGodEffect.Vomit, 120f) },
        { "throw up", new("throw up", "vomit", VoiceOfGodEffect.Vomit, 120f) },

        { "shut up", new("shut up", "shut up", VoiceOfGodEffect.Silence, 120f) },
        { "silence", new("silence", "shut up", VoiceOfGodEffect.Silence, 120f) },
        { "ssh", new("ssh", "shut up", VoiceOfGodEffect.Silence, 120f) },
        { "quiet", new("quiet", "shut up", VoiceOfGodEffect.Silence, 120f) },
        { "hush", new("hush", "shut up", VoiceOfGodEffect.Silence, 120f) },

        { "wake up", new("wake up", "wake up", VoiceOfGodEffect.WakeUp, 60f) },
        { "awaken", new("awaken", "wake up", VoiceOfGodEffect.WakeUp, 60f) },

        { "live", new("live", "live", VoiceOfGodEffect.Heal, 60f) },
        { "heal", new("heal", "live", VoiceOfGodEffect.Heal, 60f) },
        { "survive", new("survive", "live", VoiceOfGodEffect.Heal, 60f) },
        { "mend", new("mend", "live", VoiceOfGodEffect.Heal, 60f) },
        { "heroes never die", new("heroes never die", "live", VoiceOfGodEffect.Heal, 60f) },

        { "die", new("die", "die", VoiceOfGodEffect.Damage, 60f) },
        { "suffer", new("suffer", "die", VoiceOfGodEffect.Damage, 60f) },

        { "bleed", new("bleed", "bleed", VoiceOfGodEffect.Bleed, 60f) },

        { "burn", new("burn", "burn", VoiceOfGodEffect.Burn, 60f) },
        { "ignite", new("ignite", "burn", VoiceOfGodEffect.Burn, 60f) },

        { "shoo", new("shoo", "shoo", VoiceOfGodEffect.Push, 60f) },
        { "go away", new("go away", "shoo", VoiceOfGodEffect.Push, 60f) },
        { "leave me alone", new("leave me alone", "shoo", VoiceOfGodEffect.Push, 60f) },
        { "begone", new("begone", "shoo", VoiceOfGodEffect.Push, 60f) },
        { "flee", new("flee", "shoo", VoiceOfGodEffect.Push, 60f) },
        { "fus ro dah", new("fus ro dah", "shoo", VoiceOfGodEffect.Push, 60f) },

        { "get up", new("get up", "get up", VoiceOfGodEffect.StandUp, 60f) },

        { "who are you", new("who are you", "who are you", VoiceOfGodEffect.SayName, 30f) },
        { "say your name", new("say your name", "who are you", VoiceOfGodEffect.SayName, 30f) },
        { "state your name", new("state your name", "who are you", VoiceOfGodEffect.SayName, 30f) },
        { "identify", new("identify", "who are you", VoiceOfGodEffect.SayName, 30f) },

        { "say my name", new("say my name", "say my name", VoiceOfGodEffect.SayUserName, 30f) },

        { "knock knock", new("knock knock", "knock knock", VoiceOfGodEffect.KnockKnock, 30f) },

        { "state laws", new("state laws", "state laws", VoiceOfGodEffect.StateLaws, 30f) },
        { "state your laws", new("state your laws", "state laws", VoiceOfGodEffect.StateLaws, 30f) },

        { "throw", new("throw", "throw", VoiceOfGodEffect.Throw, 30f) },
        { "catch", new("catch", "throw", VoiceOfGodEffect.Throw, 30f) },

        { "sit", new("sit", "sit", VoiceOfGodEffect.Sit, 30f) },

        { "stand", new("stand", "stand", VoiceOfGodEffect.Stand, 30f) },

        { "salute", new("salute", "salute", VoiceOfGodEffect.Salute, 30f) },

        { "play dead", new("play dead", "play dead", VoiceOfGodEffect.PlayDead, 30f) },

        { "clap", new("clap", "clap", VoiceOfGodEffect.Clap, 30f) },
        { "applaud", new("applaud", "clap", VoiceOfGodEffect.Clap, 30f) },

        { "honk", new("honk", "honk", VoiceOfGodEffect.Honk, 30f) },

        { "rest", new("rest", "rest", VoiceOfGodEffect.Rest, 30f) }
    };

    private FrozenDictionary<string, VoiceOfGodCommand> _commandsChache = default!;

    private static readonly ProtoId<DamageTypePrototype> BluntDamage = "Blunt";

    private static readonly EntProtoId ForceSleeping = "StatusEffectForcedSleeping";

    private static readonly ProtoId<EmotePrototype> Deathgasp = "DefaultDeathgasp";
    private static readonly ProtoId<EmotePrototype> Salute = "Salute";
    private static readonly ProtoId<EmotePrototype> Clap = "Clap";
    private static readonly ProtoId<EmotePrototype> Honk = "Honk";

    public override void Initialize()
    {
        base.Initialize();

        InitializeCommandsChache();

        SubscribeLocalEvent<VoiceOfGodComponent, EntitySpokeEvent>(OnEntitySpoke);

        SubscribeLocalEvent<VoiceOfGodImplantComponent, ImplantImplantedEvent>(OnImplantImplanted);
        SubscribeLocalEvent<VoiceOfGodImplantComponent, ImplantRemovedEvent>(OnImplantRemoved);
    }

    private void InitializeCommandsChache()
    {
        var commandsDict = new Dictionary<string, VoiceOfGodCommand>();
        foreach (var pair in Commands)
        {
            commandsDict[pair.Key] = pair.Value;
        }

        _commandsChache = commandsDict.ToFrozenDictionary();
    }

    private void OnEntitySpoke(Entity<VoiceOfGodComponent> ent, ref EntitySpokeEvent args)
    {
        if (args.ObfuscatedMessage != null) // Ignore whisper
            return;

        var message = args.Message.ToLowerInvariant();
        var command = FindCommandInMessage(message);

        if (command != null)
        {
            ProcessCommand(ent, command, args.Message);
        }
    }

    private VoiceOfGodCommand? FindCommandInMessage(string message)
    {
        foreach (var (key, command) in _commandsChache)
        {
            if (message.Contains(key))
                return command;
        }

        return null;
    }

    private void ProcessCommand(Entity<VoiceOfGodComponent> ent, VoiceOfGodCommand command, string originalMessage)
    {
        if (_timing.CurTime < ent.Comp.LastCommandUse + TimeSpan.FromSeconds(ent.Comp.GlobalCooldown))
            return;

        if (ent.Comp.CommandCooldowns.TryGetValue(command.Id, out var lastUse))
        {
            var cooldownEnd = lastUse + TimeSpan.FromSeconds(command.Cooldown);
            if (_timing.CurTime < cooldownEnd)
                return;
        }

        var listeners = GetListenersInRange(ent);
        foreach (var listener in listeners)
        {
            ApplyEffect(listener, ent, command.Effect);
        }

        ent.Comp.CommandCooldowns[command.Id] = _timing.CurTime;
        ent.Comp.LastCommandUse = _timing.CurTime;

        _admin.Add(LogType.Action, LogImpact.Extreme,
            $"{ToPrettyString(ent):player} used Voice of God command '{command.Id}' with message: {originalMessage}");
    }

    private List<EntityUid> GetListenersInRange(EntityUid source)
    {
        var listeners = new List<EntityUid>();
        var range = SharedChatSystem.VoiceRange;

        var query = _lookup.GetEntitiesInRange<MobStateComponent>(Transform(source).Coordinates, range);
        foreach (var ent in query)
        {
            if (ent.Owner == source)
                continue;

            if (_mobState.IsIncapacitated(ent))
                continue;

            if (HasComp<DeafnessComponent>(ent))
                continue;

            listeners.Add(ent.Owner);
        }

        return listeners;
    }

    private void ApplyEffect(EntityUid target, EntityUid source, VoiceOfGodEffect effect)
    {
        switch (effect)
        {
            case VoiceOfGodEffect.Stun:
                ApplyStun(target, 4f + (float)_random.NextDouble() * 2f);
                break;
            case VoiceOfGodEffect.Weaken:
                ApplyWeaken(target, 60f);
                break;
            case VoiceOfGodEffect.Sleep:
                ApplySleep(target, 2f + (float)_random.NextDouble() * 2f);
                break;
            case VoiceOfGodEffect.Vomit:
                ApplyVomit(target);
                break;
            case VoiceOfGodEffect.Silence:
                ApplySilence(target, 20f);
                break;
            case VoiceOfGodEffect.WakeUp:
                ApplyWakeUp(target);
                break;
            case VoiceOfGodEffect.Heal:
                ApplyHeal(target, 20f);
                break;
            case VoiceOfGodEffect.Damage:
                ApplyDamage(target, 15f);
                break;
            case VoiceOfGodEffect.Bleed:
                ApplyBleed(target);
                break;
            case VoiceOfGodEffect.Burn:
                ApplyBurn(target);
                break;
            case VoiceOfGodEffect.Push:
                ApplyPush(target, source);
                break;
            case VoiceOfGodEffect.StandUp:
                ApplyStandUp(target);
                break;
            case VoiceOfGodEffect.SayName:
                MakeSayName(target);
                break;
            case VoiceOfGodEffect.SayUserName:
                MakeSayUserName(target, source);
                break;
            case VoiceOfGodEffect.KnockKnock:
                MakeKnockKnock(target);
                break;
            case VoiceOfGodEffect.StateLaws:
                MakeStateLaws(target);
                break;
            case VoiceOfGodEffect.Throw:
                ToggleThrow(target);
                break;
            case VoiceOfGodEffect.Sit:
                MakeSit(target);
                break;
            case VoiceOfGodEffect.Stand:
                MakeStand(target);
                break;
            case VoiceOfGodEffect.Salute:
                MakeSalute(target);
                break;
            case VoiceOfGodEffect.PlayDead:
                MakePlayDead(target);
                break;
            case VoiceOfGodEffect.Clap:
                MakeClap(target);
                break;
            case VoiceOfGodEffect.Honk:
                MakeHonk(target);
                break;
            case VoiceOfGodEffect.Rest:
                MakeRest(target);
                break;
        }
    }

    private void OnImplantImplanted(Entity<VoiceOfGodImplantComponent> ent, ref ImplantImplantedEvent args)
    {
        EnsureComp<VoiceOfGodComponent>(args.Implanted);
    }

    private void OnImplantRemoved(Entity<VoiceOfGodImplantComponent> ent, ref ImplantRemovedEvent args)
    {
        RemComp<VoiceOfGodComponent>(args.Implanted);
    }

    #region Effects
    private void ApplyStun(EntityUid target, float duration)
    {
        _stun.TryAddStunDuration(target, TimeSpan.FromSeconds(duration));
    }

    private void ApplyWeaken(EntityUid target, float amount)
    {
        _stamina.TakeStaminaDamage(target, amount);
    }

    private void ApplySleep(EntityUid target, float duration)
    {
        _statusEffects.TryAddStatusEffectDuration(target, ForceSleeping, out _, TimeSpan.FromSeconds(duration));
    }

    private void ApplyVomit(EntityUid target)
    {
        _vomit.Vomit(target);
    }

    private void ApplySilence(EntityUid target, float duration)
    {
        if (HasComp<MutedComponent>(target))
            return;

        EnsureComp<MutedComponent>(target);
        Timer.Spawn(TimeSpan.FromSeconds(duration), () =>
        {
            RemComp<MutedComponent>(target);
        });
    }

    private void ApplyWakeUp(EntityUid target)
    {
        _sleeping.TryWaking(target, true);
    }

    private void ApplyHeal(EntityUid target, float amount)
    {
        if (!TryComp<DamageableComponent>(target, out var damageable))
            return;

        var positiveDamage = _damage.GetPositiveDamage((target, damageable));
        var damagedTypes = positiveDamage.DamageDict.Keys.ToList();

        if (damagedTypes.Count == 0)
            return;

        var type = damagedTypes[_random.Next(damagedTypes.Count)];
        if (!positiveDamage.DamageDict.TryGetValue(type, out var currentDamage))
            return;

        var healAmount = FixedPoint2.Min(currentDamage, amount);

        var healSpecifier = new DamageSpecifier();
        healSpecifier.DamageDict[type] = -healAmount;
        _damage.TryChangeDamage(target, healSpecifier, true, false);
    }

    private void ApplyDamage(EntityUid target, float amount)
    {
        var damage = new DamageSpecifier { DamageDict = { { BluntDamage, amount } } };
        _damage.TryChangeDamage(target, damage);
    }

    private void ApplyBleed(EntityUid target)
    {
        _bloodstream.TryModifyBleedAmount(target, 10f);
    }

    private void ApplyBurn(EntityUid target)
    {
        if (!TryComp<FlammableComponent>(target, out var flammable))
            return;

        _flammable.AdjustFireStacks(target, flammable.MaximumFireStacks, ignite: true);
    }

    private void ApplyPush(EntityUid target, EntityUid source)
    {
        if (!TryComp(target, out PhysicsComponent? physics))
            return;

        var sourcePosition = _transform.GetWorldPosition(target);
        var targetPosition = _transform.GetWorldPosition(target);
        var direction = (targetPosition - sourcePosition).Normalized();

        var force = 10000f;
        if (physics.Mass < 80f)
            force *= 2;

        _physics.ApplyLinearImpulse(target, direction * force, body: physics);
    }

    private void ApplyStandUp(EntityUid target)
    {
        _stun.ForceStandUp(target);
    }

    private void MakeSayName(EntityUid target)
    {
        _chat.TrySendInGameICMessage(target, Name(target), InGameICChatType.Speak, false);
    }

    private void MakeSayUserName(EntityUid target, EntityUid source)
    {
        _chat.TrySendInGameICMessage(target, Identity.Name(source, EntityManager), InGameICChatType.Speak, false);
    }

    private void MakeKnockKnock(EntityUid target)
    {
        _chat.TrySendInGameICMessage(target, "who's there?", InGameICChatType.Speak, false);
    }

    private void MakeStateLaws(EntityUid target)
    {
        /// TODO: Do it if you find where they say it, I've tried.
    }

    private void ToggleThrow(EntityUid target)
    {
        if (!_hands.TryGetActiveItem(target, out var item))
            return;

        _throwing.TryThrow(item.Value, _random.NextVector2());
    }

    private void MakeSit(EntityUid target)
    {
        var strap = _lookup.GetEntitiesInRange<StrapComponent>(Transform(target).Coordinates, 1f)
            .Where(s => s.Comp.Enabled).ToList();

        if (strap.Count == 0)
            return;

        _buckle.TryBuckle(target, null, _random.Pick(strap), popup: false);
    }

    private void MakeStand(EntityUid target)
    {
        _buckle.Unbuckle(target, null);
    }

    private void MakeSalute(EntityUid target)
    {
        _chat.TryEmoteWithChat(target, Salute);
    }

    private void MakePlayDead(EntityUid target)
    {
        _chat.TryEmoteWithChat(target, Deathgasp);
    }

    private void MakeClap(EntityUid target)
    {
        _chat.TryEmoteWithChat(target, Clap);
    }

    private void MakeHonk(EntityUid target)
    {
        _chat.TryEmoteWithChat(target, Honk);
    }

    private void MakeRest(EntityUid target)
    {
        _stun.TryKnockdown(target, null);
    }

    #endregion
}

public sealed class VoiceOfGodCommand
{
    public string Key { get; set; }
    public string Id { get; set; }
    public VoiceOfGodEffect Effect { get; set; }
    public float Cooldown { get; set; }
    public string[] Keywords { get; set; }

    public VoiceOfGodCommand(string key, string id, VoiceOfGodEffect effect, float cooldown, string[]? keywords = null)
    {
        Key = key;
        Id = id;
        Effect = effect;
        Cooldown = cooldown;
        Keywords = keywords ?? new[] { key };
    }
}
