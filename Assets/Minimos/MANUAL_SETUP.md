# ⚠️ MANUAL SETUP — Unity Editor Steps

> These are the steps that **must be done in Unity Editor** to wire up the code foundation.
> Follow them in order after opening the project.

---

## 📦 Step 1: Package Manager — Verify/Install Dependencies

**Window → Package Manager → Unity Registry**

Ensure these are installed (most should be from the template):
- ✅ Netcode for GameObjects (2.x)
- ✅ Unity Transport (2.x)
- ✅ Input System (1.x)
- ✅ Cinemachine (3.x)
- ✅ TextMesh Pro
- ✅ Visual Effect Graph

Install if missing (search "Unity Registry"):
- ⚠️ **Multiplayer Services** (`com.unity.services.multiplayer`)
- ⚠️ **Relay** (`com.unity.services.relay`)
- ⚠️ **Lobby** (`com.unity.services.lobby`)
- ⚠️ **Authentication** (`com.unity.services.authentication`)

---

## 📁 Step 2: Generate Input Actions C# Class

1. Navigate to `Assets/Minimos/Input/MinimosInputActions.inputactions`
2. Select it in the Inspector
3. Check **"Generate C# Class"**
4. Set namespace to: `Minimos.Input`
5. Click **Apply**

This generates the `MinimosInputActions` class that `PlayerSetup.cs` references.

---

## 🎬 Step 3: Create Scenes

Create these scenes in `Assets/Minimos/Scenes/`:

| Scene Name | Purpose |
|---|---|
| `SplashScreen` | Logo + auto-transition |
| `MainMenu` | Interactive 3D menu |
| `CharacterStudio` | Character customization |
| `Lobby` | Matchmaking / room code |
| `Gameplay` | Mini-game play area |
| `Results` | Final podium / scores |

**For each scene:**
1. Right-click `Assets/Minimos/Scenes/` → Create → Scene
2. Name it as above
3. Add to **Build Settings** (File → Build Settings → Add Open Scenes)

---

## 🏗️ Step 4: Create Core Manager Objects

### 4a. Create "GameBootstrap" Prefab

Create an empty GameObject called **"GameBootstrap"** and add these components:
- `GameManager`
- `SceneLoader`
- `AudioManager` (add 2 AudioSources for music, 8 for SFX pool, 1 for announcer — or let the script create them)
- `TeamManager`
- `UIManager`
- `CameraManager`
- `AnnouncerManager`
- `FirebaseManager`
- `NetworkGameManager`
- `LobbyManager`

Save as prefab in `Assets/Minimos/Prefabs/Network/GameBootstrap.prefab`

**Place this prefab in the SplashScreen scene.** All singletons use DontDestroyOnLoad.

### 4b. Wire TeamManager

1. Create 6 TeamData ScriptableObjects:
   - Right-click `Assets/Minimos/Data/Teams/` → Create → Minimos → Teams → Team Data
   - Create: CoralRed, SkyBlue, MintGreen, SunnyYellow, PeachOrange, LavenderPurple
   - Set colors per the GDD hex values
2. Assign all 6 to `TeamManager.teamDataAssets` array

### 4c. Wire Audio

1. Create SFXLibrary: `Assets/Minimos/Data/Audio/` → Create → Minimos → Audio → SFX Library
2. Create MusicLibrary: same path → Create → Minimos → Audio → Music Library
3. Populate with audio clips from the imported free asset packs
4. Assign to AudioManager references

### 4d. Wire Announcer

1. Create AnnouncerConfig: `Assets/Minimos/Data/Audio/` → Create → Minimos → Announcer → Announcer Config
2. Populate with voice clips from the Casual Game Announcer pack
3. Assign to AnnouncerManager

---

## 🧑 Step 5: Create Player Prefab

1. Create a Capsule (or import Minimo character model later)
2. Add components:
   - `NetworkObject`
   - `CharacterController`
   - `PlayerController`
   - `PlayerCombat`
   - `PlayerVisuals`
   - `PlayerAnimator`
   - `PlayerSetup`
   - `PowerUpInventory`
   - `QuickReactionUI`
3. Create child transforms for cosmetic attachment points:
   - `HeadSlot` (above head)
   - `FaceSlot` (front of face)
   - `BackSlot` (behind torso)
   - `FeetSlot` (at feet)
4. Assign attachment point references in `PlayerVisuals`
5. Create a **World Space Canvas** child for nameplate (assign to PlayerVisuals)
6. Apply the `Minimos/TeamColorToon` shader to the body material
7. Save as prefab: `Assets/Minimos/Prefabs/Player/MinimoPlayer.prefab`
8. Add to **NetworkManager's Network Prefabs list**

---

## 🎯 Step 6: Create Projectile Prefab

1. Create a small sphere
2. Add: `NetworkObject`, `Projectile`, `Rigidbody` (isKinematic=true), `SphereCollider` (isTrigger=true)
3. Add a `TrailRenderer` component
4. Save as: `Assets/Minimos/Prefabs/Player/Projectile.prefab`
5. Add to **NetworkManager's Network Prefabs list**
6. Assign to `PlayerCombat.projectilePrefab`

---

## 📷 Step 7: Camera Setup

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

## 🖥️ Step 8: UI Setup

### MainMenu Scene:
1. Create a Canvas with:
   - Play Button, Character Studio Button, Settings Button, Quit Button
   - Player profile display (name, level, coins texts)
2. Attach `MainMenuController` and wire button references

### Lobby Scene:
1. Create Canvas with lobby UI (see `LobbyUIController` serialized fields)
2. Wire all references

### Gameplay Scene:
1. Create HUD Canvas with timer, score bars, cooldowns, minimap, event popup
2. Attach `GameHUD` and wire references
3. Create Loading Screen canvas (initially hidden) with `LoadingScreenController`

### Results Scene:
1. Create results UI canvas
2. Create 3D podium objects (3 platforms at different heights)
3. Attach `PodiumController` and `ResultsScreenController`

---

## 🎮 Step 9: Mini-Game Setup

### Create MiniGameConfigs:
1. `Assets/Minimos/Data/MiniGames/` → Create → Minimos → MiniGames → Mini Game Config
2. Create: CTF_Config, KOTH_Config
3. Fill in: name, description, duration, scoreToWin, cameraMode, min/max teams

### CTF Setup (in Gameplay scene):
1. Create a "CTFGameMode" GameObject with `CTFGameMode` component
2. Create flag spawn point transforms (5-8 empty GameObjects scattered around map)
3. Assign spawn points to CTFGameMode
4. Create a Flag prefab: `NetworkObject` + `Flag` + visual flag model + trigger collider
5. Save Flag prefab, assign to CTFGameMode

### KOTH Setup:
1. Create "KOTHGameMode" GameObject with `KOTHGameMode` component
2. Create zone position transforms (4-5 positions around map)
3. Create CaptureZone prefab: `NetworkObject` + `CaptureZone` + cylinder mesh + trigger collider

---

## ⚡ Step 10: Power-Up Setup

1. Create PowerUpConfig ScriptableObjects for each power-up type:
   - SpeedBoost, MegaPunch, BuddyShield, FreezeBomb
   - Set rarity, duration, description, VFX/SFX references
2. Create a base PowerUpCrate prefab:
   - Cube/crate model, `NetworkObject`, specific power-up script, trigger collider
3. Create `PowerUpSpawner` GameObject in Gameplay scene
4. Assign spawn point transforms and power-up config references

---

## 🌐 Step 11: Unity Services Setup

1. **Window → General → Services**
2. Link to your Unity Dashboard project
3. Enable: Relay, Lobby, Authentication
4. In code, services auto-initialize via `UnityServices.InitializeAsync()`

---

## 🔥 Step 12: Firebase SDK (Optional for now)

The project works without Firebase using `MockFirebaseService`. When ready:
1. Download Firebase Unity SDK from https://firebase.google.com/docs/unity/setup
2. Import `FirebaseAuth.unitypackage` and `FirebaseFirestore.unitypackage`
3. Add `FIREBASE_AVAILABLE` to **Player Settings → Other Settings → Scripting Define Symbols**
4. `FirebaseManager` will auto-switch from Mock to real Firebase

---

## 🗺️ Step 13: Map Setup (Sunny Meadows)

1. Create terrain or flat plane in Gameplay scene
2. Import the **Low-Poly Simple Nature Pack** assets
3. Dress the map: trees, rocks, grass, flowers, fences
4. Apply **Fantasy Skybox** to the scene lighting
5. Create team spawn zones (color-coded areas, one per team)
6. Create power-up spawn points scattered around the map
7. Add environmental hazards (bee spawners, mud patches, hay bales)

---

## ✅ Step 14: Build Settings

1. **File → Build Settings**
2. Add scenes in order:
   - SplashScreen (index 0)
   - MainMenu (index 1)
   - CharacterStudio (index 2)
   - Lobby (index 3)
   - Gameplay (index 4)
   - Results (index 5)
3. Set platform to **PC, Mac & Linux Standalone**
4. **Player Settings:**
   - Company Name: Pexon
   - Product Name: Minimos
   - Default Icon: (add later)
   - Color Space: Linear
   - API Compatibility: .NET Standard 2.1

---

> 💡 **Tip:** Work through these steps in order. The game will be playable (basic movement + combat + CTF) once steps 1-9 are complete. Audio, announcer, and power-ups can be wired later.
