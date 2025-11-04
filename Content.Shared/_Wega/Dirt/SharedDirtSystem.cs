using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Foldable;
using Content.Shared.Inventory;
using Content.Shared.Random.Helpers;
using Content.Shared.Standing;
using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.DirtVisuals;

public sealed class SharedDirtSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public const float MaxDirtLevel = 100f;
    private const float DirtAccumulationRate = 0.01f;
    private ProtoId<TagPrototype> _hardsuit = "Hardsuit";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DirtableComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<DirtableComponent, FoldedEvent>(OnFolded);
        SubscribeLocalEvent<DirtableComponent, ExaminedEvent>(OnExamined);
    }

    private void OnGetState(EntityUid uid, DirtableComponent comp, ref ComponentGetState args)
    {
        args.State = new DirtableComponentState(
            comp.CurrentDirtLevel,
            comp.DirtColor,
            comp.IsDirty
        );
    }

    private void OnFolded(EntityUid uid, DirtableComponent comp, ref FoldedEvent args)
    {
        // Finally update this shit
        Dirty(uid, comp);
    }

    private void OnExamined(Entity<DirtableComponent> entity, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange || entity.Comp.CurrentDirtLevel < entity.Comp.Threshold)
            return;

        float dirtPercentage = Math.Clamp(entity.Comp.CurrentDirtLevel.Float() / MaxDirtLevel * 100f, 0f, 100f);
        string colorHex = entity.Comp.DirtColor.ToHex();

        string dirtLevel = dirtPercentage switch
        {
            < 30 => Loc.GetString("dirt-examined-level-low"),
            < 70 => Loc.GetString("dirt-examined-level-medium"),
            _ => Loc.GetString("dirt-examined-level-high")
        };

        args.PushMarkup(
            Loc.GetString("dirt-examined-message", ("color", colorHex), ("percentage", (int)dirtPercentage), ("level", dirtLevel))
        );
    }

    public void AddBloodDirtFromDamage(EntityUid target, EntityUid attacker, DamageSpecifier damage, bool isGunshot = false)
    {
        if (!TryComp<BloodstreamComponent>(target, out var bloodstream))
            return;

        var bloodTypes = new[] { "Slash", "Piercing", "Blunt" };

        FixedPoint2 bloodAmount = 0;
        foreach (var type in bloodTypes)
        {
            if (damage.DamageDict.TryGetValue(type, out var amount))
                bloodAmount += amount;
        }

        if (bloodAmount <= 0)
            return;

        var bloodSolution = new Solution();
        bloodSolution.AddReagent(bloodstream.BloodReagent, bloodAmount * (0.2f / DirtAccumulationRate));

        var slots = new List<string> { "outerClothing", "jumpsuit", "gloves", "belt", "mask", "head" };
        ApplyDirtToClothing(target, bloodSolution, _random.Pick(slots));

        if (attacker != target)
        {
            if (isGunshot)
            {
                var targetPosition = _transform.GetWorldPosition(target);
                var attackerPosition = _transform.GetWorldPosition(attacker);
                var distance = (attackerPosition - targetPosition).Length();
                if (distance > 2.5f)
                    return;
            }
            ApplyDirtToClothing(attacker, bloodSolution, _random.Pick(slots));
        }
    }

    public void ApplyDirtToClothing(EntityUid wearer, Solution solution)
    {
        var slots = new List<string> { "shoes" };

        var isLyingDown = TryComp<StandingStateComponent>(wearer, out var standing) && !standing.Standing;
        if (isLyingDown)
        {
            slots.AddRange(new[] { "outerClothing", "jumpsuit", "gloves", "belt", "mask", "head" });
        }
        else
        {
            if (_inventory.TryGetSlotEntity(wearer, "outerClothing", out var outerClothing)
                && _tag.HasTag(outerClothing.Value, _hardsuit))
                slots.Add("outerClothing");
        }

        foreach (var slot in slots)
        {
            if (_inventory.TryGetSlotEntity(wearer, slot, out var clothing))
                UpdateDirtFromSolution(clothing.Value, solution);
        }
    }

    public void ApplyDirtToClothing(EntityUid wearer, Solution solution, string slot)
    {
        if (_inventory.TryGetSlotEntity(wearer, slot, out var clothing))
            UpdateDirtFromSolution(clothing.Value, solution);
    }

    private void UpdateDirtFromSolution(EntityUid uid, Solution solution, DirtableComponent? comp = null)
    {
        if (!TryComp(uid, out comp) || solution.Volume == 0)
            return;

        bool isOnlyWater = solution.Contents.Count == 1
            && solution.Contents[0].Reagent.Prototype == "Water";

        if (isOnlyWater)
        {
            float removedDirt = Math.Min(solution.Volume.Float() * DirtAccumulationRate * 2f, comp.CurrentDirtLevel.Float());
            CleanDirt(uid, removedDirt);
            return;
        }

        float addedDirt = Math.Min(solution.Volume.Float() * DirtAccumulationRate, 10f);
        comp.CurrentDirtLevel = FixedPoint2.New(
            Math.Min(comp.CurrentDirtLevel.Float() + addedDirt, MaxDirtLevel)
        );

        if (comp.CurrentDirtLevel >= MaxDirtLevel)
            return;

        var colors = new List<Color>();
        foreach (var reagent in solution.Contents)
        {
            if (reagent.Reagent.Prototype == "Water")
                continue;

            if (!_prototype.TryIndex<ReagentPrototype>(reagent.Reagent.Prototype, out var proto))
                continue;

            colors.Add(proto.SubstanceColor);
        }

        if (colors.Count > 0)
        {
            comp.DirtColor = BlendColorsProperly(comp.DirtColor, colors);
            Dirty(uid, comp);
        }
    }

    public void CleanDirt(EntityUid uid, float amount, float efficiency = 1f, DirtableComponent? comp = null)
    {
        if (!Resolve(uid, ref comp, false) || amount <= 0)
            return;

        var actualRemoval = amount * efficiency;
        comp.CurrentDirtLevel = FixedPoint2.Max(comp.CurrentDirtLevel - actualRemoval, 0);

        if (comp.CurrentDirtLevel <= 0)
            comp.DirtColor = Color.White;

        Dirty(uid, comp);
    }

    private Color BlendColorsProperly(Color currentColor, List<Color> newColors)
    {
        if (newColors.Count == 1 && currentColor == Color.White)
            return newColors[0];

        float rNew = 0, gNew = 0, bNew = 0;
        foreach (var color in newColors)
        {
            rNew += color.R / 255f;
            gNew += color.G / 255f;
            bNew += color.B / 255f;
        }
        rNew /= newColors.Count;
        gNew /= newColors.Count;
        bNew /= newColors.Count;

        float rCurrent = currentColor.R / 255f;
        float gCurrent = currentColor.G / 255f;
        float bCurrent = currentColor.B / 255f;

        float r = rCurrent * 0.85f + rNew * 0.15f;
        float g = gCurrent * 0.85f + gNew * 0.15f;
        float b = bCurrent * 0.85f + bNew * 0.15f;

        r = Math.Clamp(r, 0f, 1f) * 255f;
        g = Math.Clamp(g, 0f, 1f) * 255f;
        b = Math.Clamp(b, 0f, 1f) * 255f;

        return new Color(r, g, b);
    }
}
