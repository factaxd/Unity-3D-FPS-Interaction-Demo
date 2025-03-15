using UnityEngine;

namespace KeyboardSim.Data
{
    /// <summary>
    /// Scriptable object that stores text templates for player interactions.
    /// Follows SRP by only handling text data storage.
    /// </summary>
    [CreateAssetMenu(fileName = "InteractionTextData", menuName = "KeyboardSim/Interaction Text Data")]
    public class InteractionTextData : ScriptableObject
    {
        [Header("Basic Interaction Templates")]
        [Tooltip("Format: {0} = object name")]
        [TextArea(2, 3)]
        public string pickupText = "Pickup {0}";

        [Tooltip("Format: {0} = object name")]
        [TextArea(2, 3)]
        public string dropText = "Drop {0}";

        [Tooltip("Format: {0} = object name")]
        [TextArea(2, 3)]
        public string placeText = "Place {0}";

        [Tooltip("Format: {0} = object name")]
        [TextArea(2, 3)]
        public string detachText = "Detach {0}";

        [Header("Advanced Interaction Templates")]
        [Tooltip("Format: {0} = held object name")]
        [TextArea(2, 3)]
        public string attachText = "Attach {0}";

        [Tooltip("Format: {0} = held object name, {1} = target object name")]
        [TextArea(2, 3)]
        public string attachToObjectText = "Attach {0} to {1}";

        [Tooltip("Format: {0} = object name")]
        [TextArea(2, 3)]
        public string examineText = "Examine {0}";

        [Header("Status Messages")]
        [TextArea(2, 3)]
        public string cannotAttachText = "Cannot attach";

        [TextArea(2, 3)]
        public string incompatibleComponentText = "Incompatible";

        [TextArea(2, 3)]
        public string slotOccupiedText = "Slot already occupied";
    }
} 