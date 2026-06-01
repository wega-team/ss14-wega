using Content.Server.Ninja.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Movement.Components;
using Content.Shared.Ninja.Components;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Random;
using System.Numerics;

namespace Content.Server.Ninja.Systems;

/// <summary>
/// Handles ninja phase cloak: full invisibility, examine blocking, overheating, and spark effects.
/// </summary>
public sealed class NinjaCloakSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom   _random    = default!;
    [Dependency] private readonly AudioSystem     _audio     = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly ItemToggleSystem _toggle   = default!;

    private static readonly SoundSpecifier SparksSound =
        new SoundCollectionSpecifier("sparks", AudioParams.Default.WithVolume(-4f));

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NinjaCloakActiveComponent, ComponentInit>(OnCloakInit);
        SubscribeLocalEvent<NinjaCloakActiveComponent, ComponentShutdown>(OnCloakShutdown);
        SubscribeLocalEvent<NinjaCloakActiveComponent, ExamineAttemptEvent>(OnExamineAttempt);
        SubscribeLocalEvent<NinjaCloakActiveComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<NinjaCloakActiveComponent, DamageChangedEvent>(OnCloakedDamaged);
        SubscribeLocalEvent<NinjaCloakHeatComponent, ExaminedEvent>(OnHeatExamined);
    }

    private void OnCloakInit(EntityUid uid, NinjaCloakActiveComponent _, ComponentInit args)
    {
        if (TryGetHeat(uid, out var heat))
            heat.IsCloaked = true;

        EnsureComp<FootstepModifierComponent>(uid);
    }

    private void OnCloakShutdown(EntityUid uid, NinjaCloakActiveComponent _, ComponentShutdown args)
    {
        if (TryGetHeat(uid, out var heat))
            heat.IsCloaked = false;

        RemComp<FootstepModifierComponent>(uid);
    }

    private void OnExamineAttempt(EntityUid uid, NinjaCloakActiveComponent _, ExamineAttemptEvent args)
    {
        var source = args.Examiner;
        do
        {
            if (source == uid) return;
            source = Transform(source).ParentUid;
        } while (source.IsValid());

        args.Cancel();
    }

    private void OnExamined(EntityUid uid, NinjaCloakActiveComponent _, ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("stealth-visual-effect", ("target", uid)));
    }

    private void OnHeatExamined(EntityUid uid, NinjaCloakHeatComponent heat, ExaminedEvent args)
    {
        var pct = (int) MathF.Round(heat.SparkChance * 100f);

        var (color, label) = heat.SparkChance switch
        {
            < 0.10f => ("green",  "Термодатчики в норме"),
            < 0.25f => ("yellow", "Лёгкий перегрев"),
            < 0.40f => ("orange", "Умеренный перегрев"),
            _       => ("red",    "Критический перегрев")
        };

        args.PushMarkup($"[color={color}]Термостат плаща: {label} ({pct}%)[/color]");
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<NinjaCloakHeatComponent>();
        while (query.MoveNext(out var suitUid, out var heat))
        {
            if (heat.IsCloaked)
            {
                heat.SparkChance = MathF.Min(
                    heat.SparkChance + NinjaCloakHeatComponent.HeatRate * frameTime,
                    NinjaCloakHeatComponent.MaxChance);

                heat.SparkTimer += frameTime;
                if (heat.SparkTimer >= NinjaCloakHeatComponent.SparkCheckInterval)
                {
                    heat.SparkTimer = 0f;
                    if (_random.Prob(heat.SparkChance))
                    {
                        var wearer = Transform(suitUid).ParentUid;
                        if (wearer.IsValid() && !TerminatingOrDeleted(wearer))
                            SpawnSparks(wearer);
                    }
                }
            }
            else
            {
                heat.SparkTimer = 0f;
                heat.SparkChance = MathF.Max(
                    heat.SparkChance - NinjaCloakHeatComponent.CoolRate * frameTime,
                    NinjaCloakHeatComponent.MinChance);
            }
        }
    }

    private void SpawnSparks(EntityUid uid)
    {
        var pos = Transform(uid).Coordinates;
        var count = _random.Next(2, 5);
        for (var i = 0; i < count; i++)
        {
            var offset = new Vector2(
                _random.NextFloat(-0.4f, 0.4f),
                _random.NextFloat(-0.4f, 0.4f));
            Spawn("EffectSparks", pos.Offset(offset));
        }
        _audio.PlayPvs(SparksSound, uid);
    }

    private void OnCloakedDamaged(EntityUid uid, NinjaCloakActiveComponent _, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased)
            return;

        if (!_inventory.TryGetSlotEntity(uid, "outerClothing", out var suitUid))
            return;

        _toggle.TryDeactivate(suitUid.Value, uid);
    }

    /// <summary>
    /// If the ninja is cloaked, deactivates the cloak and returns true.
    /// </summary>
    public bool TryRevealCloak(EntityUid performer)
    {
        if (!HasComp<NinjaCloakActiveComponent>(performer))
            return false;

        if (!_inventory.TryGetSlotEntity(performer, "outerClothing", out var suitUid))
            return false;

        _toggle.TryDeactivate(suitUid.Value, performer);
        return true;
    }

    private bool TryGetHeat(EntityUid wearerUid, out NinjaCloakHeatComponent heat)
    {
        heat = default!;
        return _inventory.TryGetSlotEntity(wearerUid, "outerClothing", out var suit)
               && TryComp(suit, out heat!);
    }
}
