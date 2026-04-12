using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Eui;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Veil.Cult.UI;

[Serializable, NetSerializable]
public enum VeilAltarUiKey : byte
{
    Key
}

// Events
[Serializable, NetSerializable]
public sealed class VeilAltarSelectEnergyMessage : BoundUserInterfaceMessage
{
    public NetEntity User;

    public VeilAltarSelectEnergyMessage(NetEntity user)
    {
        User = user;
    }
}

[Serializable, NetSerializable]
public sealed class VeilAltarSelectOfferMessage : BoundUserInterfaceMessage
{
    public NetEntity Altar;

    public VeilAltarSelectOfferMessage(NetEntity altar)
    {
        Altar = altar;
    }
}

[Serializable, NetSerializable]
public sealed class VeilAltarState : BoundUserInterfaceState
{
	public readonly NetEntity User;
    public readonly NetEntity Altar;
    
	public VeilAltarState(NetEntity user, NetEntity altar)
	{
		User = user;
		Altar = altar;
	}
}