namespace Content.Shared.Strangulation
{
    [RegisterComponent]
    public sealed partial class StranglerComponent : Component
    {
        [DataField]
        public EntityUid? Target;

        [DataField("freeHandsRequired")]
        public int FreeHandsRequired = 2;
    }
}
