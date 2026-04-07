using System.IO;
using Minimos.Audio;
using Minimos.Teams;
using UnityEditor;
using UnityEngine;

namespace Minimos.Editor
{
    /// <summary>
    /// One-click setup wizard that creates all ScriptableObjects, prefabs, and
    /// the GameBootstrap object needed to run Minimos.
    /// Access via the Unity menu: Minimos → Setup Wizard → ...
    /// </summary>
    public static class MinimosSetupWizard
    {
        private const string DataPath = "Assets/Minimos/Data";
        private const string PrefabPath = "Assets/Minimos/Prefabs";

        // =============================================
        // 🚀 FULL SETUP (does everything)
        // =============================================

        [MenuItem("Minimos/Setup Wizard/🚀 Run Full Setup (All Steps)", false, 0)]
        public static void RunFullSetup()
        {
            CreateTeamData();
            CreateAudioLibraries();
            CreateAnnouncerConfig();
            CreateMiniGameConfigs();
            CreatePowerUpConfigs();
            CreateGameBootstrap();
            CreatePlayerPrefab();
            CreateProjectilePrefab();

            Debug.Log("✅ [Minimos Setup] Full setup complete! Check Assets/Minimos/Data/ and Assets/Minimos/Prefabs/");
            EditorUtility.DisplayDialog("Minimos Setup Complete",
                "All ScriptableObjects, prefabs, and the GameBootstrap have been created.\n\n" +
                "Next steps:\n" +
                "1. Place GameBootstrap prefab in your SplashScreen scene\n" +
                "2. Add MinimoPlayer prefab to NetworkManager's Network Prefabs list\n" +
                "3. Add Projectile prefab to NetworkManager's Network Prefabs list\n" +
                "4. Populate audio libraries with clips from imported asset packs",
                "Got it!");
        }

        // =============================================
        // INDIVIDUAL STEPS
        // =============================================

        // --- Step 4b: Team Data ---

        [MenuItem("Minimos/Setup Wizard/Create Team Data (6 teams)", false, 100)]
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

        // --- Step 4c: Audio Libraries ---

        [MenuItem("Minimos/Setup Wizard/Create Audio Libraries", false, 101)]
        public static void CreateAudioLibraries()
        {
            string path = $"{DataPath}/Audio";
            EnsureDirectory(path);

            if (!AssetExists($"{path}/SFXLibrary.asset"))
            {
                var sfx = ScriptableObject.CreateInstance<SFXLibrary>();
                AssetDatabase.CreateAsset(sfx, $"{path}/SFXLibrary.asset");
            }

            if (!AssetExists($"{path}/MusicLibrary.asset"))
            {
                var music = ScriptableObject.CreateInstance<MusicLibrary>();
                AssetDatabase.CreateAsset(music, $"{path}/MusicLibrary.asset");
            }

            Debug.Log("✅ [Minimos Setup] Created SFXLibrary and MusicLibrary assets. Populate with clips from imported packs.");
        }

        // --- Step 4d: Announcer Config ---

        [MenuItem("Minimos/Setup Wizard/Create Announcer Config", false, 102)]
        public static void CreateAnnouncerConfig()
        {
            string path = $"{DataPath}/Audio";
            EnsureDirectory(path);

            if (!AssetExists($"{path}/AnnouncerConfig.asset"))
            {
                var config = ScriptableObject.CreateInstance<Announcer.AnnouncerConfig>();
                AssetDatabase.CreateAsset(config, $"{path}/AnnouncerConfig.asset");
            }

            Debug.Log("✅ [Minimos Setup] Created AnnouncerConfig asset. Populate with voice clips.");
        }

        // --- MiniGame Configs ---

        [MenuItem("Minimos/Setup Wizard/Create MiniGame Configs", false, 103)]
        public static void CreateMiniGameConfigs()
        {
            string path = $"{DataPath}/MiniGames";
            EnsureDirectory(path);

            CreateMiniGameConfig(path, "Capture The Flags",
                "Grab flags and hold them to earn points. First to 100 wins!",
                MiniGames.MiniGameCategory.Objective, 3, 6, 300, 100,
                MiniGames.CameraMode.Follow);

            CreateMiniGameConfig(path, "King of the Hill",
                "Hold the zone to score points. Both teammates inside = bonus!",
                MiniGames.MiniGameCategory.Objective, 2, 6, 180, 0,
                MiniGames.CameraMode.Arena);

            Debug.Log("✅ [Minimos Setup] Created CTF and KOTH MiniGameConfig assets.");
        }

        private static void CreateMiniGameConfig(string folder, string gameName, string description,
            MiniGames.MiniGameCategory category, int minTeams, int maxTeams,
            int duration, int scoreToWin, MiniGames.CameraMode cameraMode)
        {
            string safeName = gameName.Replace(" ", "");
            string assetPath = $"{folder}/{safeName}_Config.asset";
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

        // --- Power-Up Configs ---

        [MenuItem("Minimos/Setup Wizard/Create PowerUp Configs", false, 104)]
        public static void CreatePowerUpConfigs()
        {
            string path = $"{DataPath}/PowerUps";
            EnsureDirectory(path);

            CreatePowerUpConfig(path, "Speed Boost", "2x movement speed for 3 seconds.",
                PowerUps.PowerUpRarity.Common, 3f);
            CreatePowerUpConfig(path, "Mega Punch", "Next melee attack deals 3x knockback.",
                PowerUps.PowerUpRarity.Uncommon, 0f);
            CreatePowerUpConfig(path, "Buddy Shield", "Both teammates get a 1-hit shield.",
                PowerUps.PowerUpRarity.Rare, 0f);
            CreatePowerUpConfig(path, "Freeze Bomb", "Throw to root nearby enemies for 2 seconds.",
                PowerUps.PowerUpRarity.Uncommon, 0f);

            Debug.Log("✅ [Minimos Setup] Created 4 PowerUpConfig assets.");
        }

        private static void CreatePowerUpConfig(string folder, string powerUpName, string description,
            PowerUps.PowerUpRarity rarity, float duration)
        {
            string safeName = powerUpName.Replace(" ", "");
            string assetPath = $"{folder}/{safeName}_Config.asset";
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

        // --- Step 4a: GameBootstrap Prefab ---

        [MenuItem("Minimos/Setup Wizard/Create GameBootstrap Prefab", false, 200)]
        public static void CreateGameBootstrap()
        {
            string prefabDir = $"{PrefabPath}/Network";
            EnsureDirectory(prefabDir);

            string prefabAssetPath = $"{prefabDir}/GameBootstrap.prefab";
            if (AssetExists(prefabAssetPath))
            {
                Debug.Log("📝 [Minimos Setup] GameBootstrap prefab already exists. Skipping.");
                return;
            }

            var go = new GameObject("GameBootstrap");

            // Core
            go.AddComponent<Core.GameManager>();
            go.AddComponent<Core.SceneLoader>();

            // Audio — AudioManager creates its own AudioSources if needed
            go.AddComponent<Audio.AudioManager>();

            // Teams
            var teamManager = go.AddComponent<Teams.TeamManager>();

            // Wire TeamData assets if they exist
            var teamAssets = new TeamData[6];
            string[] teamNames = { "CoralRed", "SkyBlue", "MintGreen", "SunnyYellow", "PeachOrange", "LavenderPurple" };
            for (int i = 0; i < teamNames.Length; i++)
            {
                string assetPath = $"{DataPath}/Teams/Team_{teamNames[i]}.asset";
                teamAssets[i] = AssetDatabase.LoadAssetAtPath<TeamData>(assetPath);
            }

            // Use SerializedObject to set private serialized fields
            var serializedTeamManager = new SerializedObject(teamManager);
            var teamDataProp = serializedTeamManager.FindProperty("teamDataAssets");
            if (teamDataProp != null)
            {
                teamDataProp.arraySize = 6;
                for (int i = 0; i < 6; i++)
                {
                    teamDataProp.GetArrayElementAtIndex(i).objectReferenceValue = teamAssets[i];
                }
                serializedTeamManager.ApplyModifiedProperties();
            }

            // UI
            go.AddComponent<UI.UIManager>();

            // Camera
            go.AddComponent<Camera.CameraManager>();

            // Announcer
            var announcerManager = go.AddComponent<Announcer.AnnouncerManager>();
            var announcerConfig = AssetDatabase.LoadAssetAtPath<Announcer.AnnouncerConfig>($"{DataPath}/Audio/AnnouncerConfig.asset");
            if (announcerConfig != null)
            {
                var serializedAnnouncer = new SerializedObject(announcerManager);
                var configProp = serializedAnnouncer.FindProperty("config");
                if (configProp != null)
                {
                    configProp.objectReferenceValue = announcerConfig;
                    serializedAnnouncer.ApplyModifiedProperties();
                }
            }

            // Firebase
            go.AddComponent<Firebase.FirebaseManager>();

            // Networking
            go.AddComponent<Networking.NetworkGameManager>();

            // Save as prefab
            PrefabUtility.SaveAsPrefabAsset(go, prefabAssetPath);
            Object.DestroyImmediate(go);

            Debug.Log("✅ [Minimos Setup] Created GameBootstrap prefab with all manager components wired.");
        }

        // --- Step 5: Player Prefab ---

        [MenuItem("Minimos/Setup Wizard/Create Player Prefab", false, 201)]
        public static void CreatePlayerPrefab()
        {
            string prefabDir = $"{PrefabPath}/Player";
            EnsureDirectory(prefabDir);

            string prefabAssetPath = $"{prefabDir}/MinimoPlayer.prefab";
            if (AssetExists(prefabAssetPath))
            {
                Debug.Log("📝 [Minimos Setup] MinimoPlayer prefab already exists. Skipping.");
                return;
            }

            // Root object with capsule body
            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "MinimoPlayer";

            // Apply TeamColorToon shader if available
            var shader = Shader.Find("Minimos/TeamColorToon");
            if (shader != null)
            {
                var mat = new Material(shader);
                mat.name = "MinimoBody";
                string matPath = "Assets/Minimos/Materials/TeamColors/MinimoBody.mat";
                EnsureDirectory("Assets/Minimos/Materials/TeamColors");
                if (!AssetExists(matPath))
                {
                    AssetDatabase.CreateAsset(mat, matPath);
                }
                player.GetComponent<Renderer>().sharedMaterial =
                    AssetDatabase.LoadAssetAtPath<Material>(matPath);
            }

            // Add NetworkObject
            player.AddComponent<Unity.Netcode.NetworkObject>();

            // Add CharacterController
            var cc = player.GetComponent<CapsuleCollider>();
            if (cc != null) Object.DestroyImmediate(cc); // Remove default collider
            var charController = player.AddComponent<CharacterController>();
            charController.center = new Vector3(0, 1f, 0);
            charController.height = 2f;
            charController.radius = 0.5f;

            // Add player scripts
            player.AddComponent<Player.PlayerController>();
            player.AddComponent<Player.PlayerCombat>();
            var visuals = player.AddComponent<Player.PlayerVisuals>();
            player.AddComponent<Player.PlayerAnimator>();
            player.AddComponent<Player.PlayerSetup>();
            player.AddComponent<PowerUps.PowerUpInventory>();

            // Create attachment point children
            var headSlot = CreateChildTransform(player, "HeadSlot", new Vector3(0, 2.2f, 0));
            var faceSlot = CreateChildTransform(player, "FaceSlot", new Vector3(0, 1.6f, 0.5f));
            var backSlot = CreateChildTransform(player, "BackSlot", new Vector3(0, 1.2f, -0.5f));
            var feetSlot = CreateChildTransform(player, "FeetSlot", new Vector3(0, 0f, 0));

            // Wire attachment points via SerializedObject
            var serializedVisuals = new SerializedObject(visuals);
            SetPropertyRef(serializedVisuals, "headSlot", headSlot.transform);
            SetPropertyRef(serializedVisuals, "faceSlot", faceSlot.transform);
            SetPropertyRef(serializedVisuals, "backSlot", backSlot.transform);
            SetPropertyRef(serializedVisuals, "feetSlot", feetSlot.transform);
            SetPropertyRef(serializedVisuals, "bodyRenderer", player.GetComponent<Renderer>());
            serializedVisuals.ApplyModifiedProperties();

            // Create nameplate (World Space Canvas)
            var nameplateCanvas = new GameObject("NameplateCanvas");
            nameplateCanvas.transform.SetParent(player.transform);
            nameplateCanvas.transform.localPosition = new Vector3(0, 2.5f, 0);
            var canvas = nameplateCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(2f, 0.5f);
            canvas.transform.localScale = Vector3.one * 0.01f;

            // Save prefab
            PrefabUtility.SaveAsPrefabAsset(player, prefabAssetPath);
            Object.DestroyImmediate(player);

            Debug.Log("✅ [Minimos Setup] Created MinimoPlayer prefab with all components and attachment points.");
        }

        // --- Step 6: Projectile Prefab ---

        [MenuItem("Minimos/Setup Wizard/Create Projectile Prefab", false, 202)]
        public static void CreateProjectilePrefab()
        {
            string prefabDir = $"{PrefabPath}/Player";
            EnsureDirectory(prefabDir);

            string prefabAssetPath = $"{prefabDir}/Projectile.prefab";
            if (AssetExists(prefabAssetPath))
            {
                Debug.Log("📝 [Minimos Setup] Projectile prefab already exists. Skipping.");
                return;
            }

            var proj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            proj.name = "Projectile";
            proj.transform.localScale = Vector3.one * 0.3f;

            // Make trigger
            var collider = proj.GetComponent<SphereCollider>();
            collider.isTrigger = true;

            // NetworkObject
            proj.AddComponent<Unity.Netcode.NetworkObject>();

            // Rigidbody (kinematic — movement handled by script)
            var rb = proj.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            // Projectile script
            proj.AddComponent<Player.Projectile>();

            // Trail renderer
            var trail = proj.AddComponent<TrailRenderer>();
            trail.startWidth = 0.15f;
            trail.endWidth = 0f;
            trail.time = 0.3f;
            trail.material = new Material(Shader.Find("Sprites/Default"));
            trail.startColor = Color.white;
            trail.endColor = new Color(1, 1, 1, 0);

            PrefabUtility.SaveAsPrefabAsset(proj, prefabAssetPath);
            Object.DestroyImmediate(proj);

            Debug.Log("✅ [Minimos Setup] Created Projectile prefab.");
        }

        // =============================================
        // HELPERS
        // =============================================

        private static void EnsureDirectory(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
                string folder = Path.GetFileName(path);
                if (parent != null && !AssetDatabase.IsValidFolder(parent))
                {
                    EnsureDirectory(parent);
                }
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
            {
                prop.objectReferenceValue = value;
            }
        }
    }
}
