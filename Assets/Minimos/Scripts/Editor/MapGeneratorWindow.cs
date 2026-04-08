using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Minimos.Maps;
using Minimos.Teams;

namespace Minimos.Editor
{
    /// <summary>
    /// EditorWindow for generating procedural party game maps in the Minimos project.
    /// Supports themed prop placement, spawn point distribution, and terrain generation.
    /// Open via Minimos > Map Generator > Open Generator.
    /// </summary>
    public class MapGeneratorWindow : EditorWindow
    {
        #region Enums

        /// <summary>Ground mesh generation mode.</summary>
        private enum GroundType
        {
            FlatPlane,
            GentleHills
        }

        /// <summary>Prop density preset — auto-calculates tree/rock/decoration counts.</summary>
        private enum PropDensity
        {
            Sparse,
            Normal,
            Dense
        }

        #endregion

        #region Fields

        private MapThemeConfig selectedTheme;
        private MapThemeConfig[] availableThemes;
        private int selectedThemeIndex;

        private Vector2Int mapSize = new(60, 60);
        private GroundType groundType = GroundType.FlatPlane;
        private PropDensity density = PropDensity.Normal;
        private int treeCount;
        private int rockCount;
        private int decorationCount;

        private bool includeBoundaries = true;
        private int teamCount = 4;
        private int powerUpSpawnCount = 6;
        private int flagSpawnCount = 3;
        private int seed;

        private Vector2 scrollPos;

        // Cached for prop exclusion zones during generation
        private List<Vector3> teamSpawnPositions = new();

        #endregion

        #region Window Lifecycle

        /// <summary>Opens the Map Generator EditorWindow.</summary>
        [MenuItem("Minimos/Map Generator/Open Generator")]
        public static void ShowWindow()
        {
            var window = GetWindow<MapGeneratorWindow>("Map Generator");
            window.minSize = new Vector2(380, 600);
            window.Show();
        }

        /// <summary>Loads available themes and sets defaults on window open.</summary>
        private void OnEnable()
        {
            LoadAvailableThemes();
            RecalculateCounts();
        }

        /// <summary>Finds all MapThemeConfig assets in the project.</summary>
        private void LoadAvailableThemes()
        {
            var guids = AssetDatabase.FindAssets("t:MapThemeConfig");
            var themes = new List<MapThemeConfig>();

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var theme = AssetDatabase.LoadAssetAtPath<MapThemeConfig>(path);
                if (theme != null)
                    themes.Add(theme);
            }

            availableThemes = themes.ToArray();

            if (availableThemes.Length > 0)
            {
                selectedThemeIndex = Mathf.Clamp(selectedThemeIndex, 0, availableThemes.Length - 1);
                selectedTheme = availableThemes[selectedThemeIndex];
            }
        }

        #endregion

        #region OnGUI

        /// <summary>Draws the Map Generator window UI.</summary>
        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            DrawHeader();
            EditorGUILayout.Space(8);
            DrawThemeSection();
            EditorGUILayout.Space(4);
            DrawMapSizeSection();
            EditorGUILayout.Space(4);
            DrawGroundTypeSection();
            EditorGUILayout.Space(4);
            DrawPropsSection();
            EditorGUILayout.Space(4);
            DrawBoundariesToggle();
            EditorGUILayout.Space(4);
            DrawSpawnPointsSection();
            EditorGUILayout.Space(4);
            DrawSeedField();
            EditorGUILayout.Space(12);
            DrawButtons();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            // Title
            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter,
                richText = true
            };
            EditorGUILayout.LabelField("<color=#74B9FF>🗺️ Minimos Map Generator</color>", titleStyle,
                GUILayout.Height(36));

            // Subtitle
            var subtitleStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel) { richText = true };
            EditorGUILayout.LabelField("Generate procedural party game maps in one click", subtitleStyle);

            // Separator
            EditorGUILayout.Space(4);
            var rect = EditorGUILayout.GetControlRect(false, 2);
            EditorGUI.DrawRect(rect, new Color(0.45f, 0.72f, 1f, 0.4f));
        }

        private void DrawThemeSection()
        {
            DrawSectionHeader("🎨 Theme");

            if (availableThemes == null || availableThemes.Length == 0)
            {
                EditorGUILayout.HelpBox(
                    "No MapThemeConfig assets found.\nRun Minimos → Map Generator → Create Default Themes first.",
                    MessageType.Warning);
                var prev = GUI.backgroundColor;
                GUI.backgroundColor = new Color(1f, 0.85f, 0.4f);
                if (GUILayout.Button("⚡ Create Default Themes", GUILayout.Height(26)))
                    MapGeneratorLayerSetup.CreateDefaultThemes();
                GUI.backgroundColor = prev;
                return;
            }

            // Theme emoji mapping for dropdown
            string[] themeNames = new string[availableThemes.Length];
            for (int i = 0; i < availableThemes.Length; i++)
            {
                string name = availableThemes[i].ThemeName ?? availableThemes[i].name;
                string emoji = name switch
                {
                    "Sunny Meadows" => "🌿",
                    "Coral Cove" => "🏖️",
                    "Cozy Villa" => "🏠",
                    "Dusty Gulch" => "🤠",
                    "Candy Castle" => "🍬",
                    "Neon Nights" => "🌙",
                    _ => "🗺️"
                };
                themeNames[i] = $"{emoji} {name}";
            }

            int newIndex = EditorGUILayout.Popup("Active Theme", selectedThemeIndex, themeNames);
            if (newIndex != selectedThemeIndex)
            {
                selectedThemeIndex = newIndex;
                selectedTheme = availableThemes[selectedThemeIndex];
            }

            // ObjectField preview (read-only)
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Preview", selectedTheme, typeof(MapThemeConfig), false);
            EditorGUI.EndDisabledGroup();

            // Theme info
            if (selectedTheme != null && selectedTheme.UseBasicPrimitives)
            {
                EditorGUILayout.HelpBox(
                    "This theme uses colored primitives (no asset pack). Great for prototyping!",
                    MessageType.Info);
            }
        }

        private void DrawMapSizeSection()
        {
            DrawSectionHeader("📐 Map Size");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("X", GUILayout.Width(14));
            mapSize.x = EditorGUILayout.IntField(mapSize.x);
            EditorGUILayout.LabelField("Y", GUILayout.Width(14));
            mapSize.y = EditorGUILayout.IntField(mapSize.y);
            EditorGUILayout.EndHorizontal();
            mapSize.x = Mathf.Clamp(mapSize.x, 20, 200);
            mapSize.y = Mathf.Clamp(mapSize.y, 20, 200);

            float area = mapSize.x * mapSize.y;
            var infoStyle = new GUIStyle(EditorStyles.miniLabel) { richText = true };
            EditorGUILayout.LabelField($"  <color=#888>Area: {area:N0} units² ({(area / 3600f):F1}x default)</color>", infoStyle);
        }

        private void DrawGroundTypeSection()
        {
            DrawSectionHeader("⛰️ Ground");
            groundType = (GroundType)EditorGUILayout.EnumPopup("Type", groundType);
        }

        private void DrawPropsSection()
        {
            var prevBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.4f, 0.75f, 0.4f, 0.15f);
            EditorGUILayout.BeginVertical("box");
            GUI.backgroundColor = prevBg;

            DrawSectionHeader("🌳 Props");

            var newDensity = (PropDensity)EditorGUILayout.EnumPopup("Density", density);
            if (newDensity != density)
            {
                density = newDensity;
                RecalculateCounts();
            }

            EditorGUILayout.Space(2);
            treeCount = EditorGUILayout.IntSlider("🌲 Trees", treeCount, 0, 200);
            rockCount = EditorGUILayout.IntSlider("🪨 Rocks", rockCount, 0, 200);
            decorationCount = EditorGUILayout.IntSlider("🌸 Decorations", decorationCount, 0, 300);

            // Total count
            int total = treeCount + rockCount + decorationCount;
            var totalStyle = new GUIStyle(EditorStyles.miniLabel) { richText = true, alignment = TextAnchor.MiddleRight };
            EditorGUILayout.LabelField($"<color=#888>Total: {total} props</color>", totalStyle);

            EditorGUILayout.EndVertical();
        }

        private void DrawBoundariesToggle()
        {
            includeBoundaries = EditorGUILayout.Toggle("🧱 Include Boundaries", includeBoundaries);
        }

        private void DrawSpawnPointsSection()
        {
            var prevBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.4f, 0.5f, 0.9f, 0.15f);
            EditorGUILayout.BeginVertical("box");
            GUI.backgroundColor = prevBg;

            DrawSectionHeader("📍 Spawn Points");

            teamCount = EditorGUILayout.IntSlider("🏳️ Teams", teamCount, 2, 6);
            powerUpSpawnCount = EditorGUILayout.IntSlider("⚡ Power-Ups", powerUpSpawnCount, 0, 20);
            flagSpawnCount = EditorGUILayout.IntSlider("🚩 Flags", flagSpawnCount, 0, 10);
            EditorGUILayout.EndVertical();
        }

        private void DrawSeedField()
        {
            EditorGUILayout.BeginHorizontal();
            seed = EditorGUILayout.IntField("🎲 Seed", seed);
            if (GUILayout.Button("Random", GUILayout.Width(60)))
                seed = Random.Range(1, 99999);
            EditorGUILayout.EndHorizontal();

            var hintStyle = new GUIStyle(EditorStyles.miniLabel) { richText = true };
            EditorGUILayout.LabelField("  <color=#888>0 = random seed each time</color>", hintStyle);
        }

        /// <summary>Draws a colored section header label.</summary>
        private void DrawSectionHeader(string label)
        {
            var style = new GUIStyle(EditorStyles.boldLabel) { richText = true, fontSize = 12 };
            EditorGUILayout.LabelField(label, style);
        }

        private void DrawButtons()
        {
            EditorGUILayout.BeginHorizontal();

            // Generate button — green tinted
            var prevColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.5f, 1f, 0.5f);
            if (GUILayout.Button("\ud83c\udfb2 Generate Map", GUILayout.Height(32)))
                GenerateMap();
            GUI.backgroundColor = prevColor;

            // Clear button — red tinted
            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("\ud83d\uddd1\ufe0f Clear Map", GUILayout.Height(32)))
                ClearMap();
            GUI.backgroundColor = prevColor;

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Density Calculation

        /// <summary>
        /// Recalculates prop counts based on map area and selected density preset.
        /// Formula: count = (int)(multiplier * areaFactor) where areaFactor = area / 3600.
        /// </summary>
        private void RecalculateCounts()
        {
            float areaFactor = (mapSize.x * mapSize.y) / 3600f;

            switch (density)
            {
                case PropDensity.Sparse:
                    treeCount       = (int)(8  * areaFactor);
                    rockCount       = (int)(6  * areaFactor);
                    decorationCount = (int)(12 * areaFactor);
                    break;
                case PropDensity.Normal:
                    treeCount       = (int)(20 * areaFactor);
                    rockCount       = (int)(15 * areaFactor);
                    decorationCount = (int)(30 * areaFactor);
                    break;
                case PropDensity.Dense:
                    treeCount       = (int)(40 * areaFactor);
                    rockCount       = (int)(30 * areaFactor);
                    decorationCount = (int)(60 * areaFactor);
                    break;
            }
        }

        #endregion

        #region Map Generation

        /// <summary>
        /// Main entry point — generates a complete map with ground, boundaries,
        /// spawn points, props, and atmosphere based on current settings.
        /// </summary>
        private void GenerateMap()
        {
            // Validate layers
            if (!MapGeneratorLayerSetup.LayersExist())
            {
                bool runSetup = EditorUtility.DisplayDialog(
                    "Layers Not Configured",
                    "Physics layers 8-11 are not set up.\nRun Setup Layers now?",
                    "Setup Layers", "Cancel");

                if (runSetup)
                    MapGeneratorLayerSetup.SetupLayers();
                else
                    return;
            }

            // Validate theme
            if (selectedTheme == null)
            {
                EditorUtility.DisplayDialog("No Theme",
                    "Please select a map theme before generating.", "OK");
                return;
            }

            ClearMap();

            // Init random state from seed
            Random.InitState(seed);

            string themeName = selectedTheme.ThemeName ?? "Untitled";
            string rootName = $"Map_{themeName.Replace(" ", "")}";
            var rootGo = new GameObject(rootName);
            Undo.RegisterCreatedObjectUndo(rootGo, $"Generate Map ({themeName})");

            try
            {
                EditorUtility.DisplayProgressBar("Generating Map", "Creating ground...", 0.1f);
                CreateGround(rootGo.transform, out var groundObj);

                EditorUtility.DisplayProgressBar("Generating Map", "Creating boundaries...", 0.3f);
                if (includeBoundaries)
                    CreateBoundaries(rootGo.transform);

                EditorUtility.DisplayProgressBar("Generating Map", "Placing spawn points...", 0.5f);
                CreateSpawnPoints(rootGo.transform);

                EditorUtility.DisplayProgressBar("Generating Map", "Placing props...", 0.7f);
                CreateProps(rootGo.transform, groundObj);

                EditorUtility.DisplayProgressBar("Generating Map", "Applying atmosphere...", 0.9f);
                ApplyAtmosphere();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"✅ Map generated: {rootName} ({mapSize.x}x{mapSize.y}) with theme '{themeName}'");
        }

        #endregion

        #region Ground

        /// <summary>
        /// Creates the ground surface — either a flat Unity plane or a Perlin-noise hills mesh.
        /// </summary>
        private void CreateGround(Transform parent, out GameObject groundObj)
        {
            if (groundType == GroundType.FlatPlane)
            {
                groundObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
                groundObj.name = "Ground";
                groundObj.transform.SetParent(parent);
                groundObj.transform.localPosition = Vector3.zero;
                // Unity plane is 10x10 units by default, so scale = mapSize / 10
                groundObj.transform.localScale = new Vector3(
                    mapSize.x / 10f, 1f, mapSize.y / 10f);
                groundObj.layer = 8; // Ground
            }
            else // GentleHills
            {
                groundObj = new GameObject("Ground");
                groundObj.transform.SetParent(parent);
                groundObj.transform.localPosition = Vector3.zero;
                groundObj.layer = 8;

                var mesh = GenerateHillsMesh();
                var mf = groundObj.AddComponent<MeshFilter>();
                mf.sharedMesh = mesh;
                groundObj.AddComponent<MeshRenderer>();
                var mc = groundObj.AddComponent<MeshCollider>();
                mc.sharedMesh = mesh;
            }

            // Apply material
            var renderer = groundObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                if (selectedTheme.GroundMaterial != null)
                {
                    renderer.sharedMaterial = selectedTheme.GroundMaterial;
                }
                else
                {
                    // Create a URP Lit material with ground color
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    mat.color = selectedTheme.GroundColor;
                    mat.name = "Ground_Generated";
                    renderer.sharedMaterial = mat;
                }
            }

            Undo.RegisterCreatedObjectUndo(groundObj, "Create Ground");
        }

        /// <summary>
        /// Generates a mesh with Perlin noise height for gentle hills terrain.
        /// Resolution: 1 vertex per unit. Y offset by Perlin noise scaled to 2.5 units max.
        /// </summary>
        private Mesh GenerateHillsMesh()
        {
            int xSize = mapSize.x;
            int zSize = mapSize.y;
            float halfX = xSize / 2f;
            float halfZ = zSize / 2f;

            // Vertices: (xSize+1) * (zSize+1)
            var vertices = new Vector3[(xSize + 1) * (zSize + 1)];
            var uvs = new Vector2[vertices.Length];

            for (int z = 0; z <= zSize; z++)
            {
                for (int x = 0; x <= xSize; x++)
                {
                    int i = z * (xSize + 1) + x;
                    float worldX = x - halfX;
                    float worldZ = z - halfZ;
                    float y = Mathf.PerlinNoise(x * 0.05f + seed, z * 0.05f + seed) * 2.5f;
                    vertices[i] = new Vector3(worldX, y, worldZ);
                    uvs[i] = new Vector2((float)x / xSize, (float)z / zSize);
                }
            }

            // Triangles: 2 per quad, 6 indices per quad
            var triangles = new int[xSize * zSize * 6];
            int t = 0;
            for (int z = 0; z < zSize; z++)
            {
                for (int x = 0; x < xSize; x++)
                {
                    int i = z * (xSize + 1) + x;
                    triangles[t++] = i;
                    triangles[t++] = i + xSize + 1;
                    triangles[t++] = i + 1;
                    triangles[t++] = i + 1;
                    triangles[t++] = i + xSize + 1;
                    triangles[t++] = i + xSize + 2;
                }
            }

            var mesh = new Mesh { name = "HillsGround" };
            // Use 32-bit indices for large maps
            if (vertices.Length > 65535)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        #endregion

        #region Boundaries

        /// <summary>
        /// Creates 4 invisible box colliders around the map perimeter to keep players in bounds.
        /// Height = 10 units. No renderers — collision only.
        /// </summary>
        private void CreateBoundaries(Transform parent)
        {
            var boundaryRoot = new GameObject("Boundaries");
            boundaryRoot.transform.SetParent(parent);
            Undo.RegisterCreatedObjectUndo(boundaryRoot, "Create Boundaries");

            float halfX = mapSize.x / 2f;
            float halfZ = mapSize.y / 2f;
            float wallHeight = 10f;
            float wallThickness = 1f;

            // North (+Z)
            CreateWall(boundaryRoot.transform, "Wall_North",
                new Vector3(0, wallHeight / 2f, halfZ + wallThickness / 2f),
                new Vector3(mapSize.x + wallThickness * 2, wallHeight, wallThickness));

            // South (-Z)
            CreateWall(boundaryRoot.transform, "Wall_South",
                new Vector3(0, wallHeight / 2f, -halfZ - wallThickness / 2f),
                new Vector3(mapSize.x + wallThickness * 2, wallHeight, wallThickness));

            // East (+X)
            CreateWall(boundaryRoot.transform, "Wall_East",
                new Vector3(halfX + wallThickness / 2f, wallHeight / 2f, 0),
                new Vector3(wallThickness, wallHeight, mapSize.y + wallThickness * 2));

            // West (-X)
            CreateWall(boundaryRoot.transform, "Wall_West",
                new Vector3(-halfX - wallThickness / 2f, wallHeight / 2f, 0),
                new Vector3(wallThickness, wallHeight, mapSize.y + wallThickness * 2));
        }

        /// <summary>Creates a single invisible boundary wall with a box collider.</summary>
        private static void CreateWall(Transform parent, string name, Vector3 position, Vector3 size)
        {
            var wall = new GameObject(name);
            wall.transform.SetParent(parent);
            wall.transform.localPosition = position;
            var col = wall.AddComponent<BoxCollider>();
            col.size = size;
            // Default layer (0) — boundaries don't need a special layer
        }

        #endregion

        #region Spawn Points

        /// <summary>
        /// Creates team spawn points, power-up spawn points, and flag spawn points.
        /// Team spawns are distributed evenly around the perimeter at 70% radius.
        /// Flag spawns at 40% radius. Power-ups are random in the interior.
        /// </summary>
        private void CreateSpawnPoints(Transform parent)
        {
            var spawnRoot = new GameObject("SpawnPoints");
            spawnRoot.transform.SetParent(parent);
            Undo.RegisterCreatedObjectUndo(spawnRoot, "Create Spawn Points");

            teamSpawnPositions.Clear();
            float radius70 = Mathf.Min(mapSize.x, mapSize.y) / 2f * 0.7f;

            // --- Team Spawns ---
            var teamRoot = new GameObject("TeamSpawns");
            teamRoot.transform.SetParent(spawnRoot.transform);

            for (int i = 0; i < teamCount; i++)
            {
                float angle = (360f / teamCount) * i * Mathf.Deg2Rad;
                var pos = new Vector3(
                    Mathf.Cos(angle) * radius70,
                    0f,
                    Mathf.Sin(angle) * radius70);

                var spawnGo = new GameObject($"Team{i}_Spawn");
                spawnGo.transform.SetParent(teamRoot.transform);
                spawnGo.transform.localPosition = pos;
                spawnGo.layer = 11; // SpawnZone

                var trigger = spawnGo.AddComponent<SphereCollider>();
                trigger.isTrigger = true;
                trigger.radius = 5f;

                // Debug visualization sphere
                var debugSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                debugSphere.name = "DebugMarker";
                debugSphere.transform.SetParent(spawnGo.transform);
                debugSphere.transform.localPosition = Vector3.zero;
                debugSphere.transform.localScale = Vector3.one * 0.5f;

                // Remove collider from debug sphere — it's purely visual
                var sphereCol = debugSphere.GetComponent<Collider>();
                if (sphereCol != null) Object.DestroyImmediate(sphereCol);

                // Apply team color
                var rend = debugSphere.GetComponent<Renderer>();
                if (rend != null)
                {
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    mat.color = TeamColors.GetPrimary(i);
                    mat.name = $"TeamColor_{i}";
                    rend.sharedMaterial = mat;
                }

                teamSpawnPositions.Add(pos);
            }

            // --- Power-Up Spawns ---
            var powerUpRoot = new GameObject("PowerUpSpawns");
            powerUpRoot.transform.SetParent(spawnRoot.transform);

            float edgeBuffer = 10f;
            float minDistFromTeam = 8f;
            var halfX = mapSize.x / 2f;
            var halfZ = mapSize.y / 2f;

            for (int i = 0; i < powerUpSpawnCount; i++)
            {
                Vector3 pos;
                int attempts = 0;
                do
                {
                    pos = new Vector3(
                        Random.Range(-halfX + edgeBuffer, halfX - edgeBuffer),
                        0f,
                        Random.Range(-halfZ + edgeBuffer, halfZ - edgeBuffer));
                    attempts++;
                } while (attempts < 100 && IsTooCloseToAny(pos, teamSpawnPositions, minDistFromTeam));

                var spawnGo = new GameObject($"PowerUp_Spawn_{i}");
                spawnGo.transform.SetParent(powerUpRoot.transform);
                spawnGo.transform.localPosition = pos;
                spawnGo.layer = 11; // SpawnZone

                var trigger = spawnGo.AddComponent<SphereCollider>();
                trigger.isTrigger = true;
                trigger.radius = 1f;
            }

            // --- Flag Spawns ---
            var flagRoot = new GameObject("FlagSpawns");
            flagRoot.transform.SetParent(spawnRoot.transform);

            float radius40 = Mathf.Min(mapSize.x, mapSize.y) / 2f * 0.4f;
            for (int i = 0; i < flagSpawnCount; i++)
            {
                float angle = (360f / flagSpawnCount) * i * Mathf.Deg2Rad;
                var pos = new Vector3(
                    Mathf.Cos(angle) * radius40,
                    0f,
                    Mathf.Sin(angle) * radius40);

                var spawnGo = new GameObject($"Flag_Spawn_{i}");
                spawnGo.transform.SetParent(flagRoot.transform);
                spawnGo.transform.localPosition = pos;
                spawnGo.layer = 11; // SpawnZone

                var trigger = spawnGo.AddComponent<SphereCollider>();
                trigger.isTrigger = true;
                trigger.radius = 1f;
            }
        }

        #endregion

        #region Props

        /// <summary>
        /// Places trees, rocks, and decorations according to the selected theme.
        /// Uses raycasting to ground for Y position and enforces minimum spacing.
        /// </summary>
        private void CreateProps(Transform parent, GameObject groundObj)
        {
            var propsRoot = new GameObject("Props");
            propsRoot.transform.SetParent(parent);
            Undo.RegisterCreatedObjectUndo(propsRoot, "Create Props");

            float halfX = mapSize.x / 2f;
            float halfZ = mapSize.y / 2f;
            var bounds = new Rect(-halfX, -halfZ, mapSize.x, mapSize.y);

            // Exclusion zones around team spawns
            float exclusionRadius = 8f;

            // --- Trees (layer 9 = Obstacle) ---
            var treeRoot = new GameObject("Trees");
            treeRoot.transform.SetParent(propsRoot.transform);

            var treePositions = PlaceWithMinDistance(
                treeCount, 5f, bounds, teamSpawnPositions, exclusionRadius);
            PlacePropCategory(treeRoot.transform, treePositions,
                selectedTheme.TreePrefabs, 9, true, CreateBasicTree);

            // --- Rocks (layer 9 = Obstacle) ---
            var rockRoot = new GameObject("Rocks");
            rockRoot.transform.SetParent(propsRoot.transform);

            var rockPositions = PlaceWithMinDistance(
                rockCount, 3f, bounds, teamSpawnPositions, exclusionRadius);
            PlacePropCategory(rockRoot.transform, rockPositions,
                selectedTheme.RockPrefabs, 9, true, CreateBasicRock);

            // --- Decorations (layer 10 = Decoration) ---
            var decorRoot = new GameObject("Decorations");
            decorRoot.transform.SetParent(propsRoot.transform);

            var decorPositions = PlaceWithMinDistance(
                decorationCount, 1.5f, bounds, teamSpawnPositions, exclusionRadius);
            PlacePropCategory(decorRoot.transform, decorPositions,
                selectedTheme.DecorationPrefabs, 10, false, CreateBasicDecoration);
        }

        /// <summary>
        /// Places a category of props at the given positions, using prefabs or basic primitives.
        /// </summary>
        /// <param name="parent">Parent transform for placed objects.</param>
        /// <param name="positions">World-space XZ positions (Y is raycast to ground).</param>
        /// <param name="prefabs">Theme prefabs to instantiate; null/empty triggers basic primitives.</param>
        /// <param name="layer">Physics layer to assign recursively.</param>
        /// <param name="ensureCollider">If true, add BoxCollider if none exists (obstacles).</param>
        /// <param name="basicFactory">Fallback factory for basic primitive creation.</param>
        private void PlacePropCategory(Transform parent, List<Vector3> positions,
            GameObject[] prefabs, int layer, bool ensureCollider,
            System.Func<GameObject> basicFactory)
        {
            bool usePrimitives = selectedTheme.UseBasicPrimitives
                                 || prefabs == null
                                 || prefabs.Length == 0;

            for (int i = 0; i < positions.Count; i++)
            {
                GameObject prop;

                if (usePrimitives)
                {
                    prop = basicFactory();
                }
                else
                {
                    var prefab = prefabs[Random.Range(0, prefabs.Length)];
                    prop = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                }

                if (prop == null) continue;

                // Raycast down from high up to find ground Y
                var pos = positions[i];
                float groundY = 0f;
                if (Physics.Raycast(
                        new Vector3(pos.x, 100f, pos.z), Vector3.down,
                        out var hit, 200f, 1 << 8)) // Layer 8 = Ground
                {
                    groundY = hit.point.y;
                }
                prop.transform.position = new Vector3(pos.x, groundY, pos.z);

                // Random Y rotation
                prop.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

                // Random scale within theme range
                float scale = Random.Range(
                    selectedTheme.PropScaleRange.x,
                    selectedTheme.PropScaleRange.y);
                prop.transform.localScale = Vector3.one * scale;

                // Set layer recursively
                SetLayerRecursive(prop, layer);

                // Obstacle: ensure collider
                if (ensureCollider && prop.GetComponent<Collider>() == null)
                    prop.AddComponent<BoxCollider>();

                // Decoration: remove all colliders
                if (!ensureCollider)
                {
                    foreach (var col in prop.GetComponentsInChildren<Collider>())
                        Object.DestroyImmediate(col);
                }

                prop.transform.SetParent(parent);
            }
        }

        /// <summary>Creates a basic primitive tree: cylinder trunk + sphere canopy.</summary>
        private GameObject CreateBasicTree()
        {
            var root = new GameObject("BasicTree");

            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(root.transform);
            trunk.transform.localPosition = new Vector3(0, 1f, 0);
            trunk.transform.localScale = new Vector3(0.3f, 1f, 0.3f);
            var trunkRend = trunk.GetComponent<Renderer>();
            if (trunkRend != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = new Color(0.45f, 0.3f, 0.15f); // Brown
                trunkRend.sharedMaterial = mat;
            }

            var canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            canopy.name = "Canopy";
            canopy.transform.SetParent(root.transform);
            canopy.transform.localPosition = new Vector3(0, 2.5f, 0);
            canopy.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            var canopyRend = canopy.GetComponent<Renderer>();
            if (canopyRend != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = selectedTheme.BasicTreeColor;
                canopyRend.sharedMaterial = mat;
            }

            return root;
        }

        /// <summary>Creates a basic primitive rock: scaled cube with rounded feel.</summary>
        private GameObject CreateBasicRock()
        {
            var rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rock.name = "BasicRock";
            rock.transform.localScale = new Vector3(
                Random.Range(0.8f, 1.5f),
                Random.Range(0.5f, 1.0f),
                Random.Range(0.8f, 1.5f));

            var rend = rock.GetComponent<Renderer>();
            if (rend != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = selectedTheme.BasicRockColor;
                rend.sharedMaterial = mat;
            }

            return rock;
        }

        /// <summary>Creates a basic primitive decoration: small sphere.</summary>
        private GameObject CreateBasicDecoration()
        {
            var decor = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            decor.name = "BasicDecoration";
            decor.transform.localScale = Vector3.one * Random.Range(0.2f, 0.5f);

            var rend = decor.GetComponent<Renderer>();
            if (rend != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = selectedTheme.BasicDecorationColor;
                rend.sharedMaterial = mat;
            }

            return decor;
        }

        #endregion

        #region Placement Utilities

        /// <summary>
        /// Distributes points in a 2D area using rejection sampling with minimum spacing.
        /// Points are rejected if they are too close to exclusion zones or previously placed points.
        /// </summary>
        /// <param name="count">Desired number of points.</param>
        /// <param name="minDist">Minimum distance between any two placed points.</param>
        /// <param name="bounds">Placement area (XZ rectangle).</param>
        /// <param name="exclusions">Positions to avoid (e.g., team spawns).</param>
        /// <param name="exclusionRadius">Minimum distance from exclusion zones.</param>
        /// <returns>List of valid positions (may be fewer than count if space is tight).</returns>
        private static List<Vector3> PlaceWithMinDistance(int count, float minDist,
            Rect bounds, List<Vector3> exclusions, float exclusionRadius)
        {
            var placed = new List<Vector3>();
            int maxAttempts = count * 30;

            for (int a = 0; a < maxAttempts && placed.Count < count; a++)
            {
                var candidate = new Vector3(
                    Random.Range(bounds.xMin, bounds.xMax),
                    0f,
                    Random.Range(bounds.yMin, bounds.yMax));

                // Check exclusion zones
                if (IsTooCloseToAny(candidate, exclusions, exclusionRadius))
                    continue;

                // Check previously placed
                if (IsTooCloseToAny(candidate, placed, minDist))
                    continue;

                placed.Add(candidate);
            }

            if (placed.Count < count)
            {
                Debug.LogWarning($"⚠️ Could only place {placed.Count}/{count} props " +
                                 $"(spacing constraints too tight for map size)");
            }

            return placed;
        }

        /// <summary>Checks if a position is within a given distance of any point in the list.</summary>
        private static bool IsTooCloseToAny(Vector3 candidate, List<Vector3> points, float minDist)
        {
            float minDistSq = minDist * minDist;
            foreach (var p in points)
            {
                float dx = candidate.x - p.x;
                float dz = candidate.z - p.z;
                if (dx * dx + dz * dz < minDistSq)
                    return true;
            }
            return false;
        }

        /// <summary>Recursively sets the physics layer on a GameObject and all its children.</summary>
        private static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
                SetLayerRecursive(child.gameObject, layer);
        }

        #endregion

        #region Atmosphere

        /// <summary>
        /// Applies the selected theme's atmosphere settings to the scene:
        /// skybox, ambient light, and fog configuration.
        /// </summary>
        private void ApplyAtmosphere()
        {
            if (selectedTheme.SkyboxMaterial != null)
                RenderSettings.skybox = selectedTheme.SkyboxMaterial;

            RenderSettings.ambientLight = selectedTheme.AmbientLightColor;
            RenderSettings.fog = selectedTheme.FogEnabled;
            RenderSettings.fogColor = selectedTheme.FogColor;
            RenderSettings.fogDensity = selectedTheme.FogDensity;

            Debug.Log($"🎨 Atmosphere applied: ambient={selectedTheme.AmbientLightColor}, " +
                      $"fog={selectedTheme.FogEnabled}");
        }

        #endregion

        #region Clear

        /// <summary>
        /// Destroys all root GameObjects in the scene whose name starts with "Map_".
        /// Uses Undo for Ctrl+Z support.
        /// </summary>
        private static void ClearMap()
        {
            var roots = UnityEngine.SceneManagement.SceneManager
                .GetActiveScene().GetRootGameObjects();

            int cleared = 0;
            foreach (var root in roots)
            {
                if (root.name.StartsWith("Map_"))
                {
                    Undo.DestroyObjectImmediate(root);
                    cleared++;
                }
            }

            if (cleared > 0)
                Debug.Log($"🗑️ Cleared {cleared} existing map(s)");
        }

        #endregion
    }
}
