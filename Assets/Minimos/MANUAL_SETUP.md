# ⚠️ MANUAL SETUP — Unity Editor Steps

> Follow these steps in order after opening the project.

---

## 📦 Step 1: Verify Packages

**Window → Package Manager**

These should already be installed from the template:
- ✅ Netcode for GameObjects (2.x)
- ✅ Unity Transport (2.x)
- ✅ Input System (1.x)
- ✅ Cinemachine (3.x)
- ✅ Multiplayer Services (`com.unity.services.multiplayer`) — **this replaces Relay + Lobby**
- ✅ TextMesh Pro
- ✅ Visual Effect Graph

> ❌ Do **NOT** install `com.unity.services.relay` or `com.unity.services.lobby` separately — they conflict with the Multiplayer Services SDK which already includes their functionality via the Sessions API.

---

## 📁 Step 2: Generate Input Actions C# Class

1. Navigate to `Assets/Minimos/Input/MinimosInputActions.inputactions`
2. Select it in the Inspector
3. Check **"Generate C# Class"**
4. Set namespace to: `Minimos.Input`
5. Click **Apply**

---

## 🎬 Step 3: Create Scenes

Create these scenes in `Assets/Minimos/Scenes/` and add them to **Build Settings** in this order:

| Index | Scene Name | Purpose |
|---|---|---|
| 0 | `SplashScreen` | First thing on launch |
| 1 | `MainMenu` | After splash auto-transitions |
| 2 | `CharacterStudio` | Character customization |
| 3 | `Lobby` | Matchmaking / room code |
| 4 | `Gameplay` | Mini-game play area |
| 5 | `Results` | Final podium / scores |

---

## 🚀 Step 4: Run Setup Wizard (One Click!)

**Minimos → Setup Wizard → 🚀 Run Full Setup (All Steps)**

This automatically creates:
- ✅ 6 TeamData ScriptableObjects (with correct hex colors)
- ✅ SFXLibrary and MusicLibrary assets
- ✅ AnnouncerConfig asset
- ✅ CTF and KOTH MiniGameConfig assets
- ✅ 4 PowerUpConfig assets (SpeedBoost, MegaPunch, BuddyShield, FreezeBomb)
- ✅ GameBootstrap prefab (with all manager components wired)
- ✅ MinimoPlayer prefab (with all scripts, attachment points, team color material)
- ✅ Projectile prefab (with NetworkObject, trail renderer)

> 💡 You can also run individual steps via **Minimos → Setup Wizard → ...** if needed.

**After running the wizard:**
1. Drag `Prefabs/Network/GameBootstrap` into the **SplashScreen** scene
2. Add `Prefabs/Player/MinimoPlayer` to **NetworkManager's Network Prefabs list**
3. Add `Prefabs/Player/Projectile` to **NetworkManager's Network Prefabs list**

---

## 📷 Step 5: Camera Setup

In the Gameplay scene:
1. Create 5 Cinemachine cameras (GameObject → Cinemachine → Cinemachine Camera):
   - `CM_FollowCam` — 3rd person follow, ~45° angle
   - `CM_ArenaCam` — high isometric view of full arena
   - `CM_SideScrollCam` — side view with horizontal/vertical follow
   - `CM_SportsCam` — tracks a target (ball) with broadcast angle
   - `CM_SplitZoneCam` — positioned to show player's team zone
2. Assign all 5 to `CameraManager` component references
3. Add `CinemachineImpulseSource` to main camera for screen shake

---

## 🖥️ Step 6: UI Setup

### MainMenu Scene:
1. Create Canvas with: Play, Character Studio, Settings, Quit buttons + profile display
2. Attach `MainMenuController` and wire button references

### Lobby Scene:
1. Create Canvas with lobby UI (see `LobbyUIController` serialized fields)

### Gameplay Scene:
1. Create HUD Canvas with timer, score bars, cooldowns, minimap, event popup
2. Attach `GameHUD` and wire references

### Results Scene:
1. Create results UI canvas + 3D podium objects
2. Attach `PodiumController` and `ResultsScreenController`

---

## 🌐 Step 7: Unity Services Setup

1. **Edit → Project Settings → Services**
2. Link to your Unity Dashboard project
3. Sessions API handles auth + relay + lobby automatically
4. In code: `UnityServices.InitializeAsync()` + `AuthenticationService.Instance.SignInAnonymouslyAsync()`

---

## 🔥 Step 8: Firebase SDK (Optional — skip for now)

The project works without Firebase using `MockFirebaseService`. When ready:
1. Download Firebase Unity SDK from https://firebase.google.com/docs/unity/setup
2. Import `FirebaseAuth.unitypackage` and `FirebaseFirestore.unitypackage`
3. Add `FIREBASE_AVAILABLE` to **Player Settings → Scripting Define Symbols**

---

## 🗺️ Step 9: Map Setup (Sunny Meadows)

1. Create terrain or flat plane in Gameplay scene
2. Use the imported **Low-Poly Simple Nature Pack** assets
3. Dress the map: trees, rocks, grass, flowers, fences
4. Apply **Fantasy Skybox** (Window → Rendering → Lighting → Environment)
5. Create team spawn zones + power-up spawn points

---

## ✅ Step 10: Build Settings

1. **File → Build Settings** → verify scene order (SplashScreen = 0)
2. Platform: **PC, Mac & Linux Standalone**
3. **Player Settings:** Company: Pexon, Product: Minimos, Color Space: Linear

---

> 💡 **Tip:** Steps 1-4 get you a compiling project with all data wired. Steps 5-6 (cameras + UI) are needed before you can play. Audio clips can be populated later.
