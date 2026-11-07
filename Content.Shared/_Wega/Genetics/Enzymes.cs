using System.Linq;
using Robust.Shared.Serialization;

namespace Content.Shared.Genetics;

[Serializable, NetSerializable]
public sealed partial class EnzymeInfo
{
    public string SampleName { get; set; } = string.Empty;
    public UniqueIdentifiersData? Identifier { get; set; }
    public List<EnzymesPrototypeInfo>? Info { get; set; }

    public object Clone() => new EnzymeInfo
    {
        SampleName = SampleName,
        Identifier = Identifier?.Clone(),
        Info = Info?.ToList()
    };
}

[Serializable, NetSerializable]
public partial record struct EnzymesPrototypeInfo
{
    public string EnzymesPrototypeId { get; set; }
    public int HexCode { get; set; } // 0x000..0xFFF
    public int Order { get; set; }

    public EnzymesPrototypeInfo()
    {
        EnzymesPrototypeId = string.Empty;
    }
}
