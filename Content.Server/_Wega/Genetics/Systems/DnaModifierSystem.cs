using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Systems;
using Content.Server.Inventory;
using Content.Server.Prayer;
using Content.Shared.Body;
using Content.Shared.Buckle;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.Forensics.Components;
using Content.Shared.Genetics;
using Content.Shared.Genetics.Systems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Genetics.System;

public sealed partial class DnaModifierSystem : SharedDnaModifierSystem
{
    [Dependency] private IAdminLogManager _admin = default!;
    [Dependency] private IComponentFactory _componentFactory = default!;
    [Dependency] private SharedBuckleSystem _buckle = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private EnsureMarkingSystem _ensureMarking = default!;
    [Dependency] private StructuralEnzymesIndexerSystem _enzymesIndexer = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private ServerInventorySystem _inventory = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private SharedMindSystem _mindSystem = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private MobThresholdSystem _mobThreshold = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private ServerPrayerSystem _prayerSystem = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private static readonly ProtoId<EmotePrototype> Scream = "Scream";

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var instabilityQuery = EntityQueryEnumerator<DnaInstabilityComponent>();
        while (instabilityQuery.MoveNext(out var uid, out var instabilityComponent))
        {
            if (instabilityComponent.NextTimeTick <= 0)
            {
                instabilityComponent.NextTimeTick = 10;
                if (_mobState.IsDead(uid))
                    return;

                switch (instabilityComponent.Stage)
                {
                    case 1:
                        {
                            InstabilityStageOne(uid);
                            break;
                        }
                    case 2:
                        {
                            InstabilityStageTwo(uid);
                            break;
                        }
                    case 3:
                        {
                            InstabilityStageThree(uid);
                            break;
                        }
                }
            }
            instabilityComponent.NextTimeTick -= frameTime;
        }
    }

    [SubscribeLocalEvent(after:[typeof(SharedVisualBodySystem)])]
    private void OnDnaModifierInit(EntityUid uid, DnaModifierComponent component, MapInitEvent args)
    {
        InitializeStructuralEnzymes(uid, component);
        InitializeUniqueIdentifiers(uid, component);
        CheckDeviations(uid, component);
        Dirty(uid, component);
    }

    [SubscribeLocalEvent]
    private void OnDnaDeviation(EntityUid uid, DnaModifierDeviationComponent component, ComponentStartup args)
    {
        if (!TryComp<DnaModifierComponent>(uid, out var dnaModifier) || dnaModifier.EnzymesPrototypes == null)
            return;

        var diseaseIndices = new List<int>();
        for (var i = 0; i < dnaModifier.EnzymesPrototypes.Count; i++)
        {
            var e = dnaModifier.EnzymesPrototypes[i];
            if (ProtoMan.TryIndex<StructuralEnzymesPrototype>(e.EnzymesPrototypeId, out var p)
                && p.TypeDeviation == EnzymesType.Disease)
                diseaseIndices.Add(i);
        }

        if (diseaseIndices.Count == 0)
            return;

        var countToModify = _random.Next(1, Math.Min(3, diseaseIndices.Count + 1));
        var picked = diseaseIndices.OrderBy(_ => _random.Next()).Take(countToModify);
        foreach (var idx in picked)
        {
            dnaModifier.EnzymesPrototypes[idx] = dnaModifier.EnzymesPrototypes[idx] with { HexCode = GetHexCodeDisease() };
        }

        TryChangeStructuralEnzymes((uid, dnaModifier));
        Dirty(uid, dnaModifier);
    }

    #region Deep Cloning
    public UniqueIdentifiersData? CloneUniqueIdentifiers(UniqueIdentifiersData? source) => source?.Clone();
    public List<EnzymesPrototypeInfo>? CloneEnzymesPrototypes(List<EnzymesPrototypeInfo>? source) => source?.ToList();
    #endregion

    #region Initialize U.I.
    private void InitializeUniqueIdentifiers(EntityUid uid, DnaModifierComponent component, HumanoidProfileComponent? humanoid = null)
    {
        if (!Resolve(uid, ref humanoid, false))
        {
            InitializeEmptyUniqueIdentifiers((uid, component));
            return;
        }

        var u = new UniqueIdentifiersData();
        var speciesProto = ProtoMan.Index(humanoid.Species);
        if (!_visualBody.TryGatherMarkingsData(uid, null, out var profiles, out _, out var applied))
        {
            InitializeEmptyUniqueIdentifiers((uid, component));
            return;
        }

        List<Marking> GetMarkings(HumanoidVisualLayers layer)
        {
            foreach (var organMarkings in applied.Values)
            {
                if (organMarkings.TryGetValue(layer, out var m))
                    return m;
            }

            return new List<Marking>();
        }

        uint[] GetColorsFromMarking(Marking m)
            => m.MarkingColors.Count > 0
                ? m.MarkingColors.Select(FromColor).ToArray()
                : new[] { GenerateRandomColor() };

        var skinColor = Color.White;
        var eyeColor = Color.White;
        foreach (var profile in profiles.Values)
        {
            skinColor = profile.SkinColor;
            eyeColor = profile.EyeColor;
            break;
        }

        // Hair
        var hair = GetMarkings(HumanoidVisualLayers.Hair);
        if (hair.Count > 0)
        {
            u.HairColor = GetColorsFromMarking(hair[0]);
            u.HairStyle = hair[0].MarkingId;
        }
        else
        {
            u.HairColor = new[] { GenerateRandomColor() };
        }

        // FacialHair
        var beard = GetMarkings(HumanoidVisualLayers.FacialHair);
        if (beard.Count > 0)
        {
            u.BeardColor = GetColorsFromMarking(beard[0]);
            u.BeardStyle = beard[0].MarkingId;
        }
        else
        {
            u.BeardColor = new[] { GenerateRandomColor() };
        }

        // Skin/Fur
        var skinProto = ProtoMan.Index(speciesProto.SkinColoration);
        switch (skinProto.Strategy.InputType)
        {
            case SkinColorationStrategyInput.Unary:
                {
                    u.SkinTone = (uint)Math.Round(HumanSkinToneFromColor(skinColor));
                    break;
                }
            case SkinColorationStrategyInput.Color:
                {
                    u.FurColor = FromColor(skinColor);
                    break;
                }
        }

        // HeadTop
        var headTop = GetMarkings(HumanoidVisualLayers.HeadTop);
        if (headTop.Count > 0)
        {
            u.HeadAccessoryColor = GetColorsFromMarking(headTop[0]);
            u.HeadAccessoryStyle = headTop[0].MarkingId;
        }
        else
        {
            u.HeadAccessoryColor = new[] { GenerateRandomColor() };
        }

        // Head
        var head = GetMarkings(HumanoidVisualLayers.Head);
        if (head.Count > 0)
        {
            u.HeadMarkingColor = GetColorsFromMarking(head[0]);
            u.HeadMarkingStyle = head[0].MarkingId;
        }
        else
        {
            u.HeadMarkingColor = new[] { GenerateRandomColor() };
        }

        // HeadSide
        var headSide = GetMarkings(HumanoidVisualLayers.HeadSide);
        if (headSide.Count > 0)
        {
            u.HeadSideColor = GetColorsFromMarking(headSide[0]);
            u.HeadSideStyle = headSide[0].MarkingId;
        }
        else
        {
            u.HeadSideColor = new[] { GenerateRandomColor() };
        }

        // Snout
        var snout = GetMarkings(HumanoidVisualLayers.Snout);
        if (snout.Count > 0)
        {
            u.SnoutColor = GetColorsFromMarking(snout[0]);
            u.SnoutStyle = snout[0].MarkingId;
        }
        else
        {
            u.SnoutColor = new[] { GenerateRandomColor() };
        }

        // SnoutCover
        var snoutCover = GetMarkings(HumanoidVisualLayers.SnoutCover);
        if (snoutCover.Count > 0)
        {
            u.SnoutCoverColor = GetColorsFromMarking(snoutCover[0]);
            u.SnoutCoverStyle = snoutCover[0].MarkingId;
        }
        else
        {
            u.SnoutCoverColor = new[] { GenerateRandomColor() };
        }

        // Chest
        var chest = GetMarkings(HumanoidVisualLayers.Chest);
        if (chest.Count > 0)
        {
            u.BodyMarkingColor = GetColorsFromMarking(chest[0]);
            u.BodyMarkingStyle = chest[0].MarkingId;
        }
        else
        {
            u.BodyMarkingColor = new[] { GenerateRandomColor() };
        }

        // Tail
        var tail = GetMarkings(HumanoidVisualLayers.Tail);
        if (tail.Count > 0)
        {
            u.TailMarkingColor = GetColorsFromMarking(tail[0]);
            u.TailMarkingStyle = tail[0].MarkingId;
        }
        else
        {
            u.TailMarkingColor = new[] { GenerateRandomColor() };
        }

        // TailOverlay
        var tailOverlay = GetMarkings(HumanoidVisualLayers.TailOverlay);
        if (tailOverlay.Count > 0)
        {
            u.TailOverlayColor = GetColorsFromMarking(tailOverlay[0]);
            u.TailOverlayStyle = tailOverlay[0].MarkingId;
        }
        else
        {
            u.TailOverlayColor = new[] { GenerateRandomColor() };
        }

        u.EyeColor = FromColor(eyeColor);
        u.Sex = humanoid.Sex;

        component.UniqueIdentifiers = u;
    }

    private void InitializeEmptyUniqueIdentifiers(Entity<DnaModifierComponent> ent)
    {
        var u = new UniqueIdentifiersData
        {
            HairColor = new[] { GenerateRandomColor() },
            BeardColor = new[] { GenerateRandomColor() },
            SkinTone = GenerateRandomSkinTone(),
            FurColor = GenerateRandomColor(),
            HeadAccessoryColor = new[] { GenerateRandomColor() },
            HeadMarkingColor = new[] { GenerateRandomColor() },
            BodyMarkingColor = new[] { GenerateRandomColor() },
            TailMarkingColor = new[] { GenerateRandomColor() },
            TailOverlayColor = new[] { GenerateRandomColor() },
            SnoutColor = new[] { GenerateRandomColor() },
            SnoutCoverColor = new[] { GenerateRandomColor() },
            HeadSideColor = new[] { GenerateRandomColor() },
            EyeColor = GenerateRandomColor(),
            Sex = (Sex)_random.Next(0, 3)
        };

        if (ent.Comp.Upper != null)
        {
            ApplyDefaultsFromUpper(ent.Comp.Upper.Value, u);
        }

        ent.Comp.UniqueIdentifiers = u;
    }

    private void ApplyDefaultsFromUpper(EntProtoId upperId, UniqueIdentifiersData u)
    {
        if (!ProtoMan.TryIndex(upperId, out var upperProto))
            return;

        if (!upperProto.TryComp<HumanoidProfileComponent>(out var humanoid, _componentFactory))
            return;

        var speciesId = humanoid.Species.Id;
        MarkingsGroupPrototype? markingGroup = null;
        if (upperProto.TryComp<InitialBodyComponent>(out var initialBody, _componentFactory))
        {
            foreach (var organId in initialBody.Organs.Values)
            {
                if (!ProtoMan.TryIndex(organId, out var organProto))
                    continue;

                if (organProto.TryComp<VisualOrganMarkingsComponent>(out var visualMarkings, _componentFactory))
                {
                    if (ProtoMan.TryIndex(visualMarkings.MarkingData.Group, out var groupProto))
                    {
                        markingGroup = groupProto;
                        break;
                    }
                }
            }
        }

        if (markingGroup == null)
            return;

        foreach (var (layerEnum, limit) in markingGroup.Limits)
        {
            if (!limit.Required)
                continue;

            if (layerEnum is not HumanoidVisualLayers layer)
                continue;

            var markingId = GetRandomMarkingForLayer(layer, speciesId, false);
            if (string.IsNullOrEmpty(markingId) && limit.Default.Count > 0)
                markingId = limit.Default[0];

            if (string.IsNullOrEmpty(markingId))
                continue;

            if (!ProtoMan.TryIndex(markingId, out MarkingPrototype? markingProto))
                continue;

            var colors = new uint[Math.Max(1, markingProto.Sprites.Count)];
            for (var i = 0; i < colors.Length; i++)
            {
                colors[i] = GenerateRandomColor();
            }

            switch (layer)
            {
                case HumanoidVisualLayers.Hair:
                    u.HairStyle = markingId;
                    u.HairColor = colors;
                    break;
                case HumanoidVisualLayers.FacialHair:
                    u.BeardStyle = markingId;
                    u.BeardColor = colors;
                    break;
                case HumanoidVisualLayers.HeadTop:
                    u.HeadAccessoryStyle = markingId;
                    u.HeadAccessoryColor = colors;
                    break;
                case HumanoidVisualLayers.Head:
                    u.HeadMarkingStyle = markingId;
                    u.HeadMarkingColor = colors;
                    break;
                case HumanoidVisualLayers.HeadSide:
                    u.HeadSideStyle = markingId;
                    u.HeadSideColor = colors;
                    break;
                case HumanoidVisualLayers.Chest:
                    u.BodyMarkingStyle = markingId;
                    u.BodyMarkingColor = colors;
                    break;
                case HumanoidVisualLayers.Tail:
                    u.TailMarkingStyle = markingId;
                    u.TailMarkingColor = colors;
                    break;
                case HumanoidVisualLayers.TailOverlay:
                    u.TailOverlayStyle = markingId;
                    u.TailOverlayColor = colors;
                    break;
                case HumanoidVisualLayers.Snout:
                    u.SnoutStyle = markingId;
                    u.SnoutColor = colors;
                    break;
                case HumanoidVisualLayers.SnoutCover:
                    u.SnoutCoverStyle = markingId;
                    u.SnoutCoverColor = colors;
                    break;
            }
        }
    }

    public string GetRandomMarkingForLayer(HumanoidVisualLayers layer, string species, bool ignoreSpecies = true)
    {
        var query = ProtoMan.EnumeratePrototypes<MarkingPrototype>()
            .Where(m => m.MarkingType is not MarkingTypes.NonGenetics)
            .Where(m => m.BodyPart == layer);

        if (!ignoreSpecies)
        {
            query = query.Where(m => m.GroupWhitelist == null || m.GroupWhitelist.Count == 0
                || m.GroupWhitelist.Any(g => g.ToString() == species));
        }

        var markings = query.ToList();

        if (markings.Count == 0)
            return string.Empty;

        return _random.Pick(markings).ID;
    }

    private float HumanSkinToneFromColor(Color color)
    {
        var hsv = Color.ToHsv(color);
        if (Math.Clamp(hsv.X, 25f / 360f, 1) > 25f / 360f && hsv.Z == 1.0)
            return Math.Abs(45 - (hsv.X * 360));

        return hsv.Y * 100;
    }

    private Color ConvertSkinToneToColor(uint skinTone)
    {
        var tone = (int)Math.Clamp(skinTone, 0, 100);
        var offset = tone - 20;

        float hue = 25f, sat = 20f, val = 100f;
        if (offset <= 0)
        {
            hue += Math.Abs(offset);
        }
        else
        {
            sat += offset;
            val -= offset;
        }

        return Color.FromHsv(new Vector4(hue / 360f, sat / 100f, val / 100f, 1.0f));
    }
    #endregion

    #region Initialize S.E.
    private void InitializeStructuralEnzymes(EntityUid uid, DnaModifierComponent component)
    {
        var enzymesPrototypes = _enzymesIndexer.GetAllEnzymesPrototypes();
        var uniqueEnzymes = new List<EnzymesPrototypeInfo>();
        var hasHumanoid = HasComp<HumanoidProfileComponent>(uid);
        foreach (var p in enzymesPrototypes)
        {
            uniqueEnzymes.Add(new EnzymesPrototypeInfo
            {
                EnzymesPrototypeId = p.EnzymesPrototypeId,
                Order = p.Order,
                HexCode = p.Order == 55
                    ? (hasHumanoid ? GenerateLastHexCode() : GenerateHexCode())
                    : GenerateHexCode()
            });
        }
        component.EnzymesPrototypes = uniqueEnzymes;
    }

    private int GenerateHexCode() => (_random.Next(0, 3) << 8) | (_random.Next(0, 16) << 4) | _random.Next(0, 16);
    private int GenerateLastHexCode() => (_random.Next(8, 16) << 8) | (_random.Next(0, 16) << 4) | _random.Next(0, 16);
    #endregion

    #region Instability
    private void UpdateInstability(EntityUid uid, DnaModifierComponent component, int totalInstability)
    {
        component.Instability = totalInstability;
        if (totalInstability <= 20)
        {
            if (HasComp<DnaInstabilityComponent>(uid))
                RemComp<DnaInstabilityComponent>(uid);

            return;
        }

        var comp = EnsureComp<DnaInstabilityComponent>(uid);
        comp.Stage = totalInstability switch
        {
            > 65 => 3,
            > 35 => 2,
            _ => 1
        };

        Dirty(uid, component);
    }

    private void CheckDeviations(EntityUid uid, DnaModifierComponent component)
    {
        if (component.EnzymesPrototypes == null)
            return;

        var total = component.Instability;
        for (var i = 0; i < component.EnzymesPrototypes.Count; i++)
        {
            var enzyme = component.EnzymesPrototypes[i];
            if (string.IsNullOrEmpty(enzyme.EnzymesPrototypeId))
                continue;

            if (!ProtoMan.TryIndex<StructuralEnzymesPrototype>(enzyme.EnzymesPrototypeId, out var proto))
                continue;

            var hasComponent = false;
            if (proto.AddComponent != null)
            {
                foreach (var entry in proto.AddComponent)
                {
                    var type = entry.Value.Component?.GetType();
                    if (type == null)
                        continue;

                    if (type == typeof(PermanentStatusEffectsComponent))
                    {
                        var perm = entry.Value.Component as PermanentStatusEffectsComponent;
                        if (perm?.StatusEffects != null)
                        {
                            foreach (var eff in perm.StatusEffects)
                            {
                                if (HasPermanentStatusEffect(uid, eff.Id))
                                {
                                    hasComponent = true;
                                    break;
                                }
                            }
                        }
                    }
                    else if (HasComp(uid, type))
                    {
                        hasComponent = true;
                    }

                    if (hasComponent)
                        break;
                }
            }

            if (hasComponent)
            {
                component.EnzymesPrototypes[i] = enzyme with { HexCode = GetHexCodeForType(proto.TypeDeviation) };
                total += proto.CostInstability;

                if (proto.TypeDeviation != EnzymesType.Disease && proto.AddComponent != null)
                {
                    foreach (var entry in proto.AddComponent)
                    {
                        var type = entry.Value.Component?.GetType();
                        if (type != null && HasComp(uid, type))
                            component.InitialAbilities.Add(type);
                    }
                }
            }
        }

        UpdateInstability(uid, component, total);
    }

    private int GetHexCodeForType(EnzymesType type)
    {
        var first = type switch
        {
            EnzymesType.Disease or EnzymesType.Minor => 9,
            EnzymesType.Intermediate => 0xC,
            EnzymesType.Base => 0xE,
            _ => _random.Next(0, 16)
        };

        return (first << 8) | (_random.Next(0, 16) << 4) | _random.Next(0, 16);
    }

    private int GetHexCodeDisease() => (_random.Next(9, 16) << 8) | (_random.Next(0, 16) << 4) | _random.Next(2, 16);

    private void InstabilityStageOne(EntityUid uid)
    {
        if (_random.NextFloat() < 0.05f)
        {
            _damage.TryChangeDamage(uid, new DamageSpecifier { DamageDict = { { "Heat", 2.5 } } }, true);
            _popup.PopupEntity(Loc.GetString("dna-instability-stage-one"), uid, uid, PopupType.SmallCaution);
        }
    }

    private void InstabilityStageTwo(EntityUid uid)
    {
        if (_random.NextFloat() < 0.25f)
        {
            _damage.TryChangeDamage(uid, new DamageSpecifier { DamageDict = { { "Heat", 2.5 }, { "Blunt", 10 }, { "Structural", 2 } } }, true);
            _chat.TryEmoteWithoutChat(uid, ProtoMan.Index(Scream), true);
            _popup.PopupEntity(Loc.GetString("dna-instability-stage-two"), uid, uid, PopupType.SmallCaution);
        }
    }

    private void InstabilityStageThree(EntityUid uid)
    {
        if (_random.NextFloat() < 0.5f)
        {
            _damage.TryChangeDamage(uid, new DamageSpecifier { DamageDict = { { "Heat", 5 }, { "Blunt", 50 }, { "Structural", 4 } } }, true);
            _chat.TryEmoteWithoutChat(uid, ProtoMan.Index(Scream), true);
            _popup.PopupEntity(Loc.GetString("dna-instability-stage-three"), uid, uid, PopupType.LargeCaution);
        }
    }
    #endregion

    public void ChangeDna(Entity<DnaModifierComponent> ent, EnzymeInfo enzyme)
    {
        if (enzyme.Identifier != null) { ent.Comp.UniqueIdentifiers = enzyme.Identifier; }
        if (enzyme.Info != null) { ent.Comp.EnzymesPrototypes = enzyme.Info; }

        Dirty(ent, ent.Comp);
        TryChangeUniqueIdentifiers(ent);
        TryChangeStructuralEnzymes(ent);
    }

    public void ChangeDna(Entity<DnaModifierComponent> ent, int type)
    {
        switch (type)
        {
            case 0:
                {
                    TryChangeUniqueIdentifiers(ent);
                    break;
                }
            case 1:
                {
                    TryChangeStructuralEnzymes(ent);
                    break;
                }
        }
    }

    private void ChangeDna(Entity<DnaModifierComponent?> uid)
    {
        if (!Resolve(uid, ref uid.Comp, false))
            return;

        TryChangeUniqueIdentifiers((uid, uid.Comp));
        TryChangeStructuralEnzymes((uid, uid.Comp));
    }

    #region Modify U.I.
    private void TryChangeUniqueIdentifiers(Entity<DnaModifierComponent> ent, HumanoidProfileComponent? humanoid = null)
    {
        if (!Resolve(ent, ref humanoid, false) || ent.Comp.UniqueIdentifiers == null)
            return;

        var u = ent.Comp.UniqueIdentifiers;
        UpdateSkin((ent, humanoid), u);
        UpdateMarkings((ent, humanoid), u);
        UpdateEyeColor((ent, humanoid), u);
        UpdateGender((ent, humanoid), u);

        Dirty(ent, humanoid);
    }

    private void UpdateSkin(Entity<HumanoidProfileComponent> humanoid, UniqueIdentifiersData u)
    {
        var speciesProto = ProtoMan.Index(humanoid.Comp.Species);
        var skinProto = ProtoMan.Index(speciesProto.SkinColoration);

        var newColor = Color.White;
        switch (skinProto.Strategy.InputType)
        {
            case SkinColorationStrategyInput.Unary:
                {
                    newColor = ConvertSkinToneToColor(u.SkinTone);
                    break;
                }
            case SkinColorationStrategyInput.Color:
                {
                    newColor = ToColor(u.FurColor);
                    break;
                }
        }

        newColor = skinProto.Strategy.EnsureVerified(newColor);
        if (_visualBody.TryGatherMarkingsData(humanoid.Owner, null, out var profiles, out _, out _))
        {
            var updated = profiles.ToDictionary(p => p.Key, p => p.Value with { SkinColor = newColor });
            _visualBody.ApplyProfiles(humanoid.Owner, updated);
        }
    }

    private void UpdateMarkings(Entity<HumanoidProfileComponent> humanoid, UniqueIdentifiersData u)
    {
        var species = humanoid.Comp.Species;
        _ensureMarking.UpdateMarkingCategory(humanoid.Owner, HumanoidVisualLayers.Hair,
            u.HairColor, u.HairStyle, species);

        _ensureMarking.UpdateMarkingCategory(humanoid.Owner, HumanoidVisualLayers.FacialHair,
            u.BeardColor, u.BeardStyle, species);

        _ensureMarking.UpdateMarkingCategory(humanoid.Owner, HumanoidVisualLayers.Head,
            u.HeadMarkingColor, u.HeadMarkingStyle, species);

        _ensureMarking.UpdateMarkingCategory(humanoid.Owner, HumanoidVisualLayers.HeadTop,
            u.HeadAccessoryColor, u.HeadAccessoryStyle, species);

        _ensureMarking.UpdateMarkingCategory(humanoid.Owner, HumanoidVisualLayers.HeadSide,
            u.HeadSideColor, u.HeadSideStyle, species);

        _ensureMarking.UpdateMarkingCategory(humanoid.Owner, HumanoidVisualLayers.Chest,
            u.BodyMarkingColor, u.BodyMarkingStyle, species);

        _ensureMarking.UpdateMarkingCategory(humanoid.Owner, HumanoidVisualLayers.Tail,
            u.TailMarkingColor, u.TailMarkingStyle, species);

        var hasTail = !string.IsNullOrEmpty(u.TailMarkingStyle);
        _ensureMarking.UpdateMarkingCategory(humanoid.Owner, HumanoidVisualLayers.TailOverlay,
            hasTail ? u.TailOverlayColor : new uint[0],
            hasTail ? u.TailOverlayStyle : string.Empty, species);

        _ensureMarking.UpdateMarkingCategory(humanoid.Owner, HumanoidVisualLayers.Snout,
            u.SnoutColor, u.SnoutStyle, species);

        var hasSnout = !string.IsNullOrEmpty(u.SnoutStyle);
        _ensureMarking.UpdateMarkingCategory(humanoid.Owner, HumanoidVisualLayers.SnoutCover,
            hasSnout ? u.SnoutCoverColor : new uint[0],
            hasSnout ? u.SnoutCoverStyle : string.Empty, species);
    }

    private void UpdateEyeColor(Entity<HumanoidProfileComponent> humanoid, UniqueIdentifiersData u)
    {
        if (_visualBody.TryGatherMarkingsData(humanoid.Owner, null, out var profiles, out _, out _))
        {
            var updated = profiles.ToDictionary(p => p.Key, p => p.Value with { EyeColor = ToColor(u.EyeColor) });
            _visualBody.ApplyProfiles(humanoid.Owner, updated);
        }
    }

    private void UpdateGender(Entity<HumanoidProfileComponent> humanoid, UniqueIdentifiersData u)
    {
        humanoid.Comp.Sex = u.Sex;
        humanoid.Comp.Gender = u.Sex switch
        {
            Sex.Male => Gender.Male,
            Sex.Female => Gender.Female,
            _ => Gender.Neuter
        };
    }
    #endregion

    #region Modify S.E.
    public bool TrySetEnzymeHexCode(Entity<DnaModifierComponent?> ent, string enzymePrototypeId, int hexCode)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return false;

        if (ent.Comp.EnzymesPrototypes == null)
            return false;

        for (int i = 0; i < ent.Comp.EnzymesPrototypes.Count; i++)
        {
            if (string.Equals(ent.Comp.EnzymesPrototypes[i].EnzymesPrototypeId, enzymePrototypeId, StringComparison.Ordinal))
            {
                ent.Comp.EnzymesPrototypes[i] =
                    ent.Comp.EnzymesPrototypes[i] with { HexCode = hexCode };

                Dirty(ent, ent.Comp);
                ChangeDna((ent.Owner, ent.Comp), 1);
                return true;
            }
        }

        return false;
    }

    private void TryChangeStructuralEnzymes(Entity<DnaModifierComponent> ent)
    {
        if (ent.Comp.EnzymesPrototypes == null)
            return;

        var total = ent.Comp.Instability;
        var messages = new List<string>();
        for (var i = 0; i < ent.Comp.EnzymesPrototypes.Count; i++)
        {
            var enzyme = ent.Comp.EnzymesPrototypes[i];
            if (enzyme.Order == 55)
            {
                TryChangeLastBlock(ent, ent.Comp, enzyme);
                continue;
            }

            if (!ProtoMan.TryIndex<StructuralEnzymesPrototype>(enzyme.EnzymesPrototypeId, out var proto))
                continue;

            var meets = CheckHexCodeCondition(enzyme.HexCode, proto.TypeDeviation);
            if (proto.AddComponent == null)
                continue;

            foreach (var entry in proto.AddComponent)
            {
                var type = entry.Value.Component?.GetType();
                if (type == null)
                    continue;

                if (type == typeof(PermanentStatusEffectsComponent))
                {
                    var perm = entry.Value.Component as PermanentStatusEffectsComponent;
                    if (perm?.StatusEffects == null)
                        continue;

                    if (meets)
                    {
                        if (_random.NextFloat() <= proto.ChanceAssimilation)
                        {
                            foreach (var eff in perm.StatusEffects)
                            {
                                if (!HasPermanentStatusEffect(ent, eff.Id))
                                    AddPermanentStatusEffect(ent, eff.Id);
                            }
                            total += proto.CostInstability;

                            if (!string.IsNullOrEmpty(proto.Message))
                                messages.Add(proto.Message);

                            _admin.Add(LogType.Action, LogImpact.Medium,
                                    $"{ToPrettyString(ent):user} acquires gene type: '{proto.ID}'.");
                        }
                    }
                    else
                    {
                        var removed = false;
                        foreach (var eff in perm.StatusEffects)
                        {
                            if (HasPermanentStatusEffect(ent, eff.Id) && !ent.Comp.InitialAbilities.Contains(type))
                            {
                                RemovePermanentStatusEffect(ent, eff.Id);
                                removed = true;
                            }
                        }
                        if (removed)
                        {
                            total -= proto.CostInstability;
                            _admin.Add(LogType.Action, LogImpact.Medium,
                                $"{ToPrettyString(ent):user} loses gene type: '{proto.ID}'.");
                        }
                    }
                    continue;
                }

                if (meets)
                {
                    var hasAny = proto.AddComponent.Any(e =>
                    {
                        var t = e.Value.Component?.GetType();
                        return t != null && HasComp(ent, t);
                    });

                    if (!hasAny && _random.NextFloat() <= proto.ChanceAssimilation)
                    {
                        EntityManager.AddComponents(ent, proto.AddComponent, false);
                        total += proto.CostInstability;
                        if (!string.IsNullOrEmpty(proto.Message))
                            messages.Add(proto.Message);

                        _admin.Add(LogType.Action, LogImpact.Medium,
                            $"{ToPrettyString(ent):user} acquires gene type: '{proto.ID}'.");
                    }
                }
                else
                {
                    if (HasComp(ent, type) && !ent.Comp.InitialAbilities.Contains(type))
                    {
                        RemComp(ent, type);
                        total -= proto.CostInstability;
                        _admin.Add(LogType.Action, LogImpact.Medium,
                            $"{ToPrettyString(ent):user} loses gene type: '{proto.ID}'.");
                    }
                }
            }
        }

        UpdateInstability(ent, ent.Comp, total);
        if (messages.Count > 0) { _ = ShowMessagesWithDelay(ent, messages); }
    }

    private void TryChangeLastBlock(EntityUid target, DnaModifierComponent component, EnzymesPrototypeInfo enzyme)
    {
        if (string.IsNullOrEmpty(component.Upper) || string.IsNullOrEmpty(component.Lowest))
            return;

        var hexValue = (enzyme.HexCode >> 8) & 0xF;
        if (hexValue < 8)
        {
            if (!HasComp<HumanoidProfileComponent>(target))
                return;

            _buckle.TryUnbuckle(target, target, true);
            var child = Spawn(component.Lowest, Transform(target).Coordinates);
            if (_mobThreshold.GetScaledDamage(target, child, out var damage) && damage != null)
                _damage.SetDamage(child, damage);

            EnsureComp<DnaLowestComponent>(child).Parent = target;

            if (_inventory.TryGetContainerSlotEnumerator(target, out var enumerator))
            {
                while (enumerator.MoveNext(out var slot))
                {
                    _inventory.TryUnequip(target, slot.ID, true, true);
                }
            }

            foreach (var held in _hands.EnumerateHeld(target))
            {
                _hands.TryDrop(target, held);
            }

            if (TryComp(target, out MetaDataComponent? meta))
                _metaData.SetEntityName(child, meta.EntityName);

            if (_mindSystem.TryGetMind(target, out var mindId, out var mind))
                _mindSystem.TransferTo(mindId, child, mind: mind);

            if (TryComp(target, out DnaComponent? dna))
                EnsureComp<DnaComponent>(child).DNA = dna.DNA;

            var childMod = EnsureComp<DnaModifierComponent>(child);
            childMod.UniqueIdentifiers = component.UniqueIdentifiers;
            childMod.EnzymesPrototypes = component.EnzymesPrototypes?.ToList();
            childMod.Instability = component.Instability;
            childMod.Upper = component.Upper;
            childMod.Lowest = component.Lowest;

            Dirty(child, childMod);
            ChangeDna(child);

            _admin.Add(LogType.Action, LogImpact.High, $"{ToPrettyString(target):user} gene down up a step.");

            EnsurePausedMap();
            if (PausedMap != null)
                _transform.SetParent(target, Transform(target), PausedMap.Value);
        }
        else
        {
            if (HasComp<HumanoidProfileComponent>(target))
                return;

            if (TryComp<DnaLowestComponent>(target, out var low) && low.Parent != null)
            {
                var parent = low.Parent.Value;

                if (_inventory.TryGetContainerSlotEnumerator(target, out var e1))
                {
                    while (e1.MoveNext(out var slot))
                    {
                        _inventory.TryUnequip(target, slot.ID, true, true);
                    }
                }

                foreach (var held in _hands.EnumerateHeld(target))
                {
                    _hands.TryDrop(target, held);
                }

                foreach (var held in _hands.EnumerateHeld(target))
                {
                    _hands.TryDrop(target, held);
                    _hands.TryPickupAnyHand(parent, held, checkActionBlocker: false);
                }

                if (_mindSystem.TryGetMind(target, out var mindId, out var mind))
                    _mindSystem.TransferTo(mindId, parent, mind: mind);

                if (_mobThreshold.GetScaledDamage(target, parent, out var dmg) && dmg != null)
                    _damage.SetDamage(parent, dmg);

                if (TryComp<DnaModifierComponent>(parent, out var parentMod))
                {
                    parentMod.UniqueIdentifiers = component.UniqueIdentifiers;
                    parentMod.EnzymesPrototypes = component.EnzymesPrototypes?.ToList();
                    parentMod.Instability = component.Instability;
                    parentMod.Upper = component.Upper;
                    parentMod.Lowest = component.Lowest;
                    Dirty(parent, parentMod);
                    ChangeDna(parent);
                }

                var xform = Transform(parent);
                _transform.SetCoordinates(parent, xform, Transform(target).Coordinates);
                _transform.AttachToGridOrMap(parent, xform);
                Del(target);
                return;
            }

            _buckle.TryUnbuckle(target, target, true);
            var childUp = Spawn(component.Upper, Transform(target).Coordinates);
            if (_mobThreshold.GetScaledDamage(target, childUp, out var dmgUp) && dmgUp != null)
                _damage.SetDamage(childUp, dmgUp);

            if (_inventory.TryGetContainerSlotEnumerator(target, out var e2))
            {
                while (e2.MoveNext(out var slot))
                {
                    _inventory.TryUnequip(target, slot.ID, true, true);
                }
            }

            foreach (var held in _hands.EnumerateHeld(target))
            {
                _hands.TryDrop(target, held);
            }

            if (TryComp(target, out MetaDataComponent? meta2))
                _metaData.SetEntityName(childUp, meta2.EntityName);

            if (_mindSystem.TryGetMind(target, out var mindId2, out var mind2))
                _mindSystem.TransferTo(mindId2, childUp, mind: mind2);

            if (TryComp(target, out DnaComponent? dna2))
                EnsureComp<DnaComponent>(childUp).DNA = dna2.DNA;

            EnsureComp<DnaModifiedComponent>(childUp);

            var childModUp = EnsureComp<DnaModifierComponent>(childUp);
            childModUp.UniqueIdentifiers = component.UniqueIdentifiers;
            childModUp.EnzymesPrototypes = component.EnzymesPrototypes?.ToList();
            childModUp.Instability = component.Instability;
            childModUp.Upper = component.Upper;
            childModUp.Lowest = component.Lowest;

            Dirty(childUp, childModUp);
            ChangeDna(childUp);

            _admin.Add(LogType.Action, LogImpact.High, $"{ToPrettyString(target):user} gene went up a step.");
            Del(target);
        }
    }

    private async Task ShowMessagesWithDelay(EntityUid target, List<string> messages)
    {
        if (!TryComp<ActorComponent>(target, out var actor))
            return;

        foreach (var msg in messages)
        {
            _prayerSystem.SendSubtleMessage(actor.PlayerSession, actor.PlayerSession, string.Empty, Loc.GetString(msg));
            await Task.Delay(2000);
        }
    }

    private bool CheckHexCodeCondition(int hex, EnzymesType type)
    {
        var a = (hex >> 8) & 0xF;
        var b = (hex >> 4) & 0xF;
        var c = hex & 0xF;

        return type switch
        {
            EnzymesType.Disease or EnzymesType.Minor => a > 8 || (a == 8 && b >= 0 && c >= 2),
            EnzymesType.Intermediate => a > 0xB || (a == 0xB && b >= 0xE && c >= 0xA),
            EnzymesType.Base => a > 0xD || (a == 0xD && b >= 0xA && c >= 0xC),
            _ => false
        };
    }
    #endregion

    #region Chemistry
    [SubscribeLocalEvent]
    private void OnTryCureDnaDisease(EntityUid uid, DnaModifierComponent comp, CureDnaDiseaseAttemptEvent args)
    {
        if (comp.EnzymesPrototypes == null)
            return;

        for (var i = 0; i < comp.EnzymesPrototypes.Count; i++)
        {
            var enzyme = comp.EnzymesPrototypes[i];
            if (!ProtoMan.TryIndex<StructuralEnzymesPrototype>(enzyme.EnzymesPrototypeId, out var p))
                continue;

            if (p.TypeDeviation != EnzymesType.Disease)
                continue;

            var a = (enzyme.HexCode >> 8) & 0xF;
            var b = (enzyme.HexCode >> 4) & 0xF;
            var c = enzyme.HexCode & 0xF;
            if (a >= 8 && b >= 0 && c >= 2)
            {
                comp.EnzymesPrototypes[i] = enzyme with { HexCode = GenerateHexCode() };
            }
        }

        TryChangeStructuralEnzymes((uid, comp));
        Dirty(uid, comp);
    }

    [SubscribeLocalEvent]
    private void OnTryMutateDna(EntityUid uid, DnaModifierComponent comp, MutateDnaAttemptEvent args)
    {
        if (comp.EnzymesPrototypes == null)
            return;

        for (var i = 0; i < comp.EnzymesPrototypes.Count; i++)
        {
            var enzyme = comp.EnzymesPrototypes[i];
            if (enzyme.Order == 55)
            {
                comp.EnzymesPrototypes[i] = enzyme with { HexCode = GenerateLastHexCode() };
                continue;
            }

            if (!ProtoMan.TryIndex<StructuralEnzymesPrototype>(enzyme.EnzymesPrototypeId, out var p))
                continue;

            if (p.TypeDeviation == EnzymesType.Disease)
                comp.EnzymesPrototypes[i] = enzyme with { HexCode = GetHexCodeDisease() };
        }

        TryChangeStructuralEnzymes((uid, comp));
        Dirty(uid, comp);
    }
    #endregion

    [SubscribeLocalEvent]
    private void OnDamageChanged(EntityUid uid, DnaModifierComponent comp, DamageDealtEvent args)
    {
        if (args.Damage.GetTotal() <= 0 || !args.Damage.DamageDict.TryGetValue("Radiation", out var rad))
            return;

        if (rad < 1.5f)
            return;

        if (comp.EnzymesPrototypes == null)
            return;

        if (!_random.Prob(0.05f))
            return;

        var indices = new List<int>();
        for (var i = 0; i < comp.EnzymesPrototypes.Count; i++)
        {
            var e = comp.EnzymesPrototypes[i];
            if (ProtoMan.TryIndex<StructuralEnzymesPrototype>(e.EnzymesPrototypeId, out var p)
                && p.TypeDeviation == EnzymesType.Disease)
                indices.Add(i);
        }

        if (indices.Count == 0)
            return;

        var idx = indices[_random.Next(indices.Count)];
        comp.EnzymesPrototypes[idx] = comp.EnzymesPrototypes[idx] with { HexCode = GetHexCodeDisease() };

        TryChangeStructuralEnzymes((uid, comp));
        Dirty(uid, comp);
    }

    #region Temp
    private bool HasPermanentStatusEffect(EntityUid uid, EntProtoId effect)
        => TryComp<PermanentStatusEffectsComponent>(uid, out var p) && p.StatusEffects?.Contains(effect.Id) == true;

    private void AddPermanentStatusEffect(EntityUid uid, EntProtoId effect)
    {
        var p = EnsureComp<PermanentStatusEffectsComponent>(uid);
        p.StatusEffects ??= new HashSet<EntProtoId>();
        if (!p.StatusEffects.Contains(effect.Id))
        {
            p.StatusEffects.Add(effect.Id);
            Dirty(uid, p);
        }
    }

    private void RemovePermanentStatusEffect(EntityUid uid, EntProtoId effect)
    {
        if (!TryComp<PermanentStatusEffectsComponent>(uid, out var p))
            return;

        if (p.StatusEffects?.Remove(effect.Id) == true)
        {
            if (p.StatusEffects.Count == 0)
            {
                RemComp<PermanentStatusEffectsComponent>(uid);
                return;
            }

            Dirty(uid, p);
        }
    }
    #endregion
}
