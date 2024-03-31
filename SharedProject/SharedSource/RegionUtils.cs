using System.Collections.Generic;
using Microsoft.Xna.Framework;
using System.Linq;
using Barotrauma;

namespace DSSIFactionCraft
{
    internal static class RegionUtils
    {
        public static IList<Item> GetFromLinkedTo(Item item, Identifier identifier)
            => item.linkedTo.OfType<Item>()
            .Where(linked => linked.Prefab.Identifier == identifier).ToList();

        public static bool Contains(Vector2 worldPos, IList<Item> includes, IList<Item> excludes)
        {
            double x = worldPos.X, y = worldPos.Y;
            bool Contains(Rectangle rect) => x > rect.X && x < rect.X + rect.Width && y < rect.Y && y > rect.Y - rect.Height;
            if (excludes != null && excludes.Count > 0 && excludes.Any(item => Contains(item.WorldRect))) { return false; }
            if (includes == null || includes.Count == 0 || includes.Any(item => Contains(item.WorldRect))) { return true; }
            return false;
        }
    }
}