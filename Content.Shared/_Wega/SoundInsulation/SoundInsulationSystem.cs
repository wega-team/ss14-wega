using System.Text;
using Content.Shared.Resomi.Abilities.Hearing;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Physics;
using Robust.Shared.Configuration;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;

namespace Content.Shared.SoundInsolation;

public sealed class SoundInsulationSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public float GetSoundInsulation(EntityUid source, EntityUid listener)
    {
        if (source == listener)
            return 0f;

        // Because he doesn't hear anything.
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
        var direction = listenerPos - sourcePos;
        var distance = direction.Length();

        if (distance <= 0.5f)
            return 0f;

        if (TryComp<ListenUpComponent>(listener, out var listen) && distance <= listen.Radius)
            return 0f; // Chickens have good hearing.

        var normalizedDir = direction.Normalized();
        var ray = new CollisionRay(sourcePos, normalizedDir, (int)(CollisionGroup.WallLayer | CollisionGroup.AirlockLayer));
        var rayCastResults = _physics.IntersectRay(sourceXform.MapID, ray, distance, source, false);

        float totalInsulation = 0f;
        foreach (var result in rayCastResults)
        {
            if (TryComp<SoundInsulatorComponent>(result.HitEntity, out var insulator) && insulator.Isolates)
            {
                totalInsulation += insulator.InsulationFactor;
                if (totalInsulation >= 1f)
                    return 1f;
            }
        }

        return Math.Clamp(totalInsulation, 0f, 1f);
    }

    public void ToggleInsulation(EntityUid uid, SoundInsulatorComponent? insulator = null)
    {
        if (!Resolve(uid, ref insulator))
            return;

        insulator.Isolates = !insulator.Isolates;
        Dirty(uid, insulator);
    }

    public void SetInsulation(EntityUid uid, bool isolates, SoundInsulatorComponent? insulator = null)
    {
        if (!Resolve(uid, ref insulator))
            return;

        insulator.Isolates = isolates;
        Dirty(uid, insulator);
    }

    #region Chat Helpers

    public string ObfuscateMessageByInsulation(string message, float insulation)
    {
        return ObfuscateMessageReadability(message, insulation);
    }

    private string ObfuscateMessageReadability(string message, float chance)
    {
        var modifiedMessage = new StringBuilder(message);

        for (var i = 0; i < message.Length; i++)
        {
            if (char.IsWhiteSpace(modifiedMessage[i]))
            {
                continue;
            }

            if (_random.Prob(chance))
            {
                modifiedMessage[i] = '~';
            }
        }

        return modifiedMessage.ToString();
    }

    #endregion Chat Helpers
}
