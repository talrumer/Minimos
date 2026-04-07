# ⚠️ SETUP GUIDE

---

## 🚀 One-Click Setup

**Minimos → Setup Wizard → 🚀 Run Full Setup (All Steps)**

This single click does EVERYTHING:

| What | Automated? |
|---|---|
| Create 6 scenes + add to Build Settings | ✅ |
| Configure Input Actions (generate C# class) | ✅ |
| Create 6 TeamData ScriptableObjects | ✅ |
| Create SFXLibrary + MusicLibrary + AnnouncerConfig | ✅ |
| Create CTF + KOTH MiniGameConfig assets | ✅ |
| Create 4 PowerUpConfig assets | ✅ |
| Create MinimoPlayer prefab (all scripts + attachment points) | ✅ |
| Create Projectile prefab | ✅ |
| Create GameBootstrap in SplashScreen (NetworkManager + all managers) | ✅ |
| Wire NetworkTransport + PlayerPrefab + NetworkPrefabsList | ✅ |
| Create 5 Cinemachine cameras in Gameplay scene | ✅ |
| Create basic UI canvases in MainMenu, Lobby, Gameplay, Results | ✅ |
| Configure Build Settings (scene order) + Player Settings | ✅ |

---

## ⚠️ Remaining Manual Steps (can't be automated)

These require browser login or creative design work:

### 1. Unity Services
- **Edit → Project Settings → Services** → link to Unity Dashboard project

### 2. Firebase SDK (optional — skip for now)
- Download from https://firebase.google.com/docs/unity/setup
- Import `FirebaseAuth.unitypackage` + `FirebaseFirestore.unitypackage`
- Add `FIREBASE_AVAILABLE` to Player Settings → Scripting Define Symbols

### 3. Map Design (Sunny Meadows)
- Design your first map in the Gameplay scene using imported asset packs
- Add team spawn points + power-up spawn points

### 4. Populate Audio (when ready)
- Add audio clips to SFXLibrary, MusicLibrary, AnnouncerConfig assets
