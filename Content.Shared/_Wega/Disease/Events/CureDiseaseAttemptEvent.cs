namespace Content.Shared.Disease.Events;

/// <summary>
/// This event is fired by chems
/// and other brute-force rather than
/// specific cures. It will roll the dice to attempt
/// to cure each disease on the target
/// </summary>
public sealed class CureDiseaseAttemptEvent : EntityEventArgs
{
    public float CureChance { get; }
    public CureDiseaseAttemptEvent(float cureChance)
    {
        CureChance = cureChance;
    }
}