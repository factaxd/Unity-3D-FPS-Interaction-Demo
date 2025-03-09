using UnityEngine;

[CreateAssetMenu(fileName = "InteractionTextData", menuName = "Interaction/Text Data")]
public class InteractionTextData : ScriptableObject
{
    [Header("Pickup Interactions")]
    [Tooltip("Text displayed when looking at a pickable object")]
    public string pickupText = "Press [E] to pick up {0}";
    
    [Tooltip("Text displayed when looking at an attached object")]
    public string detachText = "Press [E] to detach {0}";
    
    [Header("Placement Interactions")]
    [Tooltip("Text displayed when holding an object")]
    public string dropText = "Press [E] to drop {0}";
    
    [Tooltip("Text displayed when looking at a surface while holding an object")]
    public string placeText = "Press [E] to place {0}";
    
    [Header("Attachment Interactions")]
    [Tooltip("Text displayed when looking at an attachment point")]
    public string attachText = "Press [E] to attach {0}";
    
    [Tooltip("Text displayed when looking at an object with attachment points")]
    public string attachToObjectText = "Press [E] to attach {0} to {1}";
    
    [Header("Error Messages")]
    [Tooltip("Text displayed when cannot place an object")]
    public string cannotPlaceText = "Cannot place here, try another spot";
} 