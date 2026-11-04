using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Genetics;

[Serializable, NetSerializable]
public sealed partial class UniqueIdentifiersData
{
    // Colors stored as 0xRRGGBB
    public uint SkinTone { get; set; } = 0; // 0-100
    public uint FurColor { get; set; } = 0;
    public uint EyeColor { get; set; } = 0;

    // Arrays for markings that may have multiple sprite layers
    public uint[] HairColor { get; set; } = new uint[0];
    public uint[] BeardColor { get; set; } = new uint[0];
    public uint[] HeadAccessoryColor { get; set; } = new uint[0];
    public uint[] HeadMarkingColor { get; set; } = new uint[0];
    public uint[] BodyMarkingColor { get; set; } = new uint[0];
    public uint[] TailMarkingColor { get; set; } = new uint[0];
    public uint[] TailOverlayColor { get; set; } = new uint[0];
    public uint[] SnoutColor { get; set; } = new uint[0];
    public uint[] SnoutCoverColor { get; set; } = new uint[0];
    public uint[] HeadSideColor { get; set; } = new uint[0];

    // Styles stored as prototype IDs
    public ProtoId<MarkingPrototype> HairStyle { get; set; } = string.Empty;
    public ProtoId<MarkingPrototype> BeardStyle { get; set; } = string.Empty;
    public ProtoId<MarkingPrototype> HeadAccessoryStyle { get; set; } = string.Empty;
    public ProtoId<MarkingPrototype> HeadMarkingStyle { get; set; } = string.Empty;
    public ProtoId<MarkingPrototype> BodyMarkingStyle { get; set; } = string.Empty;
    public ProtoId<MarkingPrototype> TailMarkingStyle { get; set; } = string.Empty;
    public ProtoId<MarkingPrototype> TailOverlayStyle { get; set; } = string.Empty;
    public ProtoId<MarkingPrototype> SnoutStyle { get; set; } = string.Empty;
    public ProtoId<MarkingPrototype> SnoutCoverStyle { get; set; } = string.Empty;
    public ProtoId<MarkingPrototype> HeadSideStyle { get; set; } = string.Empty;

    public Sex Sex { get; set; } = Sex.Unsexed;

    public UniqueIdentifiersData Clone()
    {
        return new UniqueIdentifiersData
        {
            HairColor = (uint[])HairColor.Clone(),
            BeardColor = (uint[])BeardColor.Clone(),
            HeadAccessoryColor = (uint[])HeadAccessoryColor.Clone(),
            HeadMarkingColor = (uint[])HeadMarkingColor.Clone(),
            BodyMarkingColor = (uint[])BodyMarkingColor.Clone(),
            TailMarkingColor = (uint[])TailMarkingColor.Clone(),
            TailOverlayColor = (uint[])TailOverlayColor.Clone(),
            SnoutColor = (uint[])SnoutColor.Clone(),
            SnoutCoverColor = (uint[])SnoutCoverColor.Clone(),
            HeadSideColor = (uint[])HeadSideColor.Clone(),
            SkinTone = SkinTone,
            FurColor = FurColor,
            EyeColor = EyeColor,
            HairStyle = HairStyle,
            BeardStyle = BeardStyle,
            HeadAccessoryStyle = HeadAccessoryStyle,
            HeadMarkingStyle = HeadMarkingStyle,
            BodyMarkingStyle = BodyMarkingStyle,
            TailMarkingStyle = TailMarkingStyle,
            TailOverlayStyle = TailOverlayStyle,
            SnoutStyle = SnoutStyle,
            SnoutCoverStyle = SnoutCoverStyle,
            HeadSideStyle = HeadSideStyle,
            Sex = Sex
        };
    }
}
