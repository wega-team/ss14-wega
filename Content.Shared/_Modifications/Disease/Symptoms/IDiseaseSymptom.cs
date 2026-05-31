// Developed by Nox project.
// Author: KloopRe

using Content.Shared._Modifications.Disease.Components;
using Content.Shared._Modifications.Disease.Prototypes;
using Content.Shared._Modifications.TimeWindow;
using Robust.Shared.Prototypes;

namespace Content.Shared._Modifications.Disease.Symptoms;

public interface IDiseaseSymptom
{
    ProtoId<DiseaseSymptomPrototype> PrototypeId { get; }

    TimedWindow EffectTimedWindow { get; }

    /// <summary>
    ///     Вызывается при добавлении симптома.
    ///     ApplyDataEffect true.
    /// </summary>
    void OnAdded(EntityUid host, DiseaseComponent disease);

    /// <summary>
    ///     Периодически вызывается DiseaseSystem, для обновления симптома.
    /// </summary>
    void OnUpdate(EntityUid host, DiseaseComponent disease);

    /// <summary>
    ///     Вызывается при удалении симптома (например, излечение).
    ///     ApplyDataEffect false.
    /// </summary>
    void OnRemoved(EntityUid host, DiseaseComponent disease);

    /// <summary>
    ///     Запускает эффект симптома.
    /// </summary>
    void DoEffect(EntityUid host, DiseaseComponent disease);

    /// <summary>
    ///     Метод для передачи симптомов от одного носителя к другому.
    /// </summary>
    IDiseaseSymptom Clone();

    /// <summary>
    ///     Применяет эффект симптома к данным вируса.
    ///     data - данные вируса.
    ///     add - применить/убрать эффект?
    /// </summary>
    void ApplyDataEffect(DiseaseData data, bool add);
}
