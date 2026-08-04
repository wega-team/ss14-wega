using Robust.Shared.Serialization;

namespace Content.Shared.Ninja;

[Serializable, NetSerializable]
public sealed class SpiderOSBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly int SuitGender;
    public readonly int SuitStyleVariant;
    public readonly int SuitColor;
    public readonly bool IsActivated;
    public readonly int[] AbilityChoices;
    public readonly string[]? ConsoleErrors;
    public readonly bool ShuttleLinked;
    public readonly int CoordX;
    public readonly int CoordY;
    public readonly bool InNullspace;
    public readonly bool IsLoading;
    public readonly string? TerminalLine;
    public readonly bool TerminalFinished;

    public SpiderOSBoundUserInterfaceState(
        int suitGender, int suitStyleVariant, int suitColor,
        bool isActivated, int[] abilityChoices, string[]? consoleErrors = null,
        bool shuttleLinked = false,
        int coordX = 0, int coordY = 0, bool inNullspace = true,
        bool isLoading = false, string? terminalLine = null, bool terminalFinished = false)
    {
        SuitGender       = suitGender;
        SuitStyleVariant = suitStyleVariant;
        SuitColor        = suitColor;
        IsActivated      = isActivated;
        AbilityChoices   = abilityChoices;
        ConsoleErrors    = consoleErrors;
        ShuttleLinked    = shuttleLinked;
        CoordX           = coordX;
        CoordY           = coordY;
        InNullspace      = inNullspace;
        IsLoading        = isLoading;
        TerminalLine     = terminalLine;
        TerminalFinished = terminalFinished;
    }
}

/// <summary>
/// Slot mapping: 0 = SuitGender, 1 = SuitStyleVariant, 2 = SuitColor
/// </summary>
[Serializable, NetSerializable]
public sealed class SpiderOSSetStyleMessage : BoundUserInterfaceMessage
{
    public readonly int Slot;
    public readonly int Index;

    public SpiderOSSetStyleMessage(int slot, int index)
    {
        Slot  = slot;
        Index = index;
    }
}

[Serializable, NetSerializable]
public sealed class SpiderOSActivateMessage : BoundUserInterfaceMessage { }

[Serializable, NetSerializable]
public sealed class SpiderOSSetAbilityMessage : BoundUserInterfaceMessage
{
    public readonly int Row;
    public readonly int Choice;

    public SpiderOSSetAbilityMessage(int row, int choice)
    {
        Row    = row;
        Choice = choice;
    }
}

[Serializable, NetSerializable]
public sealed class SpiderOSOpenShuttleConsoleMessage : BoundUserInterfaceMessage { }
