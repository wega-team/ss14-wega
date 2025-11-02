// Corvax-Wega-Full-Edit
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Crayon
{

    /// <summary>
    /// Component holding the state of a crayon-like component
    /// </summary>
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
    public sealed partial class CrayonComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [AutoNetworkedField]
        public string SelectedState = string.Empty;

        [DataField("color")]
        [AutoNetworkedField]
        public Color Color;

        [ViewVariables(VVAccess.ReadWrite)]
        [AutoNetworkedField]
        public Angle Angle;

        [DataField("useSound")]
        [AutoNetworkedField]
        public SoundSpecifier? UseSound;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("selectableColor")]
        [AutoNetworkedField]
        public bool SelectableColor;

        [ViewVariables(VVAccess.ReadWrite)]
        [AutoNetworkedField]
        public int Charges;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("capacity")]
        [AutoNetworkedField]
        public int Capacity = 30;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("deleteEmpty")]
        [AutoNetworkedField]
        public bool DeleteEmpty = true;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool UIUpdateNeeded;

        [Serializable, NetSerializable]
        public enum CrayonUiKey : byte
        {
            Key,
        }
    }

    /// <summary>
    /// Used by the client to notify the server about the selected decal ID
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class CrayonSelectMessage : BoundUserInterfaceMessage
    {
        public readonly string State;
        public CrayonSelectMessage(string selected)
        {
            State = selected;
        }
    }

    /// <summary>
    /// Sets the color of the crayon, used by Rainbow Crayon
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class CrayonColorMessage : BoundUserInterfaceMessage
    {
        public readonly Color Color;
        public CrayonColorMessage(Color color)
        {
            Color = color;
        }
    }

    /// <summary>
    /// Server to CLIENT. Notifies the BUI that a decal with given ID has been drawn.
    /// Allows the client UI to advance forward in the client-only ephemeral queue,
    /// preventing the crayon from becoming a magic text storage device.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class CrayonUsedMessage : BoundUserInterfaceMessage
    {
        public readonly string DrawnDecal;

        public CrayonUsedMessage(string drawn)
        {
            DrawnDecal = drawn;
        }
    }

    /// <summary>
    /// Component state, describes how many charges are left in the crayon in the near-hand UI
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class CrayonComponentState : ComponentState
    {
        public readonly Color Color;
        public readonly string State;
        public readonly int Charges;
        public readonly int Capacity;

        public CrayonComponentState(Color color, string state, int charges, int capacity)
        {
            Color = color;
            State = state;
            Charges = charges;
            Capacity = capacity;
        }
    }

    /// <summary>
    /// The state of the crayon UI as sent by the server
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class CrayonBoundUserInterfaceState : BoundUserInterfaceState
    {
        public string Selected;
        /// <summary>
        /// Whether or not the color can be selected
        /// </summary>
        public bool SelectableColor;
        public Color Color;

        public CrayonBoundUserInterfaceState(string selected, bool selectableColor, Color color)
        {
            Selected = selected;
            SelectableColor = selectableColor;
            Color = color;
        }
    }

    [Serializable, NetSerializable]
    public sealed class CrayonRotateEvent : EntityEventArgs
    {
        public readonly Angle Angle;

        public CrayonRotateEvent(Angle angle)
        {
            Angle = angle;
        }
    }
}
