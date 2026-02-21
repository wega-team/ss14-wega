using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Sharpening.Events;

[Serializable, NetSerializable]
public sealed class SharpeningFinishedEvent : EntityEventArgs
{
    public NetEntity Sharpener;
    public NetEntity Sharpening;

    public SharpeningFinishedEvent(NetEntity sharpener, NetEntity sharpening)
    {
        Sharpener = sharpener;
        Sharpening = sharpening;
    }
}
