using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Lavaland.Components;
using Content.Shared.Tools.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Lavaland;

public sealed class FloraSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly SharedPointLightSystem _light = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FloraComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<FloraComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<FloraComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<FloraComponent, FloraHarvestDoAfterEvent>(OnHarvestDoAfter);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var floraQuery = EntityQueryEnumerator<FloraComponent>();
        while (floraQuery.MoveNext(out var uid, out var flora))
        {
            if (flora.IsGrown)
                continue;

            if ((float)_gameTiming.CurTime.TotalSeconds >= flora.NextGrowthTick)
                CompleteGrowth(uid, flora);
        }
    }

    private void OnMapInit(EntityUid uid, FloraComponent component, MapInitEvent args)
    {
        component.GrowthTime = _random.NextFloat(component.MinGrowthTime, component.MaxGrowthTime);
        UpdateAppearance(uid, component);
    }

    private void OnInteractHand(EntityUid uid, FloraComponent component, InteractHandEvent args)
    {
        if (args.Handled || !component.IsGrown || component.SpecialTool != null)
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, TimeSpan.FromSeconds(component.HarvestDuration),
            new FloraHarvestDoAfterEvent(), uid, uid, args.User)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            BreakOnHandChange = true,
            NeedHand = false
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        args.Handled = true;
    }

    private void OnInteractUsing(EntityUid uid, FloraComponent component, InteractUsingEvent args)
    {
        if (args.Handled || !component.IsGrown)
            return;

        if (component.SpecialTool != null)
        {
            if (!_tool.HasQuality(args.Used, component.SpecialTool))
                return;
        }

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, TimeSpan.FromSeconds(component.HarvestDuration),
            new FloraHarvestDoAfterEvent(), uid, uid, args.Used)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            BreakOnHandChange = true,
            NeedHand = false
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        args.Handled = true;
    }

    private void OnHarvestDoAfter(EntityUid uid, FloraComponent component, FloraHarvestDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || !component.IsGrown || args.Used == null)
            return;

        if (component.SpecialTool != null && args.Used.Value.Valid)
        {
            if (!_tool.HasQuality(args.Used.Value, component.SpecialTool.Value))
                return;
        }

        HarvestFlora(uid, component, args.User);
        args.Handled = true;
    }

    private void CompleteGrowth(EntityUid uid, FloraComponent component)
    {
        component.IsGrown = true;

        UpdateAppearance(uid, component);
        Dirty(uid, component);
    }

    private void HarvestFlora(EntityUid uid, FloraComponent component, EntityUid user)
    {
        var yieldCount = _random.Next(component.MinYield, component.MaxYield + 1);

        var spawnPos = Transform(uid).Coordinates;
        for (int i = 0; i < yieldCount; i++)
        {
            Spawn(component.HarvestPrototype, spawnPos);
        }

        component.IsGrown = false;
        component.NextGrowthTick = (float)_gameTiming.CurTime.TotalSeconds + component.GrowthTime;

        _audio.PlayPvs(component.HarvestSound, uid);

        UpdateAppearance(uid, component);
        Dirty(uid, component);
    }

    private void UpdateAppearance(EntityUid uid, FloraComponent component)
    {
        if (HasComp<PointLightComponent>(uid))
            _light.SetEnabled(uid, component.IsGrown);

        if (!TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        FloraState state = FloraState.Grown;
        if (!component.IsGrown)
            state = FloraState.Harvested;

        _appearance.SetData(uid, FloraVisuals.State, state, appearance);
    }
}
