# AGENTS.md — Test-Surf-Swarm

## Project

Unity 2022.3 LTS (`2022.3.58f1c1`) project implementing 2D Boid flocking simulation based on Reynolds 1987 paper.

## Working with this repo

- **Editor required**: Most changes (scenes, prefabs, materials, ScriptableObjects) must be made inside the Unity Editor. CLI/scripting edits via the MCP tools or `*.asset` YAML patches are possible but scene files are fragile to hand-edit.
- **Solution file**: `Test-Surf-Swarm.sln` — VS Code is configured as the IDE (`.vscode/settings.json`).
- **Scene setup**: Use menu `Tools > Setup 2D Boid Scene` in Unity Editor to auto-generate the complete scene.
- **No build/test pipeline yet**: No CI, no test assemblies, no custom build scripts. When tests are added, run via Unity Test Runner (EditMode or PlayMode).
- **No git yet**: Project is not under version control. Initialize before any meaningful work.

## Key packages

- `com.coplaydev.unity-mcp` — MCP for Unity (AI editor bridge). Open via Window > MCP for Unity. Use Auto-Setup, then Start Bridge.
- `com.unity.visual_scripting` — Visual Scripting enabled.
- `com.unity.textmeshpro` — UI text rendering.
- Standard Unity modules (physics, AI, animation, UI, etc.) are all present.

## Conventions

- C# scripts go in `Assets/Scripts/` (create when needed).
- Scenes in `Assets/Scenes/`.
- Follow standard Unity naming: PascalCase for classes, `m_` prefix for serialized private fields.

## Project structure

```
Assets/
├── Prefabs/
│   └── Boid.prefab           # Boid prefab (Cube with Boid2D script)
├── Scripts/
│   ├── Boid2D.cs             # Single boid behavior (270° perception, 3 steering rules)
│   ├── BoidManager.cs        # Boid spawning, counting, boundary enforcement
│   ├── BoundaryRenderer.cs   # Red wireframe boundary (20x20)
│   ├── Camera2DController.cs # Orthographic follow camera
│   └── UI/
│       ├── UIManager.cs      # Panel toggle, pause, slider bindings
│       └── FlightTimer.cs    # MM:SS flight timer
└── Scenes/
    └── 2D_Swarm.unity
```

## MCP for Unity gotchas

Objects created via `unityMCP_execute_code` are NOT saved to the scene automatically. After creating GameObjects, you must call `EditorSceneManager.SaveOpenScenes()` or use `unityMCP_manage_scene(action=save)`. Otherwise objects disappear on domain reload.

The `Tools > Setup 2D Boid Scene` menu item has issues with prefab creation. Use `unityMCP_execute_code` to build the scene manually instead:
1. Create Boid prefab and save to `Assets/Prefabs/Boid.prefab`
2. Create BoidManager, Boundary, Camera, UI Canvas with all elements
3. Set all references programmatically
4. Save scene with `EditorSceneManager.SaveOpenScenes()`

Button onClick listeners added via `AddListener` at runtime are invisible to `GetPersistentEventCount()`. This is normal. Initialize UIManager in `Awake()` not `Start()` to ensure listeners register before the first frame.

## Boid behavior

- **Collision Avoidance**: Steer away from nearby boids within separation distance
- **Velocity Matching**: Match heading of nearby boids in 270° perception cone
- **Flock Centering**: Steer toward center of mass of nearby boids
- **Wall Warning**: Boids turn red when approaching boundaries (uses MaterialPropertyBlock with `_Color` property on Standard shader)
