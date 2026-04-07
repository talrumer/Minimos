# CLAUDE.md — Minimos

## Communication Style
- **Concise and direct** — no fluff, get to the point
- **Use emojis** for readability and visual scanning:
  - ✅ Correct / done / approved
  - ❌ Incorrect / failed / rejected
  - ⚠️ Warning or attention needed
  - 🚨 Error or critical issue
  - 💡 Tip or suggestion
  - 📝 Note or info
  - **⚠️ MANUAL ACTION** — prefix for any step requiring the user to do something manually in Unity/browser/etc.

## Automation First
- **Prefer Editor scripts over manual steps.** When something needs to be done in Unity (creating assets, prefabs, wiring references, etc.), write a C# Editor script under `Assets/Minimos/Scripts/Editor/` instead of asking the user to do it manually.
- Use `MenuItem` attributes so scripts are accessible via the **Minimos** menu in Unity.
- Use `SerializedObject` + `FindProperty` to set private `[SerializeField]` fields.
- Save prefab first with `PrefabUtility.SaveAsPrefabAsset`, then load it back to wire references.
- Only use **⚠️ MANUAL ACTION** for things that genuinely can't be automated (e.g., linking Unity Dashboard, dragging prefabs into scenes, visual scene design).

## Project Overview
- **Minimos** — a P2P multiplayer party game built in Unity
- Stylized, cartoonish, low-poly aesthetic with pastel team colors
- Multiple competitive mini-games (e.g., Capture the Flag, etc.)
- Team-based (2-6 teams, 2 players per team)
- Character customization with skins and attachments

## Tech Stack
- Unity 6+ (URP rendering pipeline)
- Netcode for GameObjects (multiplayer)
- Unity Multiplayer Services SDK — Sessions API (replaces old Relay + Lobby)
- Unity Input System
- Cinemachine 3.x (camera)
- Firebase (Auth + Firestore) — optional, `MockFirebaseService` works offline

## Conventions
- C# scripts follow Unity naming conventions (PascalCase for public, camelCase for private)
- Use `[SerializeField] private` for inspector-exposed fields
- Use `namespace Minimos.*` for all scripts (Core, Player, Teams, UI, etc.)
- Game logic lives under `Assets/Minimos/`
- Editor scripts live under `Assets/Minimos/Scripts/Editor/`
- Keep existing example assets under `Assets/Shooter/`, `Assets/Platformer/`, `Assets/Core/` as reference

## Key Docs
- `docs/GAME_DESIGN.md` — full game design document
- `docs/ASSET_SCOUTING.md` — asset packs (free + paid upgrade path)
- `Assets/Minimos/MANUAL_SETUP.md` — Unity Editor setup steps
