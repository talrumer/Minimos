using System.IO;
using UnityEditor;
using UnityEngine;
using Minimos.Maps;

namespace Minimos.Editor
{
    /// <summary>
    /// Static utility class for setting up physics layers and default map themes
    /// required by the Minimos Map Generator. Accessible via Minimos > Map Generator menu.
    /// </summary>
    public static class MapGeneratorLayerSetup
    {
        #region Constants

        private const int LAYER_GROUND     = 8;
        private const int LAYER_OBSTACLE   = 9;
        private const int LAYER_DECORATION = 10;
        private const int LAYER_SPAWNZONE  = 11;

        private const string LAYER_NAME_GROUND     = "Ground";
        private const string LAYER_NAME_OBSTACLE   = "Obstacle";
        private const string LAYER_NAME_DECORATION = "Decoration";
        private const string LAYER_NAME_SPAWNZONE  = "SpawnZone";

        private const string PLAYER_PREFAB_PATH =
            "Assets/Minimos/Prefabs/Player/MinimoPlayer.prefab";

        private const string THEMES_FOLDER = "Assets/Minimos/Data/Maps";

        #endregion

        #region Layer Setup

        /// <summary>
        /// Configures physics layers 8-11 for Ground, Obstacle, Decoration, and SpawnZone.
        /// Also wires the PlayerController.groundLayer field on the MinimoPlayer prefab.
        /// </summary>
        [MenuItem("Minimos/Map Generator/Setup Layers")]
        public static void SetupLayers()
        {
            // --- Configure layers in TagManager ---
            var tagManagerAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (tagManagerAssets == null || tagManagerAssets.Length == 0)
            {
                Debug.LogError("🚨 Could not load TagManager.asset!");
                return;
            }

            var tagManager = new SerializedObject(tagManagerAssets[0]);
            var layersProp = tagManager.FindProperty("layers");
            if (layersProp == null)
            {
                Debug.LogError("🚨 Could not find 'layers' property on TagManager!");
                return;
            }

            SetLayer(layersProp, LAYER_GROUND, LAYER_NAME_GROUND);
            SetLayer(layersProp, LAYER_OBSTACLE, LAYER_NAME_OBSTACLE);
            SetLayer(layersProp, LAYER_DECORATION, LAYER_NAME_DECORATION);
            SetLayer(layersProp, LAYER_SPAWNZONE, LAYER_NAME_SPAWNZONE);

            tagManager.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();

            Debug.Log($"✅ Layers configured: " +
                      $"[{LAYER_GROUND}]={LAYER_NAME_GROUND}, " +
                      $"[{LAYER_OBSTACLE}]={LAYER_NAME_OBSTACLE}, " +
                      $"[{LAYER_DECORATION}]={LAYER_NAME_DECORATION}, " +
                      $"[{LAYER_SPAWNZONE}]={LAYER_NAME_SPAWNZONE}");

            // --- Wire PlayerController.groundLayer on MinimoPlayer prefab ---
            WirePlayerGroundLayer();
        }

        /// <summary>
        /// Sets a single layer name in the TagManager layers array.
        /// </summary>
        private static void SetLayer(SerializedProperty layersProp, int index, string name)
        {
            var element = layersProp.GetArrayElementAtIndex(index);
            if (element != null)
            {
                element.stringValue = name;
            }
            else
            {
                Debug.LogWarning($"⚠️ Could not set layer [{index}] — element is null");
            }
        }

        /// <summary>
        /// Loads the MinimoPlayer prefab and sets PlayerController.groundLayer to include the Ground layer.
        /// Uses SerializedObject + FindProperty per UNITY_EDITOR_GOTCHAS.md (camelCase for private fields).
        /// </summary>
        private static void WirePlayerGroundLayer()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PLAYER_PREFAB_PATH);
            if (prefab == null)
            {
                Debug.LogWarning($"⚠️ MinimoPlayer prefab not found at {PLAYER_PREFAB_PATH} — skipping groundLayer wiring");
                return;
            }

            var playerController = prefab.GetComponent<Minimos.Player.PlayerController>();
            if (playerController == null)
            {
                Debug.LogWarning("⚠️ PlayerController component not found on MinimoPlayer prefab — skipping groundLayer wiring");
                return;
            }

            var so = new SerializedObject(playerController);
            var prop = so.FindProperty("groundLayer");
            if (prop != null)
            {
                // Include Ground layer (layer 8) via bitmask OR
                prop.intValue |= (1 << LAYER_GROUND);
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(prefab);
                AssetDatabase.SaveAssets();
                Debug.Log("✅ PlayerController.groundLayer wired to include Ground layer (8)");
            }
            else
            {
                Debug.LogWarning("⚠️ Property 'groundLayer' not found on PlayerController");
            }
        }

        /// <summary>
        /// Checks whether physics layers 8-11 are correctly named.
        /// </summary>
        /// <returns>True if all four layers match expected names.</returns>
        public static bool LayersExist()
        {
            return LayerMask.LayerToName(LAYER_GROUND) == LAYER_NAME_GROUND
                && LayerMask.LayerToName(LAYER_OBSTACLE) == LAYER_NAME_OBSTACLE
                && LayerMask.LayerToName(LAYER_DECORATION) == LAYER_NAME_DECORATION
                && LayerMask.LayerToName(LAYER_SPAWNZONE) == LAYER_NAME_SPAWNZONE;
        }

        #endregion

        #region Default Themes

        /// <summary>
        /// Creates 6 default MapThemeConfig ScriptableObjects in Assets/Minimos/Data/Maps/.
        /// Themes include both asset-pack-based and basic-primitive fallback configurations.
        /// </summary>
        [MenuItem("Minimos/Map Generator/Create Default Themes")]
        public static void CreateDefaultThemes()
        {
            EnsureDirectory(THEMES_FOLDER);

            // --- Sunny Meadows (asset-pack) ---
            CreateTheme("sunny_meadows", "Sunny Meadows", config =>
            {
                SetColor(config, "groundColor", new Color(0.45f, 0.75f, 0.35f));
                SetColor(config, "ambientLightColor", new Color(1.0f, 0.95f, 0.85f));
                SetBool(config, "useBasicPrimitives", false);

                const string naturePath = "Assets/SimpleNaturePack/Prefabs";
                SetPrefabArray(config, "treePrefabs", naturePath, "Tree_");
                SetPrefabArray(config, "rockPrefabs", naturePath, "Rock_");
                SetPrefabArray(config, "decorationPrefabs", naturePath,
                    "Flowers_", "Grass_", "Bush_", "Mushroom_");
            });

            // --- Coral Cove (asset-pack) ---
            CreateTheme("coral_cove", "Coral Cove", config =>
            {
                SetColor(config, "groundColor", new Color(0.85f, 0.8f, 0.6f));
                SetColor(config, "ambientLightColor", new Color(1.0f, 0.98f, 0.9f));
                SetBool(config, "useBasicPrimitives", false);

                const string beachPath = "Assets/Aquaset/LowPolyTropicalBeach/Prefabs";
                SetPrefabArray(config, "treePrefabs", beachPath, "Palm");
                SetPrefabArray(config, "rockPrefabs", beachPath, "SandCastle", "Boat", "Dock");
                SetPrefabArray(config, "decorationPrefabs", beachPath,
                    "Umbrella", "Chair", "Bush", "Fence", "BeachBall", "SurfBoard", "LifeSaver", "Bucket");
            });

            // --- Cozy Villa (asset-pack) ---
            CreateTheme("cozy_villa", "Cozy Villa", config =>
            {
                SetColor(config, "groundColor", new Color(0.75f, 0.65f, 0.5f));
                SetColor(config, "ambientLightColor", new Color(1.0f, 0.9f, 0.75f));
                SetBool(config, "useBasicPrimitives", false);

                const string livingPath = "Assets/LowPolyLivingRoomPack/Prefabs";
                SetPrefabArray(config, "treePrefabs", livingPath, "PottedPlant_Tall", "Bookshelf");
                SetPrefabArray(config, "rockPrefabs", livingPath, "Sofa", "Armchair", "Coffee_table");
                SetPrefabArray(config, "decorationPrefabs", livingPath,
                    "Carpet", "Lamp", "Vase", "Books", "Pillow", "Mug", "Stool");
            });

            // --- Dusty Gulch (basic primitives) ---
            CreateTheme("dusty_gulch", "Dusty Gulch", config =>
            {
                SetBool(config, "useBasicPrimitives", true);
                SetColor(config, "groundColor", new Color(0.8f, 0.65f, 0.4f));
                SetColor(config, "basicTreeColor", new Color(0.6f, 0.5f, 0.2f));
                SetColor(config, "basicRockColor", new Color(0.7f, 0.4f, 0.3f));
                SetColor(config, "basicDecorationColor", new Color(0.85f, 0.75f, 0.5f));
            });

            // --- Candy Castle (basic primitives) ---
            CreateTheme("candy_castle", "Candy Castle", config =>
            {
                SetBool(config, "useBasicPrimitives", true);
                SetColor(config, "groundColor", new Color(1.0f, 0.8f, 0.85f));
                SetColor(config, "basicTreeColor", new Color(0.4f, 0.9f, 0.4f));
                SetColor(config, "basicRockColor", new Color(0.95f, 0.6f, 0.7f));
                SetColor(config, "basicDecorationColor", new Color(1.0f, 0.95f, 0.4f));
            });

            // --- Neon Nights (basic primitives) ---
            CreateTheme("neon_nights", "Neon Nights", config =>
            {
                SetBool(config, "useBasicPrimitives", true);
                SetColor(config, "groundColor", new Color(0.1f, 0.05f, 0.15f));
                SetColor(config, "basicTreeColor", new Color(0.0f, 0.9f, 1.0f));
                SetColor(config, "basicRockColor", new Color(0.9f, 0.0f, 1.0f));
                SetColor(config, "basicDecorationColor", new Color(1.0f, 0.3f, 0.5f));
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("✅ Created 6 default map themes in " + THEMES_FOLDER);
        }

        #endregion

        #region Theme Helpers

        /// <summary>
        /// Creates a single MapThemeConfig asset, applying properties via a configurator callback.
        /// Per UNITY_EDITOR_GOTCHAS.md: CreateAsset, SaveAssets, reload, then modify via SerializedObject.
        /// </summary>
        private static void CreateTheme(string themeId, string themeName,
            System.Action<SerializedObject> configurator)
        {
            string path = $"{THEMES_FOLDER}/{themeName.Replace(" ", "")}.asset";

            var instance = ScriptableObject.CreateInstance<MapThemeConfig>();
            AssetDatabase.CreateAsset(instance, path);
            AssetDatabase.SaveAssets();

            // Reload from disk per gotchas doc
            instance = AssetDatabase.LoadAssetAtPath<MapThemeConfig>(path);
            var so = new SerializedObject(instance);

            // Set identity fields (camelCase — private [SerializeField])
            SetString(so, "themeId", themeId);
            SetString(so, "themeName", themeName);

            // Apply theme-specific configuration
            configurator(so);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(instance);

            Debug.Log($"📝 Created theme: {themeName} ({themeId}) at {path}");
        }

        /// <summary>Sets a string property on the SerializedObject.</summary>
        private static void SetString(SerializedObject so, string propName, string value)
        {
            var prop = so.FindProperty(propName);
            if (prop != null)
                prop.stringValue = value;
            else
                Debug.LogWarning($"⚠️ Property '{propName}' not found on {so.targetObject.GetType().Name}");
        }

        /// <summary>Sets a Color property on the SerializedObject.</summary>
        private static void SetColor(SerializedObject so, string propName, Color value)
        {
            var prop = so.FindProperty(propName);
            if (prop != null)
                prop.colorValue = value;
            else
                Debug.LogWarning($"⚠️ Property '{propName}' not found on {so.targetObject.GetType().Name}");
        }

        /// <summary>Sets a bool property on the SerializedObject.</summary>
        private static void SetBool(SerializedObject so, string propName, bool value)
        {
            var prop = so.FindProperty(propName);
            if (prop != null)
                prop.boolValue = value;
            else
                Debug.LogWarning($"⚠️ Property '{propName}' not found on {so.targetObject.GetType().Name}");
        }

        /// <summary>
        /// Finds prefabs in a folder matching any of the given name prefixes and assigns them
        /// to a GameObject[] SerializedProperty array.
        /// </summary>
        private static void SetPrefabArray(SerializedObject so, string propName,
            string folderPath, params string[] namePrefixes)
        {
            var prop = so.FindProperty(propName);
            if (prop == null)
            {
                Debug.LogWarning($"⚠️ Property '{propName}' not found on {so.targetObject.GetType().Name}");
                return;
            }

            var prefabs = FindPrefabsByPrefix(folderPath, namePrefixes);

            prop.arraySize = prefabs.Length;
            for (int i = 0; i < prefabs.Length; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = prefabs[i];
            }

            if (prefabs.Length == 0)
            {
                Debug.LogWarning($"⚠️ No prefabs found in '{folderPath}' matching prefixes: " +
                                 string.Join(", ", namePrefixes) +
                                 " — theme will use basic primitives at runtime");
            }
        }

        /// <summary>
        /// Searches a folder for prefab assets whose filename starts with any of the given prefixes.
        /// Uses AssetDatabase.FindAssets which searches all subfolders recursively.
        /// </summary>
        private static GameObject[] FindPrefabsByPrefix(string folderPath, string[] prefixes)
        {
            if (!AssetDatabase.IsValidFolder(folderPath))
                return new GameObject[0];

            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });
            var results = new System.Collections.Generic.List<GameObject>();

            foreach (var guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileNameWithoutExtension(assetPath);

                foreach (var prefix in prefixes)
                {
                    if (fileName.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
                    {
                        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                        if (prefab != null)
                            results.Add(prefab);
                        break; // Don't add same prefab twice for multiple matching prefixes
                    }
                }
            }

            return results.ToArray();
        }

        /// <summary>
        /// Recursively ensures a folder path exists in the AssetDatabase.
        /// Unity requires parent folders to exist before creating child folders.
        /// </summary>
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

        #endregion
    }
}
