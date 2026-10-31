using System.Numerics;
using Content.Shared.Audio;
using Content.Shared.Audio.Components;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Robust.Client.Audio;
using Robust.Client.Player;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Sources;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Client.Audio;

/// <summary>
/// Handles client side sound insulation and environmental reverb.
/// </summary>
public sealed partial class SoundInsulationSystem : SharedSoundInsulationSystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private SharedDynamicAudioSystem _dynamicAudio = default!;
    [Dependency] private SoundRayTracer _rayTracer = default!;
    [Dependency] private SharedMapSystem _maps = default!;
    [Dependency] private SharedTransformSystem _xform = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;

    // Volume attenuation and occlusion multipliers
    private const float MaxVolumeAttenuationDb = 24f;
    private const float OcclusionMultiplier = 4f;
    private const float MaxOcclusion = 8f;

    // Full muffling and deafness thresholds
    private const float FullyMuffledThreshold = 0.999f;
    private const float DeafOcclusion = 100f;
    private const float SilentVolumeDb = -100f;

    // Runtime tracking and queries
    private EntityQuery<PhysicsComponent> _physicsQuery;
    private readonly List<Entity<AudioComponent>> _tracked = new();
    private readonly Dictionary<EntityUid, EntityUid> _localAux = new();

    private bool _insulationEnabled = true;
    private bool _audioEffectsEnabled = true;

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_cfg, WegaCVars.SoundInsulationEnabled, OnSoundInsulationChanged, true);
        Subs.CVar(_cfg, WegaCVars.AudioEffectsEnabled, OnAudioEffectsEnabledChanged, true);

        _physicsQuery = GetEntityQuery<PhysicsComponent>();
        _audio.ProcessStreamOverride += OnProcessStream;
        UpdatesAfter.Add(typeof(AudioSystem));
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _audio.ProcessStreamOverride -= OnProcessStream;

        _tracked.Clear();
        _localAux.Clear();
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);
        UpdateAuxMappings();
    }

    private void OnSoundInsulationChanged(bool value)
    {
        _insulationEnabled = value;
    }

    private void OnAudioEffectsEnabledChanged(bool value)
    {
        _audioEffectsEnabled = value;

        if (!value)
        {
            _localAux.Clear();
            foreach (var ent in _tracked)
            {
                if (Exists(ent) && ent.Comp.Loaded)
                    ApplyAuxiliaryToSource(ent);
            }
        }
    }

    [SubscribeLocalEvent]
    private void OnAudioAdd(Entity<AudioComponent> ent, ref ComponentAdd args)
    {
        _tracked.Add(ent);
    }

    [SubscribeLocalEvent]
    private void OnAudioRemove(Entity<AudioComponent> ent, ref ComponentRemove args)
    {
        _tracked.Remove(ent);
        _localAux.Remove(ent);
    }

    /// <summary>
    /// Manages local auxiliary slots for loaded audio entities.
    /// </summary>
    private void UpdateAuxMappings()
    {
        if (_tracked.Count == 0 || _player.LocalEntity is not { Valid: true })
            return;

        for (var i = _tracked.Count - 1; i >= 0; i--)
        {
            var ent = _tracked[i];
            if (!Exists(ent) || TerminatingOrDeleted(ent))
            {
                _tracked.RemoveAt(i);
                _localAux.Remove(ent);
                continue;
            }

            var comp = ent.Comp;
            if (comp.Global || (comp.Flags & AudioFlags.NoOcclusion) != 0 || !comp.Loaded)
                continue;

            EnsureLocalAux(ent);
            ApplyAuxiliaryToSource(ent);
        }
    }

    /// <summary>
    /// Handles the per stream audio tick.
    /// </summary>
    private void OnProcessStream(EntityUid entity, AudioComponent component, TransformComponent xform, MapCoordinates listener)
    {
        var shouldStart = !component.Started;
        if (shouldStart)
        {
            component.Started = true;
        }

        ApplyStreamMuffling(entity, component, xform, listener);

        if (shouldStart)
        {
            component.StartPlaying();
        }
    }

    /// <summary>
    /// Calculates final volume and occlusion based on insulation.
    /// </summary>
    private void ApplyStreamMuffling(EntityUid entity, AudioComponent component, TransformComponent xform, MapCoordinates listener)
    {
        // If insulation is globally disabled, skip tracing entirely and restore default audio
        if (!_insulationEnabled)
        {
            component.Occlusion = 0f;
            component.Volume = component.Params.Volume;
            return;
        }

        if (component.Global)
        {
            if (xform.MapID != MapId.Nullspace && listener.MapId != xform.MapID)
            {
                component.Gain = 0f;
                return;
            }

            component.Volume = component.Params.Volume;
            return;
        }

        if (listener.MapId != xform.MapID)
        {
            component.Gain = 0f;
            return;
        }

        Vector2 worldPos;
        if ((component.Flags & AudioFlags.GridAudio) != 0x0)
        {
            worldPos = _maps.GetGridPosition(xform.ParentUid);
        }
        else
        {
            worldPos = _xform.GetWorldPosition(entity);
        }

        var delta = worldPos - listener.Position;
        var distance = delta.Length();

        if (_audio.GetAudioDistance(distance) > component.MaxDistance)
        {
            component.Gain = 0f;
            return;
        }

        if (distance > 0f && distance < 0.01f)
            worldPos = listener.Position;

        component.Position = worldPos;

        if (_physicsQuery.TryGetComponent(xform.ParentUid, out var phys))
            component.Velocity = _physics.GetMapLinearVelocity(xform.ParentUid, phys);

        var local = _player.LocalEntity;
        if (local is not { Valid: true } listenerEnt)
        {
            component.Occlusion = 0f;
            component.Volume = component.Params.Volume;
            return;
        }

        if (HasComp<IgnoreSoundInsulationComponent>(listenerEnt))
        {
            component.Occlusion = 0f;
            component.Volume = component.Params.Volume;
            return;
        }

        if (HasComp<DeafnessComponent>(listenerEnt))
        {
            component.Gain = 0f;
            component.Volume = SilentVolumeDb;
            component.Occlusion = DeafOcclusion;
            return;
        }

        var trace = _rayTracer.Trace(entity, listenerEnt, xform.MapID);

        // If the sound did not reach the listener, mute it completely
        var insulation = trace.ReachedListener ? trace.TotalAbsorption : 1f;

        if (insulation >= FullyMuffledThreshold)
        {
            component.Gain = 0f;
            component.Volume = SilentVolumeDb;
            component.Occlusion = DeafOcclusion;
            return;
        }

        component.Occlusion = MathF.Min(insulation * OcclusionMultiplier, MaxOcclusion);

        var totalVolume = component.Params.Volume - insulation * MaxVolumeAttenuationDb;

        if (IsInVacuum(entity, listenerEnt))
            totalVolume += SharedDynamicAudioSystem.SpaceVolumePenaltyDb;

        component.Volume = totalVolume;
    }

    /// <summary>
    /// Creates the local auxiliary slot if it is missing.
    /// </summary>
    private void EnsureLocalAux(Entity<AudioComponent> ent)
    {
        if (!_audioEffectsEnabled)
            return;

        if (_localAux.TryGetValue(ent.Owner, out var existing) && Exists(existing))
            return;

        var aux = _dynamicAudio.GetOrCreateAuxiliaryFor(ent);
        if (aux is { } auxUid)
            _localAux[ent.Owner] = auxUid;
    }

    /// <summary>
    /// Applies the auxiliary slot to the OpenAL source.
    /// </summary>
    private void ApplyAuxiliaryToSource(Entity<AudioComponent> ent)
    {
        var source = (IAudioSource)ent.Comp;
        if (!_audioEffectsEnabled)
        {
            source.SetAuxiliary(null);
            return;
        }

        if (_localAux.TryGetValue(ent.Owner, out var auxUid) && TryComp<AudioAuxiliaryComponent>(auxUid, out var auxComp))
        {
            source.SetAuxiliary(auxComp.Auxiliary);
        }
        else
        {
            source.SetAuxiliary(null);
        }
    }

    /// <summary>
    /// Checks if the sound or player is in a vacuum.
    /// </summary>
    private bool IsInVacuum(EntityUid audioUid, EntityUid listener)
    {
        if (HasComp<InBarotraumaAudioEffectComponent>(audioUid))
            return true;

        if (HasComp<InBarotraumaAudioEffectComponent>(listener))
            return true;

        if (Transform(audioUid).GridUid is null)
            return true;

        return false;
    }
}
