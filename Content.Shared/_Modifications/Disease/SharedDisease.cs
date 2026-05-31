// Developed by Nox project.
// Author: KloopRe

using Content.Shared.Inventory;
using Robust.Shared.Serialization;
using Content.Shared.DoAfter;
using Content.Shared.Actions;
using Content.Shared._Modifications.Disease.Components;

namespace Content.Shared._Modifications.Disease;

/// <summary>
///     Логика резистов: начальный коэффициент = 0 %.
///     Происходит агригированное суммирование резистов от всех источников, например: 10% маска + 50% костюм = 60%.
///     После от процента заразности, например: 50% отнимается процент резиста: 50 - 60 = -10.
///     И берётся значение в пределах от 0 до 100%, в рассматриваемом примере -10, т.е. 0%.
/// </summary>
public sealed class DiseaseResistanceQueryEvent : EntityEventArgs, IInventoryRelayEvent
{
    public SlotFlags TargetSlots { get; }
    public float TotalCoefficient = 0f;

    public DiseaseResistanceQueryEvent(SlotFlags slots)
    {
        TargetSlots = slots;
    }
}

/// <summary>
///     Событие вызывается после излечения сущности.
/// </summary>
public sealed class CureDiseaseEvent : EntityEventArgs
{
    public EntityUid Target { get; }

    public CureDiseaseEvent(EntityUid target)
    {
        Target = target;
    }
}

/// <summary>
///     Событие вызывает сразу после вызова метода попытки заражения.
/// </summary>
public sealed class ProbInfectAttemptEvent : EntityEventArgs
{
    public EntityUid Target { get; }
    public EntityUid? Host { get; }
    public bool Cancel { get; set; }

    public ProbInfectAttemptEvent(EntityUid target, bool cancel = false, EntityUid? host = null)
    {
        Target = target;
        Host = host;
        Cancel = cancel;
    }
}

/// <summary>
///     Событие вызывается после заражения сущности.
/// </summary>
public sealed class CauseDiseaseEvent : EntityEventArgs
{
    public DiseaseData SourceData { get; }

    public CauseDiseaseEvent(DiseaseData sourceData)
    {
        SourceData = sourceData;
    }
}

public sealed class EnterCryostorageEvent : EntityEventArgs
{

}

[NetSerializable, Serializable]
public enum DiseaseMutationVisuals : byte
{
    state,
    infected
}


[Serializable, NetSerializable]
public sealed partial class CollectDiseaseDataDoAfterEvent : SimpleDoAfterEvent
{ }


public sealed partial class ShopMutationActionEvent : InstantActionEvent
{

}

public sealed partial class TeleportToPrimaryPatientEvent : InstantActionEvent
{

}
public sealed partial class SelectPrimaryPatientEvent : EntityTargetActionEvent
{

}
