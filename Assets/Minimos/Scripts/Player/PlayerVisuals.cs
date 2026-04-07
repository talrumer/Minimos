using System.Collections;
using UnityEngine;

namespace Minimos.Player
{
    /// <summary>
    /// Handles all player visual effects: team colors, cosmetics, squash-and-stretch,
    /// hit flash, invulnerability blinking, and world-space nameplate.
    /// Uses MaterialPropertyBlock for GPU instancing compatibility.
    /// </summary>
    public class PlayerVisuals : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Body Renderer")]
        [SerializeField] private Renderer bodyRenderer;
        [SerializeField] private int bodyMaterialIndex;

        [Header("Attachment Points")]
        [SerializeField] private Transform headSlot;
        [SerializeField] private Transform faceSlot;
        [SerializeField] private Transform backSlot;
        [SerializeField] private Transform feetSlot;

        [Header("Squash & Stretch")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private float squashScaleY = 0.75f;
        [SerializeField] private float squashScaleXZ = 1.2f;
        [SerializeField] private float squashStretchDuration = 0.2f;

        [Header("Hit Flash")]
        [SerializeField] private Color hitFlashColor = Color.white;
        [SerializeField] private float hitFlashDuration = 0.1f;

        [Header("Invulnerability")]
        [SerializeField] private float blinkInterval = 0.1f;

        [Header("Nameplate")]
        [SerializeField] private TMPro.TextMeshPro nameplateText;
        [SerializeField] private SpriteRenderer nameplateBackground;
        [SerializeField] private Transform nameplateAnchor;

        [Header("VFX")]
        [SerializeField] private ParticleSystem doubleJumpVFX;
        [SerializeField] private ParticleSystem landingVFX;

        #endregion

        #region Private State

        private MaterialPropertyBlock propertyBlock;
        private Color currentTeamColor = Color.white;
        private Coroutine hitFlashCoroutine;
        private Coroutine invulnerableCoroutine;
        private Coroutine squashStretchCoroutine;

        private static readonly int ColorProperty = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionProperty = Shader.PropertyToID("_EmissionColor");
        private static readonly int PatternProperty = Shader.PropertyToID("_PatternTex");

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        private void LateUpdate()
        {
            // Billboard the nameplate toward camera
            if (nameplateAnchor != null && UnityEngine.Camera.main != null)
            {
                nameplateAnchor.rotation = UnityEngine.Camera.main.transform.rotation;
            }
        }

        #endregion

        #region Team Color

        /// <summary>
        /// Set the player's team color on the main body material.
        /// Uses MaterialPropertyBlock for GPU instancing compatibility.
        /// </summary>
        /// <param name="color">Team color to apply.</param>
        public void SetTeamColor(Color color)
        {
            currentTeamColor = color;

            if (bodyRenderer == null) return;

            bodyRenderer.GetPropertyBlock(propertyBlock, bodyMaterialIndex);
            propertyBlock.SetColor(ColorProperty, color);
            bodyRenderer.SetPropertyBlock(propertyBlock, bodyMaterialIndex);
        }

        #endregion

        #region Pattern

        /// <summary>
        /// Apply an overlay pattern texture on top of the team color.
        /// </summary>
        /// <param name="pattern">Pattern texture to overlay.</param>
        public void SetPattern(Texture2D pattern)
        {
            if (bodyRenderer == null || pattern == null) return;

            bodyRenderer.GetPropertyBlock(propertyBlock, bodyMaterialIndex);
            propertyBlock.SetTexture(PatternProperty, pattern);
            bodyRenderer.SetPropertyBlock(propertyBlock, bodyMaterialIndex);
        }

        #endregion

        #region Cosmetics

        /// <summary>
        /// Instantiate a cosmetic prefab on the specified attachment slot.
        /// Destroys any existing cosmetic on that slot first.
        /// </summary>
        /// <param name="slot">Slot name: "head", "face", "back", or "feet".</param>
        /// <param name="cosmeticPrefab">Prefab to instantiate.</param>
        public void ApplyCosmetic(string slot, GameObject cosmeticPrefab)
        {
            Transform attachPoint = GetAttachmentPoint(slot);
            if (attachPoint == null)
            {
                Debug.LogWarning($"[PlayerVisuals] Unknown cosmetic slot: {slot}");
                return;
            }

            // Clear existing cosmetic on this slot
            for (int i = attachPoint.childCount - 1; i >= 0; i--)
            {
                Destroy(attachPoint.GetChild(i).gameObject);
            }

            if (cosmeticPrefab != null)
            {
                Instantiate(cosmeticPrefab, attachPoint);
            }
        }

        /// <summary>Remove all cosmetics from every slot.</summary>
        public void ClearAllCosmetics()
        {
            ClearSlot(headSlot);
            ClearSlot(faceSlot);
            ClearSlot(backSlot);
            ClearSlot(feetSlot);
        }

        private Transform GetAttachmentPoint(string slot)
        {
            return slot.ToLowerInvariant() switch
            {
                "head" => headSlot,
                "face" => faceSlot,
                "back" => backSlot,
                "feet" => feetSlot,
                _ => null
            };
        }

        private void ClearSlot(Transform slot)
        {
            if (slot == null) return;
            for (int i = slot.childCount - 1; i >= 0; i--)
            {
                Destroy(slot.GetChild(i).gameObject);
            }
        }

        #endregion

        #region Squash & Stretch

        /// <summary>
        /// Play the landing squash-and-stretch animation on the visual root.
        /// Squashes Y, expands XZ, then returns to normal.
        /// </summary>
        public void PlaySquashStretch()
        {
            if (visualRoot == null) return;
            if (squashStretchCoroutine != null) StopCoroutine(squashStretchCoroutine);
            squashStretchCoroutine = StartCoroutine(SquashStretchCoroutine());
        }

        private IEnumerator SquashStretchCoroutine()
        {
            Vector3 squashScale = new(squashScaleXZ, squashScaleY, squashScaleXZ);
            float halfDuration = squashStretchDuration * 0.5f;

            // Squash
            float t = 0f;
            while (t < halfDuration)
            {
                t += Time.deltaTime;
                visualRoot.localScale = Vector3.Lerp(Vector3.one, squashScale, t / halfDuration);
                yield return null;
            }

            // Return to normal
            t = 0f;
            while (t < halfDuration)
            {
                t += Time.deltaTime;
                visualRoot.localScale = Vector3.Lerp(squashScale, Vector3.one, t / halfDuration);
                yield return null;
            }

            visualRoot.localScale = Vector3.one;
            squashStretchCoroutine = null;
        }

        #endregion

        #region Hit Flash

        /// <summary>
        /// Flash the body white briefly when hit.
        /// </summary>
        public void PlayHitFlash()
        {
            if (hitFlashCoroutine != null) StopCoroutine(hitFlashCoroutine);
            hitFlashCoroutine = StartCoroutine(HitFlashCoroutine());
        }

        private IEnumerator HitFlashCoroutine()
        {
            if (bodyRenderer == null) yield break;

            bodyRenderer.GetPropertyBlock(propertyBlock, bodyMaterialIndex);
            propertyBlock.SetColor(EmissionProperty, hitFlashColor * 2f);
            bodyRenderer.SetPropertyBlock(propertyBlock, bodyMaterialIndex);

            yield return new WaitForSeconds(hitFlashDuration);

            propertyBlock.SetColor(EmissionProperty, Color.black);
            bodyRenderer.SetPropertyBlock(propertyBlock, bodyMaterialIndex);
            hitFlashCoroutine = null;
        }

        #endregion

        #region Invulnerability Visual

        /// <summary>
        /// Toggle the invulnerability blinking effect (used during dodge roll).
        /// </summary>
        /// <param name="active">True to start blinking, false to stop.</param>
        public void SetInvulnerableVisual(bool active)
        {
            if (active)
            {
                if (invulnerableCoroutine == null)
                    invulnerableCoroutine = StartCoroutine(InvulnerableBlinkCoroutine());
            }
            else
            {
                if (invulnerableCoroutine != null)
                {
                    StopCoroutine(invulnerableCoroutine);
                    invulnerableCoroutine = null;
                }

                if (bodyRenderer != null) bodyRenderer.enabled = true;
            }
        }

        private IEnumerator InvulnerableBlinkCoroutine()
        {
            while (true)
            {
                if (bodyRenderer != null)
                    bodyRenderer.enabled = !bodyRenderer.enabled;
                yield return new WaitForSeconds(blinkInterval);
            }
        }

        #endregion

        #region Nameplate

        /// <summary>
        /// Set the player's world-space nameplate text and background color.
        /// </summary>
        /// <param name="playerName">Display name.</param>
        /// <param name="teamColor">Background tint color.</param>
        public void SetNameplate(string playerName, Color teamColor)
        {
            if (nameplateText != null)
            {
                nameplateText.text = playerName;
                nameplateText.color = Color.white;
            }

            if (nameplateBackground != null)
            {
                teamColor.a = 0.8f;
                nameplateBackground.color = teamColor;
            }
        }

        #endregion

        #region VFX

        /// <summary>Play the double-jump burst VFX.</summary>
        public void PlayDoubleJumpVFX()
        {
            if (doubleJumpVFX != null) doubleJumpVFX.Play();
        }

        /// <summary>Play the landing dust VFX.</summary>
        public void PlayLandingVFX()
        {
            if (landingVFX != null) landingVFX.Play();
        }

        #endregion
    }
}
