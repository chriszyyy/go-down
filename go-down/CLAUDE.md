# Hex Drop — Claude Code Agent Guide

## Quick Reference

**Game:** 2D mobile puzzle — hexagonal ball balances on a tower of blocks. Tap blocks to destroy them; ball drops. Keep ball inside tower boundaries.

**Commercial name:** Hex Drop  
**Repo / codebase identifier:** `go-down` / `GoDown.*` (legacy internal name — do NOT rename folders, asmdefs, or namespaces; only the store-facing product name changed)  
**Bundle ID:** `com.chriszhang.hexdrop` (Android + iOS)  
**Unity Version:** 2022.3.62f3  
**Platform:** Android (primary), iOS (planned)

## MCP Integration

This project uses **MCP for Unity** (UnityMCP). Use MCP tools for:
- Reading scene hierarchy, editor state, project info
- Creating/modifying GameObjects, components, prefabs
- Running play mode, reading console logs
- Taking screenshots for visual verification

Always `read_console` after script edits to catch compile errors before proceeding.

## Assembly Rules (CRITICAL — will cause compile errors if broken)

```
GoDown.Core       → references NOTHING
GoDown.Managers   → references Core only
GoDown.UI         → references Managers + Core
GoDown.Gameplay   → references Managers + Core
GoDown.Visuals    → references NOTHING (accessed via reflection)
GoDown.Editor     → Editor-only
```

**Never** add cross-references that violate this graph. Visuals scripts are loaded via `System.Type.GetType("ClassName, GoDown.Visuals")`.

## Key Conventions

- **Language:** Code comments in Chinese (中文) to match existing style
- **UI Framework:** uGUI (`UnityEngine.UI.Text`) — no TextMeshPro  
- **Singletons:** Use `[RuntimeInitializeOnLoadMethod]` bootstrap pattern
- **Communication:** Static C# events (not UnityEvents) for cross-system messaging
- **Persistence:** `PlayerPrefs` with static helpers
- **Physics Layers:** Block=6, HexagonBall=7, Boundary=8 — do NOT change
- **Sprites:** PPU=64, top-right quadrant rendering, `COLLIDER_TOLERANCE = 0.015f`

## File Locations

| What | Where |
|------|-------|
| Block types | `Assets/Scripts/Core/` (TowerBlock.cs base + shape subclasses) |
| Tower generation | `Assets/Scripts/Managers/TowerBuilder.cs` |
| Background/visual layers | `Assets/Scripts/Managers/BackgroundController.cs` |
| Camera tracking | `Assets/Scripts/Managers/CameraFollower.cs` |
| Game state | `Assets/Scripts/Managers/GameStateManager.cs` |
| Score system | `Assets/Scripts/Managers/ScoreManager.cs` |
| UI panels | `Assets/Scripts/UI/` |
| Visual effects | `Assets/Scripts/Visuals/` + `Assets/Shaders/` |
| Prefabs | `Assets/Prefabs/Blocks/` |
| Design spec | `.github/agents/go-down.agent.md` |
| Dev roadmap | `development_tasks.md` |

## Background System Architecture

BackgroundController manages visual depth through two systems:
1. **Background color zones** — 10 gradient regions (outer space → lava) interpolated by camera Y
2. **Star particle system** — parallax stars that fade out approaching atmosphere (Y: -100 to -600)

Layer-specific visual effects are added as separate particle systems, each with their own Y-range fade logic.

## Workflow Tips

1. **Before editing scripts:** Read the file first. Check which assembly it belongs to.
2. **After editing scripts:** Wait for compile, then `read_console` to verify no errors.
3. **Adding components via script:** If the component type is in GoDown.Visuals, use reflection.
4. **Testing changes:** Use `manage_editor` play/stop to test in-editor.
5. **Scene changes:** The main (and only) scene is `Assets/Scenes/MainGameScene.unity`.
6. **New block types:** Follow the checklist in go-down.agent.md § "Adding a New Block Type".

## Current Focus

The BackgroundController is being enhanced with:
- Parallax star movement (stars drift up as ball descends)
- Random individual star fade-out/twinkle effects
- Layer-specific visual effects (nebula dust, clouds, dirt particles, lava embers)

## Don'ts

- Don't add Unity packages without asking
- Don't use TextMeshPro
- Don't modify physics layer assignments
- Don't break assembly definition boundaries
- Don't modify PrefabGenerator sprite conventions
