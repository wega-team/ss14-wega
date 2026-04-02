using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.Interaction;

/// <summary>
/// The sound playback effect in the PVS target.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class PlaySoundEffect : InteractionEffect
{
    /// <summary>
    /// The sound specification that will be played. <see cref="SoundSpecifier"/>
    /// </summary>
    [DataField(required: true)]
    public SoundSpecifier Sound { get; private set; } = default!;

    public override void Apply(EntityUid user, EntityUid target, IEntityManager entityManager)
    {
        var audioSystem = entityManager.System<SharedAudioSystem>();
        audioSystem.PlayPvs(Sound, target);
    }
}
