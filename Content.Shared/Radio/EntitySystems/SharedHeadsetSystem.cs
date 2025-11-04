using Content.Shared.Emp;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Radio.Components;
using Content.Shared.Verbs; // Corvax-Wega-Headset

namespace Content.Shared.Radio.EntitySystems;

public abstract class SharedHeadsetSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HeadsetComponent, InventoryRelayedEvent<GetDefaultRadioChannelEvent>>(OnGetDefault);
        SubscribeLocalEvent<HeadsetComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<HeadsetComponent, GotUnequippedEvent>(OnGotUnequipped);
        SubscribeLocalEvent<HeadsetComponent, EmpPulseEvent>(OnEmpPulse);
        SubscribeLocalEvent<HeadsetComponent, GetVerbsEvent<Verb>>(OnGetVerbs); // Corvax-Wega-Headset
    }

    private void OnGetDefault(EntityUid uid, HeadsetComponent component, InventoryRelayedEvent<GetDefaultRadioChannelEvent> args)
    {
        if (!component.Enabled || !component.IsEquipped)
        {
            // don't provide default channels from pocket slots.
            return;
        }

        if (TryComp(uid, out EncryptionKeyHolderComponent? keyHolder))
            args.Args.Channel ??= keyHolder.DefaultChannel;
    }

    protected virtual void OnGotEquipped(EntityUid uid, HeadsetComponent component, GotEquippedEvent args)
    {
        component.IsEquipped = args.SlotFlags.HasFlag(component.RequiredSlot);
        Dirty(uid, component);
    }

    protected virtual void OnGotUnequipped(EntityUid uid, HeadsetComponent component, GotUnequippedEvent args)
    {
        component.IsEquipped = false;
        Dirty(uid, component);
    }

    private void OnEmpPulse(Entity<HeadsetComponent> ent, ref EmpPulseEvent args)
    {
        if (ent.Comp.Enabled)
        {
            args.Affected = true;
            args.Disabled = true;
        }
    }

    // Corvax-Wega-Headset-start
    private void OnGetVerbs(EntityUid uid, HeadsetComponent component, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanInteract || !args.CanComplexInteract || !args.CanAccess)
            return;

        var verb = new Verb
        {
            Text = component.ToggledSound
                ? Loc.GetString("verb-common-toggle-headset-disabled")
                : Loc.GetString("verb-common-toggle-headset-enabled"),
            Priority = 1,
            Category = VerbCategory.ToggleHeadsetSound,
            Act = () => ToggleHeadsetSound(uid, component)
        };

        args.Verbs.Add(verb);
    }

    /// <summary>
    /// Toggles the ToggledSound property on the headset.
    /// </summary>
    private void ToggleHeadsetSound(EntityUid uid, HeadsetComponent component)
    {
        if (component.ToggledSound)
            component.ToggledSound = false;
        else
            component.ToggledSound = true;
    }
    // Corvax-Wega-Headset-end
}
