using Robust.Shared.Serialization;

namespace Content.Shared._Wega.SPAI;

[Serializable, NetSerializable]
public enum SpaiChemInjectorUiKey { Key }

[Serializable, NetSerializable]
public sealed record SpaiReagentEntry(string ReagentId, string LocalizedName, int EnergyCost);

[Serializable, NetSerializable]
public sealed class SpaiChemInjectorBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly int Energy;
    public readonly int EnergyMax;
    public readonly List<SpaiReagentEntry> Reagents;

    public SpaiChemInjectorBoundUserInterfaceState(int energy, int energyMax, List<SpaiReagentEntry> reagents)
    {
        Energy = energy;
        EnergyMax = energyMax;
        Reagents = reagents;
    }
}

[Serializable, NetSerializable]
public sealed class SpaiChemInjectorInjectMessage : BoundUserInterfaceMessage
{
    public readonly string ReagentId;
    public readonly int EnergyCost;

    public SpaiChemInjectorInjectMessage(string reagentId, int energyCost)
    {
        ReagentId = reagentId;
        EnergyCost = energyCost;
    }
}
