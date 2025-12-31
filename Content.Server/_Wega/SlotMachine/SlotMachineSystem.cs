using System.Linq;
using Content.Server.Chat.Systems;
using Content.Server.Destructible;
using Content.Server.Hands.Systems;
using Content.Server.Power.EntitySystems;
using Content.Server.Stack;
using Content.Shared.Damage.Systems;
using Content.Shared.Economy.SlotMachine;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Economy.SlotMachine;

public sealed class SlotMachineSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly DestructibleSystem _destructible = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StackSystem _stack = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly string[] AllSymbols = { "♥", "★", "♠", "♦", "♣", "♡" };
    private static readonly string[] CursedSymbols = { "☠", "🩸", "☢", "☣" };
    private static readonly string[] CursedWinSymbols = { "💰", "♔", "🎮" };
    private static readonly ProtoId<StackPrototype> Credit = "Credit";
    private static readonly EntProtoId SpaceCash = "SpaceCash";
    private static readonly EntProtoId Reward = "DiceOfFate";

    private const float JackpotChance = 0.0002f;
    private const float BigWinChance = 0.004f;
    private const float MediumWinChance = 0.016f;
    private const float SmallWinChance = 0.08f;
    private const float TinyWinChance = 0.1f;
    private const float CursedWinChance = 0.05f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SlotMachineComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SlotMachineComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<SlotMachineComponent, InteractUsingEvent>(OnInteractUsing);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SlotMachineComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Working && comp.SpinFinishTime.HasValue)
            {
                if (_timing.CurTime >= comp.SpinFinishTime.Value)
                    FinishSpin(uid, comp);
                else
                    UpdateSlotsAnimation(uid, comp);
            }
        }
    }

    private void OnMapInit(EntityUid uid, SlotMachineComponent comp, MapInitEvent args)
        => UpdateAppearance(uid);

    private void OnExamined(Entity<SlotMachineComponent> entity, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        string slots = string.Empty;
        foreach (var slot in entity.Comp.Slots)
            slots += $"{slot} ";

        args.PushMarkup(Loc.GetString("slot-machine-examine", ("slots", slots.Trim()), ("spins", entity.Comp.Plays)));

        if (TryComp<CursedSlotMachineComponent>(entity, out var cursedComp))
        {
            args.PushMarkup(Loc.GetString("cursed-slot-machine-uses",
                ("uses", cursedComp.Uses), ("max", cursedComp.MaxUses)));
        }
    }

    private void OnInteractUsing(Entity<SlotMachineComponent> entity, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TrySpin(entity, args.User, args.Used);
    }

    public bool TrySpin(Entity<SlotMachineComponent> entity, EntityUid user, EntityUid used)
    {
        if (!TryComp<StackComponent>(used, out var stack))
            return false;

        if (entity.Comp.Working)
        {
            _popup.PopupEntity(Loc.GetString("slot-machine-busy"), user, user);
            return false;
        }

        bool isCursed = HasComp<CursedSlotMachineComponent>(entity);
        if (!this.IsPowered(entity.Owner, EntityManager) && !isCursed)
        {
            _popup.PopupEntity(Loc.GetString("slot-machine-unpowered"), user, user);
            return false;
        }

        if (stack.StackTypeId != Credit)
            return false;

        if (isCursed)
        {
            var cursedComp = Comp<CursedSlotMachineComponent>(entity);
            if (cursedComp.Uses >= cursedComp.MaxUses)
            {
                _popup.PopupEntity(Loc.GetString("cursed-slot-machine-deny"), user, user, PopupType.SmallCaution);
                return false;
            }
        }

        if (stack.Count < entity.Comp.SpinCost)
        {
            _popup.PopupEntity(Loc.GetString("slot-machine-no-money"), user, user);
            return false;
        }

        StartSpin(entity, user, isCursed);
        _stack.ReduceCount(used, entity.Comp.SpinCost);
        return true;
    }

    private void StartSpin(Entity<SlotMachineComponent> entity, EntityUid user, bool isCursed)
    {
        entity.Comp.User = user;

        var spinTime = isCursed ? 5 : 2.5;
        entity.Comp.SpinFinishTime = _timing.CurTime + TimeSpan.FromSeconds(spinTime);
        entity.Comp.Working = true;
        entity.Comp.Plays++;

        entity.Comp.Slots = new[] { "?", "?", "?" };

        UpdateAppearance(entity.Owner);

        if (isCursed && TryComp<CursedSlotMachineComponent>(entity, out var cursedComp))
        {
            _audio.PlayPvs(entity.Comp.CoinSound, entity);
            _audio.PlayPvs(cursedComp.RollSound, entity);
        }
        else
        {
            _audio.PlayPvs(entity.Comp.CoinSound, entity);
            _audio.PlayPvs(entity.Comp.RollSound, entity);
        }

        _popup.PopupEntity(Loc.GetString("slot-machine-spinning"), user, user);

        if (isCursed)
        {
            _popup.PopupEntity(Loc.GetString("cursed-slot-machine-spin", ("name", Identity.Name(user, EntityManager))),
                entity.Owner, PopupType.Medium);
        }
    }

    private void UpdateSlotsAnimation(EntityUid uid, SlotMachineComponent comp)
    {
        var symbols = HasComp<CursedSlotMachineComponent>(uid) ? CursedSymbols : AllSymbols;

        for (int i = 0; i < comp.Slots.Length; i++)
        {
            if (_random.Prob(0.3f))
            {
                comp.Slots[i] = _random.Pick(symbols);
            }
        }
    }

    private void FinishSpin(EntityUid machineUid, SlotMachineComponent comp)
    {
        comp.Working = false;
        comp.SpinFinishTime = null;

        if (TryComp<CursedSlotMachineComponent>(machineUid, out var cursed))
        {
            DetermineCursedResult(machineUid, comp, cursed);
        }
        else
        {
            DetermineNormalResult(machineUid, comp);
        }

        UpdateAppearance(machineUid);

        _audio.PlayPvs(comp.EndSound, machineUid);
    }

    private void DetermineNormalResult(EntityUid machineUid, SlotMachineComponent comp)
    {
        var user = comp.User;
        if (user == null)
            return;

        var rand = _random.NextFloat();

        if (rand < JackpotChance)
        {
            GenerateJackpotSlots(comp);
            AwardJackpot(machineUid, comp, user.Value);
        }
        else if (rand < JackpotChance + BigWinChance)
        {
            GenerateBigWinSlots(comp);
            AwardBigWin(machineUid, comp, user.Value);
        }
        else if (rand < JackpotChance + BigWinChance + MediumWinChance)
        {
            GenerateMediumWinSlots(comp);
            AwardMediumWin(machineUid, comp, user.Value);
        }
        else if (rand < JackpotChance + BigWinChance + MediumWinChance + SmallWinChance)
        {
            GenerateSmallWinSlots(comp);
            AwardSmallWin(machineUid, comp, user.Value);
        }
        else if (rand < JackpotChance + BigWinChance + MediumWinChance + SmallWinChance + TinyWinChance)
        {
            GenerateTinyWinSlots(comp);
            AwardTinyWin(machineUid, comp, user.Value);
        }
        else
        {
            GenerateLoseSlots(comp);
            _popup.PopupEntity(Loc.GetString("slot-machine-lose"), user.Value, user.Value);
            _audio.PlayPvs(comp.FailedSound, machineUid);
        }

        comp.User = null;
    }

    private void DetermineCursedResult(EntityUid machineUid, SlotMachineComponent comp, CursedSlotMachineComponent cursed)
    {
        var user = comp.User;
        if (user == null)
            return;

        var rand = _random.NextFloat();

        if (rand < CursedWinChance)
        {
            GenerateCursedWinSlots(comp);
            AwardCursedJackpot(machineUid, user.Value, cursed);
        }
        else
        {
            GenerateCursedLoseSlots(comp);
            AwardCursedLoss(machineUid, comp, user.Value, cursed);
        }

        comp.User = null;
    }

    #region Slots Vis Generation

    private void GenerateJackpotSlots(SlotMachineComponent comp)
    {
        comp.Slots = new[] { "★", "★", "★" };
    }

    private void GenerateBigWinSlots(SlotMachineComponent comp)
    {
        var symbol = _random.Pick(AllSymbols.Where(s => s != "★").ToArray());
        comp.Slots = new[] { symbol, symbol, symbol };
    }

    private void GenerateMediumWinSlots(SlotMachineComponent comp)
    {
        var symbols = new[] { "♥", "♦", "♡" };
        var symbol = _random.Pick(symbols);
        comp.Slots = new[] { symbol, symbol, symbol };
    }

    private void GenerateSmallWinSlots(SlotMachineComponent comp)
    {
        var symbol = _random.Pick(AllSymbols);
        var otherSymbols = AllSymbols.Where(s => s != symbol).ToArray();

        var pattern = _random.Next(3);
        switch (pattern)
        {
            case 0:
                comp.Slots = new[] { symbol, symbol, _random.Pick(otherSymbols) };
                break;
            case 1:
                comp.Slots = new[] { _random.Pick(otherSymbols), symbol, symbol };
                break;
            default:
                comp.Slots = new[] { symbol, _random.Pick(otherSymbols), symbol };
                break;
        }
    }

    private void GenerateTinyWinSlots(SlotMachineComponent comp)
    {
        var symbols = new[] { "♠", "♣" };
        var symbol = _random.Pick(symbols);
        var otherSymbols = AllSymbols.Where(s => s != symbol).ToArray();

        var pattern = _random.Next(3);
        switch (pattern)
        {
            case 0:
                comp.Slots = new[] { symbol, symbol, _random.Pick(otherSymbols) };
                break;
            case 1:
                comp.Slots = new[] { _random.Pick(otherSymbols), symbol, symbol };
                break;
            default:
                comp.Slots = new[] { symbol, _random.Pick(otherSymbols), symbol };
                break;
        }
    }

    private void GenerateLoseSlots(SlotMachineComponent comp)
    {
        while (true)
        {
            comp.Slots = new[]
            {
                _random.Pick(AllSymbols),
                _random.Pick(AllSymbols),
                _random.Pick(AllSymbols)
            };

            if (IsLosingCombination(comp.Slots))
                break;
        }
    }

    private void GenerateCursedWinSlots(SlotMachineComponent comp)
    {
        var symbol = _random.Pick(CursedWinSymbols);
        comp.Slots = new[] { symbol, symbol, symbol };
    }

    private void GenerateCursedLoseSlots(SlotMachineComponent comp)
    {
        comp.Slots = new[]
        {
            _random.Pick(CursedSymbols),
            _random.Pick(CursedSymbols),
            _random.Pick(CursedSymbols)
        };
    }

    private bool IsLosingCombination(string[] slots)
    {
        if (slots[0] == slots[1] && slots[1] == slots[2])
            return false;

        if (slots[0] == slots[1] || slots[1] == slots[2] || slots[0] == slots[2])
            return false;

        var luckySymbols = new[] { "♥", "♦", "♡" };
        if (luckySymbols.Contains(slots[0]) && luckySymbols.Contains(slots[1]) && luckySymbols.Contains(slots[2]))
            return false;

        return true;
    }

    #endregion

    #region Awards

    private void AwardJackpot(EntityUid machineUid, SlotMachineComponent comp, EntityUid user)
    {
        SpawnAward(machineUid, user, comp.JackpotPrize);
        _audio.PlayPvs(comp.JackpotSound, machineUid);
        _popup.PopupEntity(Loc.GetString("slot-machine-jackpot", ("prize", comp.JackpotPrize)), user, user);

        var name = Identity.Name(user, EntityManager);
        _chat.DispatchGlobalAnnouncement(Loc.GetString("auto-announcements-jackpot", ("winner", name)),
            Loc.GetString("auto-announcements-title"), true, colorOverride: Color.Turquoise);
    }

    private void AwardBigWin(EntityUid machineUid, SlotMachineComponent comp, EntityUid user)
    {
        SpawnAward(machineUid, user, comp.BigWinPrize);
        _popup.PopupEntity(Loc.GetString("slot-machine-bigwin", ("prize", comp.BigWinPrize)), user, user);
    }

    private void AwardMediumWin(EntityUid machineUid, SlotMachineComponent comp, EntityUid user)
    {
        SpawnAward(machineUid, user, comp.MediumWinPrize);
        _popup.PopupEntity(Loc.GetString("slot-medium-win", ("prize", comp.MediumWinPrize)), user, user);
    }

    private void AwardSmallWin(EntityUid machineUid, SlotMachineComponent comp, EntityUid user)
    {
        SpawnAward(machineUid, user, comp.SmallWinPrize);
        _popup.PopupEntity(Loc.GetString("slot-small-win", ("prize", comp.SmallWinPrize)), user, user);
    }

    private void AwardTinyWin(EntityUid machineUid, SlotMachineComponent comp, EntityUid user)
    {
        SpawnAward(machineUid, user, comp.TinyWinPrize);
        _popup.PopupEntity(Loc.GetString("slot-tiny-win", ("prize", comp.TinyWinPrize)), user, user);
    }

    private void AwardCursedJackpot(EntityUid machineUid, EntityUid user, CursedSlotMachineComponent cursedComp)
    {
        var die = Spawn(Reward, Transform(machineUid).Coordinates);
        _hands.TryPickupAnyHand(user, die);

        _audio.PlayPvs(cursedComp.JackpotSound, machineUid);
        _popup.PopupEntity(Loc.GetString("cursed-slot-machine-jackpot", ("name", Name(user))), // He know who are you
            machineUid, PopupType.LargeCaution);

        cursedComp.Uses = 5; // Win. Stop
        Timer.Spawn(TimeSpan.FromSeconds(5), () => { _destructible.DestroyEntity(machineUid); });
    }

    private void AwardCursedLoss(EntityUid machineUid, SlotMachineComponent comp, EntityUid user, CursedSlotMachineComponent cursedComp)
    {
        cursedComp.Uses++;
        _damage.TryChangeDamage(user, cursedComp.Damage, true);

        _audio.PlayPvs(comp.FailedSound, machineUid);
        _popup.PopupEntity(Loc.GetString("cursed-slot-machine-lose"), user, user, PopupType.SmallCaution);
    }

    private void SpawnAward(EntityUid machineUid, EntityUid user, int award)
    {
        var cash = Spawn(SpaceCash, Transform(machineUid).Coordinates);
        _stack.SetCount((cash, null), award);

        _hands.TryPickupAnyHand(user, cash);
    }

    #endregion

    public void FreeSpeen(Entity<SlotMachineComponent?> entity, EntityUid user)
    {
        if (!Resolve(entity.Owner, ref entity.Comp))
            return;

        StartSpin((entity.Owner, entity.Comp), user, HasComp<CursedSlotMachineComponent>(entity));
    }

    private void UpdateAppearance(Entity<SlotMachineComponent?> entity)
    {
        if (!Resolve(entity.Owner, ref entity.Comp))
            return;

        _appearance.SetData(entity, SlotMachineVisuals.Working, entity.Comp.Working);
    }
}
