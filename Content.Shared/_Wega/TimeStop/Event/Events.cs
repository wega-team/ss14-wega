using Content.Shared.Actions;

namespace Content.Shared.Magic.Events;

public sealed partial class TimeStopActionEvent : InstantActionEvent
{
    [DataField]
    public TimeSpan Time = TimeSpan.FromSeconds(7);
	
    [DataField]
    public TimeSpan TimePacified = TimeSpan.FromSeconds(4);
}