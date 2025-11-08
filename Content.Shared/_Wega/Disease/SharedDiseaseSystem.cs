using Content.Shared.Disease.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Content.Shared.Disease;

public abstract class SharedDiseaseSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ISerializationManager _serializationManager = default!;

    public Queue<EntityUid> AddQueue = new();

    /// <summary>
    /// Adds a disease to a target
    /// if it's not already in their current
    /// or past diseases. If you want this
    /// to not be guaranteed you are looking
    /// for TryInfect.
    /// </summary>
    public void TryAddDisease(EntityUid host, DiseasePrototype addedDisease, DiseaseCarrierComponent? target = null)
    {
        if (!Resolve(host, ref target))
            return;

        foreach (var disease in target.AllDiseases)
        {
            if (disease.ID == addedDisease?.ID)
                return;
        }

        var freshDisease = _serializationManager.CreateCopy(addedDisease);
        if (freshDisease == null)
            return;

        target.Diseases.Add(freshDisease);
        AddQueue.Enqueue(host);
    }

    public void TryAddDisease(EntityUid host, string? addedDisease, DiseaseCarrierComponent? target = null)
    {
        if (addedDisease == null || !_prototypeManager.TryIndex<DiseasePrototype>(addedDisease, out var added))
            return;

        TryAddDisease(host, added, target);
    }
}
