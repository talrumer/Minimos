using UnityEngine;

namespace Minimos.PowerUps
{
    /// <summary>
    /// Rarity tiers affecting spawn weights.
    /// </summary>
    public enum PowerUpRarity
    {
        Common,
        Uncommon,
        Rare
    }

    /// <summary>
    /// ScriptableObject defining a power-up's properties, visuals, and audio.
    /// Create via Assets > Create > Minimos > Power Up Config.
    /// </summary>
    [CreateAssetMenu(fileName = "NewPowerUpConfig", menuName = "Minimos/Power Up Config")]
    public class PowerUpConfig : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string powerUpName;
        [SerializeField] private Sprite icon;
        [SerializeField] private PowerUpRarity rarity = PowerUpRarity.Common;

        [Header("Behavior")]
        [Tooltip("Duration in seconds. 0 = instant / one-use effect.")]
        [SerializeField] private float duration;
        [SerializeField][TextArea(2, 3)] private string description;

        [Header("Presentation")]
        [SerializeField] private GameObject vfxPrefab;
        [SerializeField] private AudioClip pickupSfx;
        [SerializeField] private AudioClip activateSfx;

        [Header("Prefab")]
        [Tooltip("The PowerUpBase prefab to spawn when this power-up is selected.")]
        [SerializeField] private GameObject cratePrefab;

        // --- Public accessors ---
        public string PowerUpName => powerUpName;
        public Sprite Icon => icon;
        public PowerUpRarity Rarity => rarity;
        public float Duration => duration;
        public string Description => description;
        public GameObject VfxPrefab => vfxPrefab;
        public AudioClip PickupSfx => pickupSfx;
        public AudioClip ActivateSfx => activateSfx;
        public GameObject CratePrefab => cratePrefab;

        /// <summary>
        /// Returns the spawn weight based on rarity (Common=60, Uncommon=30, Rare=10).
        /// </summary>
        public int GetSpawnWeight()
        {
            return rarity switch
            {
                PowerUpRarity.Common => 60,
                PowerUpRarity.Uncommon => 30,
                PowerUpRarity.Rare => 10,
                _ => 60
            };
        }
    }
}
