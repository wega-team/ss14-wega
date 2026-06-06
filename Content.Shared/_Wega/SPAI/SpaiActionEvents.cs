using Content.Shared.Actions;
using Robust.Shared.Serialization;

namespace Content.Shared._Wega.SPAI;

public sealed partial class SpaiToggleThermalVisionEvent : InstantActionEvent;

public sealed partial class SpaiToggleSecurityHudEvent : InstantActionEvent;

public sealed partial class SpaiInstallChemInjectorEvent : InstantActionEvent;

public sealed partial class SpaiInstallWeakAiEvent : InstantActionEvent;

public sealed partial class SpaiToggleDoorEvent : EntityTargetActionEvent;

public sealed partial class SpaiAirlockBoltEvent : EntityTargetActionEvent;

public sealed partial class SpaiAirlockElectrifyEvent : EntityTargetActionEvent;


public sealed partial class SpaiAirlockEmergencyAccessEvent : EntityTargetActionEvent;

public sealed partial class SpaiApcDisableEvent : EntityTargetActionEvent;
