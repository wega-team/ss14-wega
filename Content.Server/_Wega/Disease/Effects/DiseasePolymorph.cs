using Content.Server.Polymorph.Systems;
using Content.Shared.Disease;
using Content.Shared.Polymorph;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Disease.Effects
{
    [UsedImplicitly]
    public sealed partial class DiseasePolymorph : DiseaseEffect
    {
        [DataField("polymorphId", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<PolymorphPrototype>))]
        [ViewVariables(VVAccess.ReadWrite)]
        public string PolymorphId = default!;

        [DataField("polymorphSound")]
        [ViewVariables(VVAccess.ReadWrite)]
        public SoundSpecifier? PolymorphSound;

        [DataField("polymorphMessage")]
        [ViewVariables(VVAccess.ReadWrite)]
        public string? PolymorphMessage;

        public override void Effect(DiseaseEffectArgs args)
        {
            var polymorphSystem = args.EntityManager.System<PolymorphSystem>();
            EntityUid? polyUid = polymorphSystem.PolymorphEntity(args.DiseasedEntity, PolymorphId);

            if (PolymorphSound != null && polyUid != null)
            {
                var audioSystem = args.EntityManager.System<SharedAudioSystem>();
                audioSystem.PlayGlobal(PolymorphSound, Filter.Pvs(polyUid.Value), true, AudioParams.Default);
            }

            if (PolymorphMessage != null && polyUid != null)
            {
                var popupSystem = args.EntityManager.System<SharedPopupSystem>();
                popupSystem.PopupEntity(Loc.GetString(PolymorphMessage), polyUid.Value, polyUid.Value, PopupType.Large);
            }
        }
    }
}
