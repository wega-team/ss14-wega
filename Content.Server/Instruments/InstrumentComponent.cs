using Content.Shared.Instruments;
using Robust.Shared.Prototypes; // Corvax-Wega-Harpy

namespace Content.Server.Instruments;

[RegisterComponent]
public sealed partial class InstrumentComponent : SharedInstrumentComponent
{
    [ViewVariables] public float Timer = 0f;
    [ViewVariables] public int BatchesDropped = 0;
    [ViewVariables] public int LaggedBatches = 0;
    [ViewVariables] public int MidiEventCount = 0;
    [ViewVariables] public uint LastSequencerTick = 0;
    [ViewVariables(VVAccess.ReadOnly)] public EntityUid? ActionUid = default!; // Corvax-Wega-Harpy
    public readonly EntProtoId Action = "ActionPlayInstrumentSelf"; // Corvax-Wega-Harpy
}
