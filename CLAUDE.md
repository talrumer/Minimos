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

## Project Overview
- **Minimos** — a P2P multiplayer party game built in Unity
- Stylized, cartoonish, low-poly aesthetic with pastel team colors
- Multiple competitive mini-games (e.g., Capture the Flag, etc.)
- Team-based (2-6 teams, 2 players per team)
- Character customization with skins and attachments

## Tech Stack
- Unity (URP rendering pipeline)
- Netcode for GameObjects (multiplayer)
- Unity Input System
- Cinemachine (camera)
- Unity Transport (networking)

## Conventions
- C# scripts follow Unity naming conventions (PascalCase for public, camelCase for private)
- Game logic lives under `Assets/Minimos/`
- Keep existing example assets under `Assets/Shooter/`, `Assets/Platformer/`, `Assets/Core/` as reference
