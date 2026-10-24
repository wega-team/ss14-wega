using System.Numerics;
using System.Text;
using Content.Shared.Audio.Components;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Physics;
using Content.Shared.Resomi.Abilities.Hearing;
using JetBrains.Annotations;
using Robust.Shared.Configuration;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;

namespace Content.Shared.Audio;

/// <summary>
/// Provides shared logic for sound insulation including chat obfuscation and basic server side direct raycasts.
/// </summary>
public abstract partial class SharedSoundInsulationSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private IRobustRandom _random = default!;

    private const int SoundOpaqueMask = (int)CollisionGroup.SoundOpaque;

    /// <summary>
    /// Returns the total insulation between the source and the listener.
    /// A value of zero means the path is clear and a value of one means it is fully blocked.
    /// Deaf listeners always receive one.
    /// </summary>
    public float GetSoundInsulation(EntityUid source, EntityUid listener)
    {
        if (source == listener)
            return 0f;

        if (HasComp<DeafnessComponent>(listener))
            return 1f;

        if (!_cfg.GetCVar(WegaCVars.SoundInsulationEnabled))
            return 0f;

        var sourceXform = Transform(source);
        var listenerXform = Transform(listener);

        if (sourceXform.MapID != listenerXform.MapID)
            return 1f;

        var sourcePos = _transform.GetWorldPosition(sourceXform);
        var listenerPos = _transform.GetWorldPosition(listenerXform);
        var distance = (listenerPos - sourcePos).Length();

        if (distance <= 0.5f)
            return 0f;

        if (TryComp<ListenUpComponent>(listener, out var listen) && distance <= listen.Radius)
            return 0f;

        return GetSoundInsulation(source, listener, sourcePos, listenerPos);
    }

    /// <summary>
    /// Performs a simple direct raycast for server side logic like chat obfuscation.
    /// The client system overrides this calculation via its own tracer.
    /// </summary>
    private float GetSoundInsulation(EntityUid source, EntityUid listener, Vector2 sourcePos, Vector2 listenerPos)
    {
        var direction = listenerPos - sourcePos;
        var distance = direction.Length();

        if (distance <= 0.1f)
            return 0f;

        var ray = new CollisionRay(sourcePos, direction.Normalized(), SoundOpaqueMask);
        var results = _physics.IntersectRay(Transform(source).MapID, ray, distance, source, false);

        var total = 0f;
        foreach (var hit in results)
        {
            if (TryComp<SoundInsulationComponent>(hit.HitEntity, out var insulation) && insulation.Isolates)
            {
                if (insulation.BlocksTransmission)
                    return 1f;

                total += insulation.AbsorptionFactor;
                if (total >= 1f)
                    return 1f;
            }
        }

        return Math.Clamp(total, 0f, 1f);
    }

    /// <summary>
    /// Toggles the insulation state on a specific component.
    /// </summary>
    [PublicAPI]
    public void ToggleInsulation(EntityUid uid, SoundInsulationComponent? insulation = null)
    {
        if (!Resolve(uid, ref insulation))
            return;

        insulation.Isolates = !insulation.Isolates;
        Dirty(uid, insulation);
    }

    /// <summary>
    /// Sets the insulation state to a specific value.
    /// </summary>
    [PublicAPI]
    public void SetInsulation(EntityUid uid, bool isolates, SoundInsulationComponent? insulation = null)
    {
        if (!Resolve(uid, ref insulation))
            return;

        insulation.Isolates = isolates;
        Dirty(uid, insulation);
    }

    #region Chat Helpers

    /// <summary>
    /// Replaces characters in a message based on the provided insulation chance.
    /// </summary>
    [PublicAPI]
    public string ObfuscateMessageByInsulation(string message, float chance)
    {
        var modifiedMessage = new StringBuilder(message);

        for (var i = 0; i < message.Length; i++)
        {
            if (char.IsWhiteSpace(modifiedMessage[i]))
                continue;

            if (_random.Prob(chance))
                modifiedMessage[i] = '~';
        }

        return modifiedMessage.ToString();
    }

    #endregion
}
