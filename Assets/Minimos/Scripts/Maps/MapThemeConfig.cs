using UnityEngine;

namespace Minimos.Maps
{
    /// <summary>
    /// ScriptableObject defining a map theme's visual configuration.
    /// Controls props, ground, atmosphere, and fallback primitive colors.
    /// Create via Assets > Create > Minimos > Map Theme Config.
    /// </summary>
    [CreateAssetMenu(fileName = "NewMapTheme", menuName = "Minimos/Map Theme Config")]
    public class MapThemeConfig : ScriptableObject
    {
        #region Identity

        /// <summary>Display name for this theme (e.g. "Tropical", "Tundra").</summary>
        [Header("Identity")]
        [SerializeField] private string themeName;

        /// <summary>Unique identifier used for serialization and lookups.</summary>
        [SerializeField] private string themeId;

        #endregion

        #region Props

        /// <summary>Tree prefabs to scatter on the map.</summary>
        [Header("Props")]
        [SerializeField] private GameObject[] treePrefabs;

        /// <summary>Rock prefabs to scatter on the map.</summary>
        [SerializeField] private GameObject[] rockPrefabs;

        /// <summary>Decorative prefabs (flowers, mushrooms, crates, etc.).</summary>
        [SerializeField] private GameObject[] decorationPrefabs;

        #endregion

        #region Ground

        /// <summary>Optional material applied to the ground plane.</summary>
        [Header("Ground")]
        [SerializeField] private Material groundMaterial;

        /// <summary>Fallback ground tint when no material is assigned.</summary>
        [SerializeField] private Color groundColor = new Color(0.3f, 0.7f, 0.2f, 1f);

        #endregion

        #region Atmosphere

        /// <summary>Skybox material for this theme.</summary>
        [Header("Atmosphere")]
        [SerializeField] private Material skyboxMaterial;

        /// <summary>Ambient light color applied to the scene.</summary>
        [SerializeField] private Color ambientLightColor = Color.white;

        /// <summary>Whether distance fog is enabled.</summary>
        [SerializeField] private bool fogEnabled;

        /// <summary>Fog color when fog is enabled.</summary>
        [SerializeField] private Color fogColor = Color.gray;

        /// <summary>Exponential fog density.</summary>
        [Range(0f, 0.1f)]
        [SerializeField] private float fogDensity = 0.01f;

        #endregion

        #region Scale

        /// <summary>Random scale range (x = min, y = max) applied to spawned props.</summary>
        [Header("Scale")]
        [SerializeField] private Vector2 propScaleRange = new Vector2(0.8f, 1.2f);

        #endregion

        #region Basic Primitives

        /// <summary>Use basic Unity primitives instead of authored prefabs.</summary>
        [Header("Basic Primitives")]
        [Tooltip("Enable to use simple colored primitives when no asset packs are available.")]
        [SerializeField] private bool useBasicPrimitives;

        /// <summary>Color for primitive tree shapes.</summary>
        [SerializeField] private Color basicTreeColor = new Color(0.2f, 0.6f, 0.15f, 1f);

        /// <summary>Color for primitive rock shapes.</summary>
        [SerializeField] private Color basicRockColor = new Color(0.5f, 0.5f, 0.5f, 1f);

        /// <summary>Color for primitive decoration shapes.</summary>
        [SerializeField] private Color basicDecorationColor = new Color(0.9f, 0.7f, 0.2f, 1f);

        #endregion

        #region Public Accessors

        // --- Identity ---
        public string ThemeName => themeName;
        public string ThemeId => themeId;

        // --- Props ---
        public GameObject[] TreePrefabs => treePrefabs;
        public GameObject[] RockPrefabs => rockPrefabs;
        public GameObject[] DecorationPrefabs => decorationPrefabs;

        // --- Ground ---
        public Material GroundMaterial => groundMaterial;
        public Color GroundColor => groundColor;

        // --- Atmosphere ---
        public Material SkyboxMaterial => skyboxMaterial;
        public Color AmbientLightColor => ambientLightColor;
        public bool FogEnabled => fogEnabled;
        public Color FogColor => fogColor;
        public float FogDensity => fogDensity;

        // --- Scale ---
        public Vector2 PropScaleRange => propScaleRange;

        // --- Basic Primitives ---
        public bool UseBasicPrimitives => useBasicPrimitives;
        public Color BasicTreeColor => basicTreeColor;
        public Color BasicRockColor => basicRockColor;
        public Color BasicDecorationColor => basicDecorationColor;

        #endregion
    }
}
