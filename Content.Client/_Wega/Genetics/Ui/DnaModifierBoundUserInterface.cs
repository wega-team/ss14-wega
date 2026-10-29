using Content.Shared.Containers.ItemSlots;
using Content.Shared.Genetics;
using Content.Shared.Genetics.UI;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Wega.Genetics.Ui;

[UsedImplicitly]
public sealed partial class DnaModifierBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private DnaModifierWindow? _window;

    public DnaModifierBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<DnaModifierWindow>();

        _window.EjectButtonDisk.OnPressed += _ => SendMessage(
            new ItemSlotButtonPressedEvent(SharedDnaModifier.DiskSlotName));

        _window.OnEjectRequested += () => SendMessage(new DnaModifierConsoleEjectEvent());
        _window.OnEjectRejuveRequested += () => SendMessage(new DnaModifierConsoleEjectRejuveEvent());
        _window.OnClearDiskRequested += () => SendMessage(new DnaModifierConsoleClearDiskEvent());

        _window.OnReleverationRequested += (tab, block, value, intensity) =>
            SendMessage(new DnaModifierConsoleReleverationEvent(tab, block, value, intensity));
        _window.OnReleverationsRequested += (tab, intensity, duration) =>
            SendMessage(new DnaModifierConsoleReleverationsEvent(tab, intensity, duration));

        _window.OnInjectorRequested += index =>
            SendMessage(new DnaModifierConsoleInjectorEvent(index));
        _window.OnInjectBlockRequested += (index, block) =>
            SendMessage(new DnaModifierInjectBlockEvent(index, block));
        _window.OnSubjectInjectRequested += index =>
            SendMessage(new DnaModifierConsoleSubjectInjectEvent(index));

        _window.OnClearBufferRequested += index =>
            SendMessage(new DnaModifierConsoleClearBufferEvent(index));
        _window.OnRenameBufferRequested += index =>
            SendMessage(new DnaModifierConsoleRenameBufferEvent(index));
        _window.OnExportOnDiskRequested += index =>
            SendMessage(new DnaModifierConsoleExportOnDiskEvent(index));
        _window.OnExportFromDiskRequested += index =>
            SendMessage(new DnaModifierConsoleExportFromDiskEvent(index));

        _window.OnSaveServerRequested += (section, type) =>
            SendMessage(new DnaModifierConsoleSaveServerEvent(section, type));

        _window.OnReagentButtonRequested += (amount, reagent) =>
            SendMessage(new DnaModifierConsoleReagentButtonEvent(amount, reagent));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        _window?.UpdateState((DnaModifierBoundUserInterfaceState)state);
    }
}
