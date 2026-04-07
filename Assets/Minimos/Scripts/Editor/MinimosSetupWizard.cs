using System.Collections.Generic;
using System.IO;
using System.Linq;
using Minimos.Audio;
using Minimos.Teams;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// Alias to avoid namespace collision with UnityEngine.Camera
using MinimosCamera = Minimos.Camera;

namespace Minimos.Editor
{
    /// <summary>
    /// One-click setup wizard that automates ALL project setup:
    /// scenes, build settings, input actions, ScriptableObjects, prefabs,
    /// GameBootstrap, cameras, basic UI, and player settings.
    ///
    /// Access via: Minimos → Setup Wizard → 🚀 Run Full Setup
    /// </summary>
    public static class MinimosSetupWizard
    {
        private const string DataPath = "Assets/Minimos/Data";
        private const string PrefabPath = "Assets/Minimos/Prefabs";
        private const string ScenePath = "Assets/Minimos/Scenes";

        private static readonly string[] SceneNames = { "SplashScreen", "MainMenu", "CharacterStudio", "Lobby", "Gameplay", "Results" };

        // =============================================
        // 🚀 FULL SETUP (does EVERYTHING)
        // =============================================

        [MenuItem("Minimos/Setup Wizard/🚀 Run Full Setup (All Steps)", false, 0)]
        public static void RunFullSetup()
        {
            EditorUtility.DisplayProgressBar("Minimos Setup", "Creating scenes...", 0.05f);
            CreateScenes();

            EditorUtility.DisplayProgressBar("Minimos Setup", "Configuring input actions...", 0.1f);
            ConfigureInputActions();

            EditorUtility.DisplayProgressBar("Minimos Setup", "Creating team data...", 0.15f);
            CreateTeamData();

            EditorUtility.DisplayProgressBar("Minimos Setup", "Creating audio libraries...", 0.2f);
            CreateAudioLibraries();
            CreateAnnouncerConfig();

            EditorUtility.DisplayProgressBar("Minimos Setup", "Creating mini-game configs...", 0.25f);
            CreateMiniGameConfigs();
            CreatePowerUpConfigs();

            EditorUtility.DisplayProgressBar("Minimos Setup", "Creating prefabs...", 0.35f);
            CreatePlayerPrefab();
            CreateProjectilePrefab();
            CreateNetworkPrefabsList();

            EditorUtility.DisplayProgressBar("Minimos Setup", "Configuring build settings...", 0.4f);
            ConfigureBuildSettings();
            ConfigurePlayerSettings();

            // Now open SplashScreen and create GameBootstrap there
            EditorUtility.DisplayProgressBar("Minimos Setup", "Setting up SplashScreen scene...", 0.5f);
            OpenSceneAndSetup("SplashScreen", SetupSplashScreen);

            EditorUtility.DisplayProgressBar("Minimos Setup", "Setting up MainMenu scene...", 0.6f);
            OpenSceneAndSetup("MainMenu", SetupMainMenuScene);

            EditorUtility.DisplayProgressBar("Minimos Setup", "Setting up Lobby scene...", 0.65f);
            OpenSceneAndSetup("Lobby", SetupLobbyScene);

            EditorUtility.DisplayProgressBar("Minimos Setup", "Setting up Gameplay scene...", 0.75f);
            OpenSceneAndSetup("Gameplay", SetupGameplayScene);

            EditorUtility.DisplayProgressBar("Minimos Setup", "Setting up Results scene...", 0.85f);
            OpenSceneAndSetup("Results", SetupResultsScene);

            // Return to SplashScreen (the boot scene)
            EditorUtility.DisplayProgressBar("Minimos Setup", "Finalizing...", 0.95f);
            EditorSceneManager.OpenScene($"{ScenePath}/SplashScreen.unity");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.ClearProgressBar();

            Debug.Log("✅ [Minimos Setup] FULL SETUP COMPLETE!");
            EditorUtility.DisplayDialog("Minimos Setup Complete",
                "Everything is set up and ready!\n\n" +
                "✅ 6 Scenes created and added to Build Settings\n" +
                "✅ Input Actions configured\n" +
                "✅ All ScriptableObjects created\n" +
                "✅ Player + Projectile prefabs created\n" +
                "✅ GameBootstrap in SplashScreen (all managers wired)\n" +
                "✅ 5 Cinemachine cameras in Gameplay scene\n" +
                "✅ Basic UI in all scenes\n" +
                "✅ Build Settings + Player Settings configured\n\n" +
                "Remaining manual steps:\n" +
                "• Link Unity Dashboard (Edit → Project Settings → Services)\n" +
                "• Design your maps with imported asset packs\n" +
                "• Populate audio libraries with clips (optional for now)",
                "Let's go!");
        }

        // =============================================
        // STEP 1: SCENES
        // =============================================

        [MenuItem("Minimos/Setup Wizard/1. Create Scenes", false, 100)]
        public static void CreateScenes()
        {
            EnsureDirectory(ScenePath);

            foreach (string sceneName in SceneNames)
            {
                string scenePath = $"{ScenePath}/{sceneName}.unity";
                if (File.Exists(scenePath)) continue;

                var newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                EditorSceneManager.SaveScene(newScene, scenePath);
                Debug.Log($"📝 [Minimos Setup] Created scene: {sceneName}");
            }

            Debug.Log("✅ [Minimos Setup] All 6 scenes created.");
        }

        // =============================================
        // STEP 2: INPUT ACTIONS
        // =============================================

        [MenuItem("Minimos/Setup Wizard/2. Configure Input Actions", false, 101)]
        public static void ConfigureInputActions()
        {
            string inputActionsPath = "Assets/Minimos/Input/MinimosInputActions.inputactions";
            var importer = AssetImporter.GetAtPath(inputActionsPath);
            if (importer == null)
            {
                Debug.LogWarning("⚠️ [Minimos Setup] MinimosInputActions.inputactions not found.");
                return;
            }

            // The InputActionImporter has properties for generating C# class
            var so = new SerializedObject(importer);
            var generateProp = so.FindProperty("m_GenerateWrapperCode");
            var namespaceProp = so.FindProperty("m_WrapperCodeNamespace");

            if (generateProp != null)
            {
                generateProp.boolValue = true;
                if (namespaceProp != null)
                    namespaceProp.stringValue = "Minimos.Input";
                so.ApplyModifiedPropertiesWithoutUndo();
                importer.SaveAndReimport();
                Debug.Log("✅ [Minimos Setup] Input Actions: Generate C# Class enabled, namespace set to Minimos.Input.");
            }
            else
            {
                Debug.LogWarning("⚠️ [Minimos Setup] Could not find GenerateWrapperCode property. You may need to enable it manually.");
            }
        }

        // =============================================
        // STEP 3: SCRIPTABLE OBJECTS
        // =============================================

        [MenuItem("Minimos/Setup Wizard/3. Create ScriptableObjects", false, 102)]
        public static void CreateAllScriptableObjects()
        {
            CreateTeamData();
            CreateAudioLibraries();
            CreateAnnouncerConfig();
            CreateMiniGameConfigs();
            CreatePowerUpConfigs();
        }

        public static void CreateTeamData()
        {
            string path = $"{DataPath}/Teams";
            EnsureDirectory(path);

            CreateTeamDataAsset(path, 0, "Coral Red", TeamColors.CoralRed, TeamColors.CoralRedAccent, "#FF6B6B", "#D63031");
            CreateTeamDataAsset(path, 1, "Sky Blue", TeamColors.SkyBlue, TeamColors.SkyBlueAccent, "#74B9FF", "#0984E3");
            CreateTeamDataAsset(path, 2, "Mint Green", TeamColors.MintGreen, TeamColors.MintGreenAccent, "#55EFC4", "#00B894");
            CreateTeamDataAsset(path, 3, "Sunny Yellow", TeamColors.SunnyYellow, TeamColors.SunnyYellowAccent, "#FFEAA7", "#FDCB6E");
            CreateTeamDataAsset(path, 4, "Peach Orange", TeamColors.PeachOrange, TeamColors.PeachOrangeAccent, "#FAB1A0", "#E17055");
            CreateTeamDataAsset(path, 5, "Lavender Purple", TeamColors.LavenderPurple, TeamColors.LavenderPurpleAccent, "#A29BFE", "#6C5CE7");

            Debug.Log("✅ [Minimos Setup] Created 6 TeamData assets.");
        }

        public static void CreateAudioLibraries()
        {
            string path = $"{DataPath}/Audio";
            EnsureDirectory(path);

            if (!AssetExists($"{path}/SFXLibrary.asset"))
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<SFXLibrary>(), $"{path}/SFXLibrary.asset");
            if (!AssetExists($"{path}/MusicLibrary.asset"))
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<MusicLibrary>(), $"{path}/MusicLibrary.asset");

            Debug.Log("✅ [Minimos Setup] Created audio libraries.");
        }

        public static void CreateAnnouncerConfig()
        {
            string path = $"{DataPath}/Audio";
            EnsureDirectory(path);

            if (!AssetExists($"{path}/AnnouncerConfig.asset"))
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<Announcer.AnnouncerConfig>(), $"{path}/AnnouncerConfig.asset");

            Debug.Log("✅ [Minimos Setup] Created AnnouncerConfig.");
        }

        public static void CreateMiniGameConfigs()
        {
            string path = $"{DataPath}/MiniGames";
            EnsureDirectory(path);

            CreateMiniGameConfig(path, "Capture The Flags",
                "Grab flags and hold them to earn points. First to 100 wins!",
                MiniGames.MiniGameCategory.Objective, 3, 6, 300f, 100, MiniGames.CameraMode.Follow);
            CreateMiniGameConfig(path, "King of the Hill",
                "Hold the zone to score points. Both teammates inside = bonus!",
                MiniGames.MiniGameCategory.Objective, 2, 6, 180f, 0, MiniGames.CameraMode.Arena);

            Debug.Log("✅ [Minimos Setup] Created MiniGameConfig assets.");
        }

        public static void CreatePowerUpConfigs()
        {
            string path = $"{DataPath}/PowerUps";
            EnsureDirectory(path);

            CreatePowerUpConfig(path, "Speed Boost", "2x movement speed for 3 seconds.", PowerUps.PowerUpRarity.Common, 3f);
            CreatePowerUpConfig(path, "Mega Punch", "Next melee attack deals 3x knockback.", PowerUps.PowerUpRarity.Uncommon, 0f);
            CreatePowerUpConfig(path, "Buddy Shield", "Both teammates get a 1-hit shield.", PowerUps.PowerUpRarity.Rare, 0f);
            CreatePowerUpConfig(path, "Freeze Bomb", "Throw to root nearby enemies for 2 seconds.", PowerUps.PowerUpRarity.Uncommon, 0f);

            Debug.Log("✅ [Minimos Setup] Created PowerUpConfig assets.");
        }

        // =============================================
        // STEP 4: PREFABS
        // =============================================

        [MenuItem("Minimos/Setup Wizard/4. Create Prefabs", false, 103)]
        public static void CreateAllPrefabs()
        {
            CreatePlayerPrefab();
            CreateProjectilePrefab();
        }

        public static void CreatePlayerPrefab()
        {
            string prefabDir = $"{PrefabPath}/Player";
            EnsureDirectory(prefabDir);
            string prefabAssetPath = $"{prefabDir}/MinimoPlayer.prefab";
            if (AssetExists(prefabAssetPath)) return;

            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "MinimoPlayer";

            // Apply team color shader
            var shader = Shader.Find("Minimos/TeamColorToon");
            if (shader != null)
            {
                string matPath = "Assets/Minimos/Materials/TeamColors/MinimoBody.mat";
                EnsureDirectory("Assets/Minimos/Materials/TeamColors");
                if (!AssetExists(matPath))
                    AssetDatabase.CreateAsset(new Material(shader) { name = "MinimoBody" }, matPath);
                player.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            }

            player.AddComponent<Unity.Netcode.NetworkObject>();

            var cc = player.GetComponent<CapsuleCollider>();
            if (cc != null) Object.DestroyImmediate(cc);
            var charController = player.AddComponent<CharacterController>();
            charController.center = new Vector3(0, 1f, 0);
            charController.height = 2f;
            charController.radius = 0.5f;

            player.AddComponent<Player.PlayerController>();
            player.AddComponent<Player.PlayerCombat>();
            player.AddComponent<Player.PlayerVisuals>();
            player.AddComponent<Player.PlayerAnimator>();
            player.AddComponent<Player.PlayerSetup>();
            player.AddComponent<PowerUps.PowerUpInventory>();

            CreateChildTransform(player, "HeadSlot", new Vector3(0, 2.2f, 0));
            CreateChildTransform(player, "FaceSlot", new Vector3(0, 1.6f, 0.5f));
            CreateChildTransform(player, "BackSlot", new Vector3(0, 1.2f, -0.5f));
            CreateChildTransform(player, "FeetSlot", new Vector3(0, 0f, 0));

            var nameplateCanvas = new GameObject("NameplateCanvas");
            nameplateCanvas.transform.SetParent(player.transform);
            nameplateCanvas.transform.localPosition = new Vector3(0, 2.5f, 0);
            var canvas = nameplateCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(2f, 0.5f);
            canvas.transform.localScale = Vector3.one * 0.01f;

            PrefabUtility.SaveAsPrefabAsset(player, prefabAssetPath);
            Object.DestroyImmediate(player);

            // Wire serialized references on the saved prefab
            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabAssetPath);
            var visuals = prefabAsset.GetComponent<Player.PlayerVisuals>();
            if (visuals != null)
            {
                var so = new SerializedObject(visuals);
                SetPropertyRef(so, "headSlot", prefabAsset.transform.Find("HeadSlot"));
                SetPropertyRef(so, "faceSlot", prefabAsset.transform.Find("FaceSlot"));
                SetPropertyRef(so, "backSlot", prefabAsset.transform.Find("BackSlot"));
                SetPropertyRef(so, "feetSlot", prefabAsset.transform.Find("FeetSlot"));
                SetPropertyRef(so, "bodyRenderer", prefabAsset.GetComponent<Renderer>());
                SetPropertyRef(so, "visualRoot", prefabAsset.transform);
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            AssetDatabase.SaveAssets();
            Debug.Log("✅ [Minimos Setup] Created MinimoPlayer prefab.");
        }

        public static void CreateProjectilePrefab()
        {
            string prefabDir = $"{PrefabPath}/Player";
            EnsureDirectory(prefabDir);
            string prefabAssetPath = $"{prefabDir}/Projectile.prefab";
            if (AssetExists(prefabAssetPath)) return;

            var proj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            proj.name = "Projectile";
            proj.transform.localScale = Vector3.one * 0.3f;
            proj.GetComponent<SphereCollider>().isTrigger = true;
            proj.AddComponent<Unity.Netcode.NetworkObject>();
            var rb = proj.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            proj.AddComponent<Player.Projectile>();
            var trail = proj.AddComponent<TrailRenderer>();
            trail.startWidth = 0.15f;
            trail.endWidth = 0f;
            trail.time = 0.3f;
            var trailMat = Shader.Find("Sprites/Default");
            if (trailMat != null) trail.material = new Material(trailMat);
            trail.startColor = Color.white;
            trail.endColor = new Color(1, 1, 1, 0);

            PrefabUtility.SaveAsPrefabAsset(proj, prefabAssetPath);
            Object.DestroyImmediate(proj);
            Debug.Log("✅ [Minimos Setup] Created Projectile prefab.");
        }

        [MenuItem("Minimos/Setup Wizard/4b. Create Network Prefabs List", false, 104)]
        public static void CreateNetworkPrefabsList()
        {
            string dir = $"{PrefabPath}/Network";
            EnsureDirectory(dir);
            string path = $"{dir}/MinimosNetworkPrefabs.asset";

            // Always recreate to ensure it has the latest prefabs
            if (AssetExists(path))
                AssetDatabase.DeleteAsset(path);

            var list = ScriptableObject.CreateInstance<Unity.Netcode.NetworkPrefabsList>();
            AssetDatabase.CreateAsset(list, path);
            AssetDatabase.SaveAssets();

            // Reload from disk
            list = AssetDatabase.LoadAssetAtPath<Unity.Netcode.NetworkPrefabsList>(path);

            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabPath}/Player/MinimoPlayer.prefab");
            var projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabPath}/Player/Projectile.prefab");

            if (playerPrefab != null)
            {
                list.Add(new Unity.Netcode.NetworkPrefab { Prefab = playerPrefab });
                Debug.Log($"📝 [Minimos Setup] Added MinimoPlayer to network prefabs list.");
            }
            else
            {
                Debug.LogWarning("⚠️ [Minimos Setup] MinimoPlayer prefab not found!");
            }

            if (projectilePrefab != null)
            {
                list.Add(new Unity.Netcode.NetworkPrefab { Prefab = projectilePrefab });
                Debug.Log($"📝 [Minimos Setup] Added Projectile to network prefabs list.");
            }
            else
            {
                Debug.LogWarning("⚠️ [Minimos Setup] Projectile prefab not found!");
            }

            EditorUtility.SetDirty(list);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"✅ [Minimos Setup] Created MinimosNetworkPrefabs.asset at {path}");
        }

        // =============================================
        // STEP 5: BUILD + PLAYER SETTINGS
        // =============================================

        [MenuItem("Minimos/Setup Wizard/5. Configure Build Settings", false, 104)]
        public static void ConfigureBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>();
            foreach (string sceneName in SceneNames)
            {
                string path = $"{ScenePath}/{sceneName}.unity";
                if (File.Exists(path))
                    scenes.Add(new EditorBuildSettingsScene(path, true));
            }
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log($"✅ [Minimos Setup] Build Settings: {scenes.Count} scenes configured in correct order.");
        }

        public static void ConfigurePlayerSettings()
        {
            PlayerSettings.companyName = "Pexon";
            PlayerSettings.productName = "Minimos";
            PlayerSettings.colorSpace = ColorSpace.Linear;
            Debug.Log("✅ [Minimos Setup] Player Settings: Pexon / Minimos / Linear color space.");
        }

        // =============================================
        // PER-SCENE SETUP
        // =============================================

        private static void OpenSceneAndSetup(string sceneName, System.Action setupAction)
        {
            string path = $"{ScenePath}/{sceneName}.unity";
            if (!File.Exists(path))
            {
                Debug.LogWarning($"⚠️ [Minimos Setup] Scene not found: {path}");
                return;
            }

            EditorSceneManager.OpenScene(path);
            setupAction();
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }

        // --- SplashScreen: GameBootstrap + NetworkManager ---
        private static void SetupSplashScreen()
        {
            if (GameObject.Find("GameBootstrap") != null) return;

            var go = new GameObject("GameBootstrap");

            // Unity's NetworkManager + Transport
            var netManager = go.AddComponent<Unity.Netcode.NetworkManager>();
            var transport = go.AddComponent<Unity.Netcode.Transports.UTP.UnityTransport>();

            // Wire transport
            var soNet = new SerializedObject(netManager);
            SetSerializedProperty(soNet, "NetworkConfig.NetworkTransport", transport);

            // Wire player prefab
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabPath}/Player/MinimoPlayer.prefab");
            if (playerPrefab != null)
                SetSerializedProperty(soNet, "NetworkConfig.PlayerPrefab", playerPrefab);

            soNet.ApplyModifiedPropertiesWithoutUndo();

            // Our managers
            go.AddComponent<Core.GameManager>();
            go.AddComponent<Core.SceneLoader>();
            go.AddComponent<Audio.AudioManager>();
            var teamMgr = go.AddComponent<Teams.TeamManager>();
            go.AddComponent<Minimos.UI.UIManager>();
            go.AddComponent<MinimosCamera.CameraManager>();
            var announcerMgr = go.AddComponent<Announcer.AnnouncerManager>();
            go.AddComponent<Firebase.FirebaseManager>();
            var netGameMgr = go.AddComponent<Networking.NetworkGameManager>();

            // Wire NetworkGameManager → NetworkManager
            var soNetGame = new SerializedObject(netGameMgr);
            SetSerializedProperty(soNetGame, "networkManager", netManager);
            soNetGame.ApplyModifiedPropertiesWithoutUndo();

            // Wire TeamData
            WireTeamData(teamMgr);

            // Wire AnnouncerConfig
            var announcerConfig = AssetDatabase.LoadAssetAtPath<Announcer.AnnouncerConfig>($"{DataPath}/Audio/AnnouncerConfig.asset");
            if (announcerConfig != null)
            {
                var soAnnouncer = new SerializedObject(announcerMgr);
                SetSerializedProperty(soAnnouncer, "config", announcerConfig);
                soAnnouncer.ApplyModifiedPropertiesWithoutUndo();
            }

            // NOW replace the NetworkPrefabsLists AFTER all initialization is done.
            // NetworkManager.AddComponent initializes with DefaultNetworkPrefabs from the project.
            // We need to forcefully replace it with our own list.
            ForceReplaceNetworkPrefabsList(netManager);

            Debug.Log("✅ [Minimos Setup] SplashScreen: GameBootstrap created with all managers wired.");
        }

        /// <summary>
        /// Replaces the NetworkManager's prefab list with our MinimosNetworkPrefabs.
        /// The asset must already exist (created by CreateNetworkPrefabsList earlier).
        /// Uses public API directly since SerializedObject gets overwritten by initialization.
        /// </summary>
        private static void ForceReplaceNetworkPrefabsList(Unity.Netcode.NetworkManager netManager)
        {
            string prefabsListPath = $"{PrefabPath}/Network/MinimosNetworkPrefabs.asset";
            var prefabsList = AssetDatabase.LoadAssetAtPath<Unity.Netcode.NetworkPrefabsList>(prefabsListPath);

            if (prefabsList == null)
            {
                Debug.LogError("🚨 [Minimos Setup] MinimosNetworkPrefabs.asset not found! CreateNetworkPrefabsList must run first.");
                return;
            }

            // Direct public API — clear the default list and add ours
            netManager.NetworkConfig.Prefabs.NetworkPrefabsLists.Clear();
            netManager.NetworkConfig.Prefabs.NetworkPrefabsLists.Add(prefabsList);
            EditorUtility.SetDirty(netManager);

            Debug.Log("✅ [Minimos Setup] NetworkManager now uses MinimosNetworkPrefabs.");
        }

        // --- MainMenu: Canvas with buttons ---
        private static void SetupMainMenuScene()
        {
            if (GameObject.Find("MainMenuCanvas") != null) return;

            var canvasGo = CreateUICanvas("MainMenuCanvas");
            var menuController = canvasGo.AddComponent<UI.MainMenuController>();

            // Create buttons
            var playBtn = CreateUIButton(canvasGo.transform, "PlayButton", "▶  PLAY", new Vector2(0, 60), new Vector2(300, 60));
            var studioBtn = CreateUIButton(canvasGo.transform, "CharacterStudioButton", "🧑  CHARACTER", new Vector2(0, -10), new Vector2(300, 60));
            var settingsBtn = CreateUIButton(canvasGo.transform, "SettingsButton", "⚙  SETTINGS", new Vector2(0, -80), new Vector2(300, 60));
            var quitBtn = CreateUIButton(canvasGo.transform, "QuitButton", "✖  QUIT", new Vector2(0, -150), new Vector2(300, 60));

            // Profile display
            var profilePanel = new GameObject("ProfilePanel");
            profilePanel.transform.SetParent(canvasGo.transform, false);
            var profileRect = profilePanel.AddComponent<RectTransform>();
            profileRect.anchorMin = new Vector2(0, 1);
            profileRect.anchorMax = new Vector2(0, 1);
            profileRect.pivot = new Vector2(0, 1);
            profileRect.anchoredPosition = new Vector2(20, -20);
            profileRect.sizeDelta = new Vector2(300, 80);

            var nameText = CreateUIText(profilePanel.transform, "PlayerNameText", "Player", new Vector2(0, 15), 24);
            var levelText = CreateUIText(profilePanel.transform, "LevelText", "Level 1 | 0 Coins", new Vector2(0, -15), 16);

            // Wire to controller
            var so = new SerializedObject(menuController);
            SetPropertyRef(so, "playButton", playBtn.GetComponent<Button>());
            SetPropertyRef(so, "characterStudioButton", studioBtn.GetComponent<Button>());
            SetPropertyRef(so, "settingsButton", settingsBtn.GetComponent<Button>());
            SetPropertyRef(so, "quitButton", quitBtn.GetComponent<Button>());
            SetPropertyRef(so, "playerNameText", nameText);
            SetPropertyRef(so, "playerLevelText", levelText);
            so.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log("✅ [Minimos Setup] MainMenu: Canvas with buttons + profile display.");
        }

        // --- Lobby: Canvas with lobby UI ---
        private static void SetupLobbyScene()
        {
            if (GameObject.Find("LobbyCanvas") != null) return;

            var canvasGo = CreateUICanvas("LobbyCanvas");
            canvasGo.AddComponent<UI.LobbyUIController>();

            CreateUIButton(canvasGo.transform, "QuickPlayButton", "⚡ QUICK PLAY", new Vector2(0, 100), new Vector2(280, 50));
            CreateUIButton(canvasGo.transform, "CreatePrivateButton", "🔒 CREATE PRIVATE", new Vector2(0, 40), new Vector2(280, 50));
            CreateUIButton(canvasGo.transform, "JoinByCodeButton", "🔗 JOIN BY CODE", new Vector2(0, -20), new Vector2(280, 50));
            CreateUIButton(canvasGo.transform, "BackButton", "← BACK", new Vector2(0, -100), new Vector2(200, 40));
            CreateUIText(canvasGo.transform, "RoomCodeText", "Room Code: ------", new Vector2(0, 160), 20);

            Debug.Log("✅ [Minimos Setup] Lobby: Canvas with lobby UI.");
        }

        // --- Gameplay: HUD + Cameras ---
        private static void SetupGameplayScene()
        {
            // HUD Canvas
            if (GameObject.Find("GameHUDCanvas") == null)
            {
                var hudCanvas = CreateUICanvas("GameHUDCanvas");
                hudCanvas.AddComponent<UI.GameHUD>();

                CreateUIText(hudCanvas.transform, "TimerText", "5:00", new Vector2(0, -30), 36, TextAlignmentOptions.Top);
                CreateUIText(hudCanvas.transform, "MiniGameNameText", "Capture The Flags", new Vector2(0, -65), 18, TextAlignmentOptions.Top);
                CreateUIText(hudCanvas.transform, "EventPopupText", "", new Vector2(0, 0), 28);
            }

            // Cinemachine Cameras
            if (GameObject.Find("CM_FollowCam") == null)
            {
                var cameraRig = new GameObject("--- Minimos Cameras ---");

                var followCam = CreateCinemachineCamera("CM_FollowCam", cameraRig.transform);
                var followComp = followCam.gameObject.AddComponent<CinemachineFollow>();
                followComp.FollowOffset = new Vector3(0f, 5f, -8f);
                followComp.TrackerSettings.PositionDamping = new Vector3(0.5f, 0.5f, 0.5f);
                followCam.gameObject.AddComponent<CinemachineRotationComposer>();
                followCam.Priority = 10;

                var arenaCam = CreateCinemachineCamera("CM_ArenaCam", cameraRig.transform);
                arenaCam.transform.localPosition = new Vector3(0f, 25f, -15f);
                arenaCam.transform.localRotation = Quaternion.Euler(55f, 0f, 0f);
                arenaCam.gameObject.AddComponent<CinemachineRotationComposer>();

                var sideScrollCam = CreateCinemachineCamera("CM_SideScrollCam", cameraRig.transform);
                var sideFollow = sideScrollCam.gameObject.AddComponent<CinemachineFollow>();
                sideFollow.FollowOffset = new Vector3(0f, 3f, -15f);
                sideFollow.TrackerSettings.PositionDamping = new Vector3(0.3f, 0.3f, 0f);
                sideScrollCam.gameObject.AddComponent<CinemachineRotationComposer>();

                var sportsCam = CreateCinemachineCamera("CM_SportsCam", cameraRig.transform);
                var sportsFollow = sportsCam.gameObject.AddComponent<CinemachineFollow>();
                sportsFollow.FollowOffset = new Vector3(0f, 12f, -18f);
                sportsFollow.TrackerSettings.PositionDamping = new Vector3(1f, 0.5f, 1f);
                sportsCam.gameObject.AddComponent<CinemachineRotationComposer>();

                var splitZoneCam = CreateCinemachineCamera("CM_SplitZoneCam", cameraRig.transform);
                var splitFollow = splitZoneCam.gameObject.AddComponent<CinemachineFollow>();
                splitFollow.FollowOffset = new Vector3(0f, 10f, -10f);
                splitFollow.TrackerSettings.PositionDamping = new Vector3(0.5f, 0.5f, 0.5f);
                splitZoneCam.gameObject.AddComponent<CinemachineRotationComposer>();

                // Impulse on main camera
                var mainCam = UnityEngine.Camera.main;
                CinemachineImpulseSource impulseSource = null;
                if (mainCam != null)
                {
                    impulseSource = mainCam.GetComponent<CinemachineImpulseSource>() ?? mainCam.gameObject.AddComponent<CinemachineImpulseSource>();
                    if (mainCam.GetComponent<CinemachineImpulseListener>() == null)
                        mainCam.gameObject.AddComponent<CinemachineImpulseListener>();
                }

                // Wire to CameraManager on GameBootstrap (if in scene via DontDestroyOnLoad)
                // Since GameBootstrap is in SplashScreen, CameraManager won't be in this scene yet.
                // Instead, CameraManager will find cameras by name at runtime, OR we wire via a helper.
                // For now, create a small bridge script that wires them at runtime.
                var cameraWirer = cameraRig.AddComponent<MinimosCamera.GameplayCameraWirer>();
                var soWirer = new SerializedObject(cameraWirer);
                SetPropertyRef(soWirer, "followCamera", followCam);
                SetPropertyRef(soWirer, "arenaCamera", arenaCam);
                SetPropertyRef(soWirer, "sideScrollCamera", sideScrollCam);
                SetPropertyRef(soWirer, "sportsCamera", sportsCam);
                SetPropertyRef(soWirer, "splitZoneCamera", splitZoneCam);
                if (impulseSource != null)
                    SetPropertyRef(soWirer, "impulseSource", impulseSource);
                soWirer.ApplyModifiedPropertiesWithoutUndo();
            }

            Debug.Log("✅ [Minimos Setup] Gameplay: HUD + 5 Cinemachine cameras.");
        }

        // --- Results: Podium + UI ---
        private static void SetupResultsScene()
        {
            if (GameObject.Find("ResultsCanvas") != null) return;

            var canvasGo = CreateUICanvas("ResultsCanvas");
            canvasGo.AddComponent<UI.ResultsScreenController>();

            CreateUIText(canvasGo.transform, "ResultsTitleText", "RESULTS", new Vector2(0, -40), 42, TextAlignmentOptions.Top);
            CreateUIButton(canvasGo.transform, "PlayAgainButton", "🔄 PLAY AGAIN", new Vector2(-120, -200), new Vector2(220, 50));
            CreateUIButton(canvasGo.transform, "ReturnToMenuButton", "🏠 MENU", new Vector2(120, -200), new Vector2(220, 50));

            // Podium platforms
            var podiumParent = new GameObject("Podium");
            CreatePodiumPlatform(podiumParent.transform, "1st Place", new Vector3(0, 1.5f, 0), new Vector3(2, 3, 2), TeamColors.CoralRed);
            CreatePodiumPlatform(podiumParent.transform, "2nd Place", new Vector3(-3, 1f, 0), new Vector3(2, 2, 2), TeamColors.SkyBlue);
            CreatePodiumPlatform(podiumParent.transform, "3rd Place", new Vector3(3, 0.5f, 0), new Vector3(2, 1, 2), TeamColors.MintGreen);

            Debug.Log("✅ [Minimos Setup] Results: Canvas + 3D podium.");
        }

        // =============================================
        // HELPERS — SCRIPTABLE OBJECTS
        // =============================================

        private static void CreateTeamDataAsset(string folder, int index, string teamName,
            Color primary, Color accent, string primaryHex, string accentHex)
        {
            string assetPath = $"{folder}/Team_{teamName.Replace(" ", "")}.asset";
            if (AssetExists(assetPath)) return;

            var data = ScriptableObject.CreateInstance<TeamData>();
            AssetDatabase.CreateAsset(data, assetPath);
            var so = new SerializedObject(data);
            so.FindProperty("teamIndex").intValue = index;
            so.FindProperty("teamName").stringValue = teamName;
            so.FindProperty("teamColor").colorValue = primary;
            so.FindProperty("teamColorHex").stringValue = primaryHex;
            so.FindProperty("accentColor").colorValue = accent;
            so.FindProperty("accentColorHex").stringValue = accentHex;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateMiniGameConfig(string folder, string gameName, string description,
            MiniGames.MiniGameCategory category, int minTeams, int maxTeams,
            float duration, int scoreToWin, MiniGames.CameraMode cameraMode)
        {
            string assetPath = $"{folder}/{gameName.Replace(" ", "")}_Config.asset";
            if (AssetExists(assetPath)) return;

            var config = ScriptableObject.CreateInstance<MiniGames.MiniGameConfig>();
            AssetDatabase.CreateAsset(config, assetPath);
            var so = new SerializedObject(config);
            so.FindProperty("gameName").stringValue = gameName;
            so.FindProperty("description").stringValue = description;
            so.FindProperty("category").enumValueIndex = (int)category;
            so.FindProperty("minTeams").intValue = minTeams;
            so.FindProperty("maxTeams").intValue = maxTeams;
            so.FindProperty("duration").floatValue = duration;
            so.FindProperty("scoreToWin").intValue = scoreToWin;
            so.FindProperty("cameraMode").enumValueIndex = (int)cameraMode;
            so.FindProperty("powerUpsEnabled").boolValue = true;
            so.FindProperty("rulesText").stringValue = description;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreatePowerUpConfig(string folder, string powerUpName, string description,
            PowerUps.PowerUpRarity rarity, float duration)
        {
            string assetPath = $"{folder}/{powerUpName.Replace(" ", "")}_Config.asset";
            if (AssetExists(assetPath)) return;

            var config = ScriptableObject.CreateInstance<PowerUps.PowerUpConfig>();
            AssetDatabase.CreateAsset(config, assetPath);
            var so = new SerializedObject(config);
            so.FindProperty("powerUpName").stringValue = powerUpName;
            so.FindProperty("description").stringValue = description;
            so.FindProperty("rarity").enumValueIndex = (int)rarity;
            so.FindProperty("duration").floatValue = duration;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // =============================================
        // HELPERS — NETWORK
        // =============================================

        // (Old CreateAndWireNetworkPrefabsList removed — replaced by CreateNetworkPrefabsList + ForceReplaceNetworkPrefabsList)

        private static void WireTeamData(Teams.TeamManager teamManager)
        {
            var teamAssets = new TeamData[6];
            string[] names = { "CoralRed", "SkyBlue", "MintGreen", "SunnyYellow", "PeachOrange", "LavenderPurple" };
            for (int i = 0; i < names.Length; i++)
                teamAssets[i] = AssetDatabase.LoadAssetAtPath<TeamData>($"{DataPath}/Teams/Team_{names[i]}.asset");

            var so = new SerializedObject(teamManager);
            var prop = so.FindProperty("teamDataAssets");
            if (prop != null)
            {
                prop.arraySize = 6;
                for (int i = 0; i < 6; i++)
                    prop.GetArrayElementAtIndex(i).objectReferenceValue = teamAssets[i];
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // =============================================
        // HELPERS — UI
        // =============================================

        private static GameObject CreateUICanvas(string name)
        {
            var canvasGo = new GameObject(name);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            // EventSystem if not present
            if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            return canvasGo;
        }

        private static GameObject CreateUIButton(Transform parent, string name, string label, Vector2 position, Vector2 size)
        {
            var btnGo = new GameObject(name);
            btnGo.transform.SetParent(parent, false);

            var rect = btnGo.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var image = btnGo.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

            btnGo.AddComponent<Button>();

            // Label text
            var textGo = new GameObject("Label");
            textGo.transform.SetParent(btnGo.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 22;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return btnGo;
        }

        private static TextMeshProUGUI CreateUIText(Transform parent, string name, string text, Vector2 position, int fontSize, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            var textGo = new GameObject(name);
            textGo.transform.SetParent(parent, false);

            var rect = textGo.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(400, 50);

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = Color.white;

            return tmp;
        }

        private static void CreatePodiumPlatform(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
        {
            var platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.name = name;
            platform.transform.SetParent(parent);
            platform.transform.localPosition = position;
            platform.transform.localScale = scale;
            platform.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
        }

        // =============================================
        // HELPERS — CAMERA
        // =============================================

        private static CinemachineCamera CreateCinemachineCamera(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            var cam = go.AddComponent<CinemachineCamera>();
            cam.Priority = 0;
            return cam;
        }

        // =============================================
        // HELPERS — GENERAL
        // =============================================

        private static void EnsureDirectory(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
                string folder = Path.GetFileName(path);
                if (parent != null && !AssetDatabase.IsValidFolder(parent))
                    EnsureDirectory(parent);
                AssetDatabase.CreateFolder(parent, folder);
            }
        }

        private static bool AssetExists(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Object>(path) != null;
        }

        private static GameObject CreateChildTransform(GameObject parent, string name, Vector3 localPos)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent.transform);
            child.transform.localPosition = localPos;
            child.transform.localRotation = Quaternion.identity;
            return child;
        }

        private static void SetPropertyRef(SerializedObject so, string propName, Object value)
        {
            var prop = so.FindProperty(propName);
            if (prop != null)
                prop.objectReferenceValue = value;
            else
                Debug.LogWarning($"⚠️ [Minimos Setup] Property '{propName}' not found on {so.targetObject.GetType().Name}.");
        }

        private static void SetSerializedProperty(SerializedObject so, string propPath, Object value)
        {
            var prop = so.FindProperty(propPath);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                Debug.LogWarning($"⚠️ [Minimos Setup] Property path '{propPath}' not found.");
            }
        }
    }

}
