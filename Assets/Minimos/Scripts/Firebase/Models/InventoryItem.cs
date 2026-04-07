using System;

namespace Minimos.Firebase.Models
{
    /// <summary>
    /// Categories of cosmetic items a player can own.
    /// </summary>
    public enum ItemType
    {
        Hat,
        Face,
        Back,
        Shoes,
        Pattern,
        Emote,
        VictoryEffect,
        SoundEffect
    }

    /// <summary>
    /// A single cosmetic item in a player's inventory.
    /// </summary>
    [Serializable]
    public class InventoryItem
    {
        /// <summary>Unique item identifier (e.g., "hat_crown_gold").</summary>
        public string ItemId;

        /// <summary>The cosmetic slot this item belongs to.</summary>
        public ItemType Type;

        /// <summary>Human-readable item name for UI display.</summary>
        public string ItemName;

        /// <summary>Whether this item is currently equipped on the player character.</summary>
        public bool IsEquipped;

        /// <summary>ISO 8601 timestamp of when the item was acquired.</summary>
        public string AcquiredAt;

        public InventoryItem() { }

        public InventoryItem(string itemId, ItemType type, string itemName)
        {
            ItemId = itemId;
            Type = type;
            ItemName = itemName;
            IsEquipped = false;
            AcquiredAt = DateTime.UtcNow.ToString("o");
        }
    }
}
