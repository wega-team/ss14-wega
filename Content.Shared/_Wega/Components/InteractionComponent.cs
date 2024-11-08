using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using Robust.Shared.Utility;

namespace Content.Shared.Chat.Prototypes
{
    [Prototype("interaction")]
    public sealed partial class InteractionPrototype : IPrototype, IInheritingPrototype
    {
        [IdDataField]
        public string ID { get; private set; } = default!;

        /// <inheritdoc />
        [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<InteractionPrototype>))]
        public string[]? Parents { get; }

        /// <inheritdoc />
        [NeverPushInheritance]
        [AbstractDataField]
        public bool Abstract { get; }

        [DataField(required: true)]
        public string Name = default!;

        [DataField]
        public SpriteSpecifier Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/Actions/scream.png"));

        [DataField("useDelay")]
        public TimeSpan UseDelay { get; set; } = TimeSpan.FromSeconds(2);

        [DataField("userMessages")]
        public List<string> UserMessages = new();

        [DataField("targetMessages")]
        public List<string> TargetMessages = new();

        [DataField("otherMessages")]
        public List<string> OtherMessages = new();

        [DataField("interactSound")]
        public SoundSpecifier? InteractSound;

        [DataField("soundPerceivedByOthers")]
        public bool SoundPerceivedByOthers = true;

        [DataField("params")]
        public AudioParams? GeneralParams = new AudioParams { Variation = 0.125f };

        [DataField("erp")]
        public bool ERP { get; set; } = false;

        [DataField("points")]
        public int Points { get; set; } = 0;

        [DataField("delay")]
        public float DoAfterDelay { get; set; } = 0f;

        [DataField]
        public List<string>? RequiredClothingSlots;

        [DataField]
        public List<string>? OneRequiredClothingSlots;

        [DataField]
        public bool RequiresStrapon { get; set; } = false;

        [DataField]
        public List<string>? AllowedSpecies = new() { "all" };

        [DataField]
        public List<string>? AllowedGenders = new() { "all" };

        [DataField]
        public List<string>? NearestAllowedSpecies = new() { "all" };

        [DataField]
        public List<string>? NearestAllowedGenders = new() { "all" };

        [DataField]
        public List<string>? TargetEntityId;
    }
}
