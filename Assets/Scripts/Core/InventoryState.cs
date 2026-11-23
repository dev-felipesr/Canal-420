using System;
using System.Collections.Generic;

namespace Canal420.Core
{
    public static class InventoryState
    {
        private static readonly HashSet<string> heldItems = new HashSet<string>();

        public static event Action<string> OnItemAdded;

        private static string Normalize(string id)
        {
            return string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim().ToLowerInvariant();
        }

        public static void ClearAll()
        {
            heldItems.Clear();
        }

        public static void AddItem(string itemId)
        {
            string norm = Normalize(itemId);
            if (string.IsNullOrEmpty(norm))
                return;

            if (heldItems.Add(norm))
            {
                OnItemAdded?.Invoke(norm);
            }
        }

        public static bool HasItem(string itemId)
        {
            string norm = Normalize(itemId);
            return !string.IsNullOrEmpty(norm) && heldItems.Contains(norm);
        }
    }
}
