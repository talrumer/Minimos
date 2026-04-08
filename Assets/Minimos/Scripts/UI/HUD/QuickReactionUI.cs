using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

namespace Minimos.UI
{
    /// <summary>
    /// Types of quick reactions available via D-pad.
    /// </summary>
    public enum ReactionType : byte
    {
        Happy = 0,
        Angry = 1,
        Sad = 2,
        Celebrate = 3
    }

    /// <summary>
    /// Networked quick-reaction emote system. Players press D-pad directions
    /// to spawn floating emoji icons above their character that are visible
    /// to all clients.
    /// </summary>
    public class QuickReactionUI : NetworkBehaviour
    {
        #region Fields

        [Header("Reaction Sprites")]
        [Tooltip("Sprites for each reaction type. Index matches ReactionType enum.")]
        [SerializeField] private Sprite[] reactionSprites = new Sprite[4];

        [Header("Display Settings")]
        [SerializeField] private float displayDuration = 2f;
        [SerializeField] private float floatSpeed = 0.5f;
        [SerializeField] private float fadeDuration = 0.5f;
        [SerializeField] private Vector3 spawnOffset = new(0f, 2.5f, 0f);

        [Header("Cooldown")]
        [SerializeField] private float cooldownDuration = 3f;

        [Header("Prefab")]
        [Tooltip("A world-space Canvas prefab with an Image child for the reaction icon.")]
        [SerializeField] private GameObject reactionIconPrefab;

        private float cooldownTimer;

        #endregion

        #region Unity Lifecycle

        private void Update()
        {
            if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;
            if (!IsOwner) return;

            HandleInput();
        }

        #endregion

        #region Input

        private void HandleInput()
        {
            if (cooldownTimer > 0f) return;

            // D-pad / keyboard input mapping.
            if (UnityEngine.Input.GetKeyDown(KeyCode.UpArrow))
                RequestReaction(ReactionType.Happy);
            else if (UnityEngine.Input.GetKeyDown(KeyCode.RightArrow))
                RequestReaction(ReactionType.Celebrate);
            else if (UnityEngine.Input.GetKeyDown(KeyCode.DownArrow))
                RequestReaction(ReactionType.Sad);
            else if (UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow))
                RequestReaction(ReactionType.Angry);
        }

        private void RequestReaction(ReactionType type)
        {
            cooldownTimer = cooldownDuration;
            ShowReactionServerRpc(type);
        }

        #endregion

        #region Network RPCs

        /// <summary>
        /// Client requests the server to broadcast a reaction to all players.
        /// </summary>
        [ServerRpc]
        private void ShowReactionServerRpc(ReactionType type)
        {
            ShowReactionClientRpc(type);
        }

        /// <summary>
        /// All clients spawn the floating reaction icon above this player.
        /// </summary>
        [ClientRpc]
        private void ShowReactionClientRpc(ReactionType type)
        {
            SpawnReactionIcon(type);
        }

        #endregion

        #region Visual

        private void SpawnReactionIcon(ReactionType type)
        {
            int index = (int)type;
            if (reactionSprites == null || index < 0 || index >= reactionSprites.Length) return;
            if (reactionSprites[index] == null) return;

            if (reactionIconPrefab != null)
            {
                // Use prefab-based spawning.
                Vector3 spawnPos = transform.position + spawnOffset;
                GameObject iconGo = Instantiate(reactionIconPrefab, spawnPos, Quaternion.identity);

                Image iconImage = iconGo.GetComponentInChildren<Image>();
                if (iconImage != null)
                    iconImage.sprite = reactionSprites[index];

                StartCoroutine(AnimateReactionIcon(iconGo.transform, iconGo.GetComponentInChildren<CanvasGroup>()));
            }
            else
            {
                // Fallback: create a simple world-space sprite.
                GameObject iconGo = new GameObject($"Reaction_{type}");
                iconGo.transform.position = transform.position + spawnOffset;

                var sr = iconGo.AddComponent<SpriteRenderer>();
                sr.sprite = reactionSprites[index];
                sr.sortingOrder = 100;

                StartCoroutine(AnimateReactionSprite(iconGo.transform, sr));
            }
        }

        private IEnumerator AnimateReactionIcon(Transform iconTransform, CanvasGroup canvasGroup)
        {
            float elapsed = 0f;
            Vector3 startPos = iconTransform.position;

            while (elapsed < displayDuration)
            {
                elapsed += Time.deltaTime;

                // Float upward.
                iconTransform.position = startPos + Vector3.up * (floatSpeed * elapsed);

                // Billboard toward camera.
                if (UnityEngine.Camera.main != null)
                    iconTransform.forward = UnityEngine.Camera.main.transform.forward;

                // Fade out during last portion.
                if (canvasGroup != null && elapsed > displayDuration - fadeDuration)
                {
                    float fadeT = (elapsed - (displayDuration - fadeDuration)) / fadeDuration;
                    canvasGroup.alpha = 1f - fadeT;
                }

                yield return null;
            }

            Destroy(iconTransform.gameObject);
        }

        private IEnumerator AnimateReactionSprite(Transform iconTransform, SpriteRenderer sr)
        {
            float elapsed = 0f;
            Vector3 startPos = iconTransform.position;
            Color startColor = sr.color;

            while (elapsed < displayDuration)
            {
                elapsed += Time.deltaTime;

                // Float upward.
                iconTransform.position = startPos + Vector3.up * (floatSpeed * elapsed);

                // Billboard toward camera.
                if (UnityEngine.Camera.main != null)
                    iconTransform.forward = UnityEngine.Camera.main.transform.forward;

                // Fade out during last portion.
                if (elapsed > displayDuration - fadeDuration)
                {
                    float fadeT = (elapsed - (displayDuration - fadeDuration)) / fadeDuration;
                    Color c = startColor;
                    c.a = 1f - fadeT;
                    sr.color = c;
                }

                yield return null;
            }

            Destroy(iconTransform.gameObject);
        }

        #endregion
    }
}
