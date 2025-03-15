# Keyboard Simulation Project Architecture

This document provides an overview of the code architecture and design principles used in the Keyboard Simulation project.

## Project Structure

The codebase is organized according to SRP (Single Responsibility Principle) with clear separation of concerns:

```
Scripts/
├── Core/
│   ├── Input/           # Input handling 
│   ├── Player/          # Player mechanics
│   └── Interaction/     # Object interaction system
├── Data/                # Scriptable objects and data containers
├── Managers/            # Game managers 
├── UI/                  # User interface scripts
└── Utils/               # Utility functions and helpers
```

## Design Principles

The codebase follows these key design principles:

1. **Single Responsibility Principle (SRP)**: Each class has one responsibility and one reason to change. For example, input handling is separated from player movement.

2. **Event-Driven Architecture**: Classes communicate via events rather than direct references, reducing tight coupling between components.

3. **Component-Based Design**: Functionality is split into focused components that can be composed together.

4. **Encapsulation**: Private fields with public property access where needed to maintain proper encapsulation.

5. **Clean Code**: Consistent naming conventions, comprehensive comments, and proper documentation.

## Key Systems

### Input System

The `InputManager` class provides a central place to handle all player inputs. It:
- Detects raw input (keyboard/mouse)
- Converts inputs into meaningful events
- Provides an event-based API for other systems to subscribe to

### Player System

Player functionality is divided into:
- `PlayerMovement`: Handles movement, jumping, and camera control
- `PlayerInteraction`: Manages interaction with game objects
- `ObjectHolder`: Handles picking up, carrying, and placing objects

### Interaction System

The interaction system is based on these components:
- `InteractableObject`: Base class for objects that can be interacted with
- `InteractionDetector`: Detects interactable objects in the scene
- `AttachmentPoint`: Places where objects can be attached
- `AttachmentPointManager`: Manages multiple attachment points on a single object

### Data Management

- `InteractionTextData`: Scriptable object for managing UI text templates

## Usage Examples

### Creating a New Interactable Object

1. Add the `InteractableObject` component to your GameObject
2. Configure its properties (name, can attach, etc.)
3. (Optional) For objects that can have parts attached to them, add an `AttachmentPointManager`
4. (Optional) Add `AttachmentPoint` components as children where parts can be attached

### Adding New Input Actions

1. Add new event definitions in the `InputManager` class
2. Add input detection code in the `Update()` method
3. Other components can subscribe to these events

## Best Practices

When extending the codebase:

1. Keep classes focused on a single responsibility
2. Use events for cross-component communication
3. Add proper XML documentation to public methods
4. Follow naming conventions (private fields with underscore prefix)
5. Group related functionality in the appropriate directories 