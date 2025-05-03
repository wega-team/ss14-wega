using System.Linq;
using Content.Server.Chat.Systems;
using Content.Shared.Body.Components;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Systems;
using Content.Shared.Xenobiology;
using Content.Shared.Xenobiology.Components;
using Content.Shared.Xenobiology.Events;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Xenobiology;

public sealed class SlimeHungerSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SlimeSocialSystem _social = default!;
    [Dependency] private readonly SlimeGrowthSystem _growth = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    private readonly Dictionary<SlimeBehaviorState, string[]> _slimePhrases = new()
    {
        [SlimeBehaviorState.Passive] = new[]
        {
            "Буль-буль... Хорошо...",
            "Мягко-мягко...",
            "Тепло...",
            "Счастлив...",
            "М-м-м... Сытый...",
            "Жизнь прекрасна...",
            "Не трогай... Я отдыхаю...",
            "Можно поспать...",
            "Блестяще..."
        },
        [SlimeBehaviorState.Hungry] = new[]
        {
            "Блёрп? Где еда?",
            "Жрать давай...",
            "Эй, меня покорми!",
            "Я недоедаю...",
            "Слышал, у тебя есть вкусняшки?",
            "Мой живот урчит...",
            "Почему все едят, а я нет?",
            "Я тебя поцелую... если дашь еды...",
            "Голод — это жестоко...",
            "Скоро я стану злым..."
        },
        [SlimeBehaviorState.Aggressive] = new[]
        {
            "РРРБЛЮРП! УМРУ, НО СЪЕМ ТЕБЯ!",
            "ХАААААААААААААААС!",
            "ДАЙ ЕДЫЫЫЫ!",
            "Я СЕЙЧАС ВЗОРВУСЬ ОТ ГОЛОДА!",
            "ТЫ ВКУСНО ВЫГЛЯДИШЬ!",
            "БЕГИ, ПОКА Я НЕ СЬЕЛ ТВОЮ НОГУ!",
            "Я НЕ СЛАЙМ, Я БУРЯ!",
            "МНЕ НУЖНА ТВОЯ ЭНЕРГИЯ!",
            "ЖИВОТ СКРИПИТ, ЗУБЫ СКРЕЖЕТЯТ!",
            "УМРИТЕ ВСЕ, Я ГОЛОДЕН!"
        },
        [SlimeBehaviorState.Dividing] = new[]
        {
            "Ой-ой, я расту!",
            "Скоро нас будет двое!",
            "Я чувствую... умножение...",
            "Внутри меня шевелится жизнь!",
            "Мама-слайм гордилась бы мной...",
            "Я становлюсь больше мира!",
            "Почему я такой тяжёлый? А, это я делюсь!",
            "Сейчас будет БОЛЬШОЙ БУЛЬК!",
            "Я — начало новой цивилизации!",
            "Прощай, старый я..."
        },
    };

    private readonly string[] _overfeedPhrases = new[]
    {
        "Я ещё не переварил предыдущую еду!",
        "Слишком много еды за раз!",
        "Мой желудок ещё полон...",
        "Подожди, дай мне переварить!",
        "Ты что, хочешь меня лопнуть?",
        "Мне нужно время, чтобы усвоить пищу!",
        "Ты меня перекармливаешь!",
        "Я не резиновый, знаешь ли..."
    };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SlimeHungerComponent, SlimeHungerStateChangedEvent>(OnHungerStateChanged);
        SubscribeLocalEvent<SlimeFoodComponent, AfterInteractEvent>(OnFoodInteract);
        SubscribeLocalEvent<BodyComponent, SlimeFeedDoAfterEvent>(OnFoodInteractDoAfter);
    }

    public override void Update(float frameTime)
    {
        var entities = new List<(EntityUid, SlimeHungerComponent, SlimeGrowthComponent)>();
        var query = EntityQueryEnumerator<SlimeHungerComponent, SlimeGrowthComponent>();
        while (query.MoveNext(out var uid, out var hunger, out var growth))
        {
            entities.Add((uid, hunger, growth));
        }

        foreach (var (uid, hunger, growth) in entities)
        {
            if (_mobState.IsDead(uid))
                continue;

            ProcessHunger(uid, hunger, growth, frameTime);
        }
    }

    private void ProcessHunger(EntityUid uid, SlimeHungerComponent hunger, SlimeGrowthComponent growth, float frameTime)
    {
        hunger.MaxHunger = growth.CurrentStage switch
        {
            SlimeStage.Young => 200f,
            SlimeStage.Adult => 250f,
            SlimeStage.Old => 300f,
            SlimeStage.Ancient => 400f,
            _ => 200f
        };

        hunger.Hunger -= hunger.DecayRate * frameTime * (1 + 0.1f * (int)growth.CurrentStage);
        hunger.Hunger = Math.Clamp(hunger.Hunger, 0f, hunger.MaxHunger);

        if (growth.CurrentStage == SlimeStage.Ancient && hunger.Hunger >= hunger.MaxHunger)
        {
            if (hunger.CurrentState != SlimeBehaviorState.Dividing)
            {
                hunger.CurrentState = SlimeBehaviorState.Dividing;
                Dirty(uid, hunger);

                var ev = new SlimeHungerStateChangedEvent(SlimeBehaviorState.Dividing);
                RaiseLocalEvent(uid, ref ev);
            }
            return;
        }

        var thresholds = hunger.ThresholdPercentages.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value * hunger.MaxHunger
        );

        var newState = thresholds
            .Where(p => hunger.Hunger >= p.Value)
            .MaxBy(p => p.Value)
            .Key;

        if (hunger.Hunger >= growth.NextStageHungerThreshold)
            _growth.TryEvolve(uid, hunger, growth);

        if (newState != hunger.CurrentState)
        {
            hunger.CurrentState = newState;
            Dirty(uid, hunger);

            var ev = new SlimeHungerStateChangedEvent(newState);
            RaiseLocalEvent(uid, ref ev);
        }
    }

    private void OnHungerStateChanged(Entity<SlimeHungerComponent> ent, ref SlimeHungerStateChangedEvent args)
    {
        if (_slimePhrases.TryGetValue(args.NewState, out var phrases))
        {
            var randomPhrase = _random.Pick(phrases);
            _chat.TrySendInGameICMessage(ent, randomPhrase, InGameICChatType.Speak, false);
        }
    }

    private void OnFoodInteract(Entity<SlimeFoodComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target == null || !TryComp<SlimeHungerComponent>(args.Target, out var slimeHunger))
            return;

        TryFeedSlime(args.User, args.Target.Value, ent, slimeHunger);
    }

    public bool TryFeedSlime(EntityUid user, EntityUid slime, EntityUid food, SlimeHungerComponent? hunger = null)
    {
        if (!Resolve(slime, ref hunger) || !HasComp<SlimeFoodComponent>(food) || _mobState.IsDead(slime))
            return false;

        if (_gameTiming.CurTime < hunger.LastFeedTime + TimeSpan.FromSeconds(hunger.FeedCooldown))
        {
            _chat.TrySendInGameICMessage(slime, _random.Pick(_overfeedPhrases), InGameICChatType.Speak, false);
            return false;
        }

        float hungerFood = CalculateFoodValue(food, slime);

        var doAfterDelay = TimeSpan.FromSeconds(4);
        var doAfterEventArgs = new DoAfterArgs(EntityManager, user, doAfterDelay, new SlimeFeedDoAfterEvent() { Hunger = hungerFood }, user, target: slime, used: food)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            MovementThreshold = 0.01f,
            DistanceThreshold = 1.0f,
            NeedHand = true
        };

        _doAfter.TryStartDoAfter(doAfterEventArgs);

        return true;
    }

    public bool TryFeedSlime(EntityUid slime, EntityUid food, SlimeHungerComponent? hunger = null)
    {
        if (!Resolve(slime, ref hunger) || !HasComp<SlimeFoodComponent>(food))
            return false;

        float hungerFood = CalculateFoodValue(food, slime);

        var doAfterDelay = TimeSpan.FromSeconds(4);
        var doAfterEventArgs = new DoAfterArgs(EntityManager, slime, doAfterDelay, new SlimeFeedDoAfterEvent() { Hunger = hungerFood }, slime, target: slime, used: food)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            MovementThreshold = 0.5f,
            DistanceThreshold = 1.0f,
            NeedHand = false
        };

        return _doAfter.TryStartDoAfter(doAfterEventArgs);
    }

    private void OnFoodInteractDoAfter(Entity<BodyComponent> ent, ref SlimeFeedDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (args.Target == null || args.Used == null || !TryComp<SlimeHungerComponent>(args.Target, out var hunger))
            return;

        hunger.LastFeedTime = _gameTiming.CurTime;
        hunger.Hunger += args.Hunger;

        Dirty(args.Target.Value, hunger);
        QueueDel(args.Used.Value);
        if (TryComp<SlimeSocialComponent>(args.Target, out var social) && args.User != args.Target)
            _social.TryBefriend(args.Target.Value, args.User, hunger, social);

        args.Handled = true;
    }

    public float CalculateFoodValue(EntityUid food, EntityUid slime)
    {
        if (!TryComp<PhysicsComponent>(food, out var physics))
            return 0f;

        var modifier = 1f;
        if (physics.Mass == 0.25)
            modifier = 20f; // For meat

        var baseValue = (physics.Mass * modifier) * 3.0f;
        if (TryComp<SlimeGrowthComponent>(slime, out var growth) && growth.CurrentStage == SlimeStage.Young)
            baseValue *= 1.5f;

        return baseValue;
    }
}

[ByRefEvent]
public record struct SlimeHungerStateChangedEvent(SlimeBehaviorState NewState);
