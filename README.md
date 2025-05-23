# Unity 3D FPS Interaction Demo

## Description

This project is a demonstration of a first-person player controller with interactive object capabilities in Unity 3D. It showcases basic FPS movement, object highlighting, picking up, dropping, and attaching objects to designated points. The system also provides dynamic interaction text prompts to guide the player.

## Features

*   **FPS Player Movement:** Standard first-person controls including walking, running, jumping, and mouse-based camera look.
*   **Object Interaction:**
    *   **Highlighting:** Objects that can be interacted with are highlighted with an outline when the player looks at them.
    *   **Picking Up & Dropping:** Players can pick up `InteractableObject`s and carry them. They can then drop these objects freely in the environment.
    *   **Placing:** Held objects can be placed on valid surfaces.
    *   **Attaching:** `InteractableObject`s can be attached to predefined `AttachmentPoint`s on other objects.
    *   **Detaching:** Attached objects can be detached and picked up again.
*   **Dynamic Interaction Text:** UI text prompts update based on the context of the interaction (e.g., "Press [E] to pick up {ObjectName}", "Press [E] to attach {ObjectName}").
*   **Attachment System:** A flexible system allowing objects to have multiple `AttachmentPoint`s, each with optional tag-based filtering for what can be attached.
*   **Gizmos for Attachment Points:** Editor gizmos to visualize `AttachmentPoint`s, their expected object size, and rotation, aiding in level design.

## Potential Future Improvements

*   Different types of interactions (e.g., puzzles, levers, buttons).
*   Sound effects for all interactions.

![Ekran görüntüsü 2025-05-23 134317](https://github.com/user-attachments/assets/5c176823-fe2e-4cf4-bde4-d5b27681da62)
![Ekran görüntüsü 2025-05-23 134341](https://github.com/user-attachments/assets/da3f0788-d73a-4f52-af9d-3abdf12403c0)
