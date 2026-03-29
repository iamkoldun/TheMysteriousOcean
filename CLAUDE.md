# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**The Mysterious Ocean** is a Unity 6 (6000.0.34f1) first-person survival exploration game. The player rides a self-propelling boat through a procedurally-generated ocean, scavenges resources from passing islands, manages energy via a heat generator and water pump, and swims between locations.

## Unity & MCP

This project uses the **UnityMCP** MCP server to interact with the Unity Editor directly. Use the Unity MCP tools to:
- Read/modify scripts, scenes, prefabs, and materials
- Run play mode, check console errors, manage GameObjects
- Always run `read_console` after modifying scripts to catch compilation errors before proceeding

## Architecture Overview

### Core Systems

**World Generation** (`IslandSpawnManager.cs`)
- Boat moves at 8 units/sec along a configurable axis
- Islands spawn every 80 units of travel from 3 prefab variants
- Ocean tiles spawn ahead (2 tiles) and despawn behind (1 tile) via `IslandDestroyer`
- `MovingIsland` + `SpawnedWorldObject` handle per-island movement and cleanup

**Player** (`FirstPersonController.cs`, `Stamina.cs`)
- Full FPS controller with walking, sprinting (stamina drain 25/sec), and swimming physics
- Swimming: ascend/sink/surface hop; ascending drains stamina (18/sec)
- `WaterVolume` triggers provide `SurfaceY`/`BottomY` for physics calculations
- Ground detection via raycast

**Inventory** (`Inventory.cs`, `PlayerInventory.cs`, `Item.cs`)
- Items are either Light (1 slot) or Heavy (2 slots, both hands)
- Two hand slots + expandable "Expansion" storage sections
- Pickup: E key (raycast, 3-unit range); Drop: Q key; Swap hands: F; Hotkeys: 1–9
- `OnInventoryChanged` event drives UI updates

**Survival** (`HeatGenerator.cs`, `WaterPump.cs`, `BurnableItem.cs`)
- Burnable items define energy yield; `HeatGenerator` stores up to 100 energy
- `WaterPump` consumes 5 energy/sec when toggled on (E key); drives water particle effect
- These systems connect through `PlayerInventory` which holds references to both

**UI** — prefab-based panels for Inventory, Hotbar, Stamina, CrosshairUI, and HeatGeneratorUI. All UI scripts live in `Assets/Scripts/`.

### Key Files Quick Reference

| File | Responsibility |
|------|---------------|
| `FirstPersonController.cs` | Player movement, swimming, camera look |
| `Stamina.cs` | Sprint/swim stamina resource |
| `Inventory.cs` | Inventory data model |
| `PlayerInventory.cs` | Input-driven inventory interactions |
| `Item.cs` | Base item: attach/detach/drop logic |
| `IslandSpawnManager.cs` | Procedural world generation |
| `HeatGenerator.cs` | Energy production |
| `WaterPump.cs` | Energy consumption, water mechanic |

## Scenes

- `Assets/Scenes/GameScene.unity` — primary game scene

## Git Branch Convention

- Working branch: `week1`
- Main branch: `main`

## Asset Conventions

- `.mat` files under `Assets/Maritime_Heritage/` and its sub-folders are the active material instances for the boat and heritage cargo
- Prefab assets live in `Assets/Prefabs/`; island variants are `Island 1–3`
