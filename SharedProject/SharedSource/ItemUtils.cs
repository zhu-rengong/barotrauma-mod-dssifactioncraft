using System;
using System.Collections.Generic;
using System.Linq;
using Barotrauma;
using Barotrauma.Extensions;
using Barotrauma.Items.Components;
using MoonSharp.Interpreter;
using static DSSIFactionCraft.Items.Components.DfcItemCleaner;

namespace DSSIFactionCraft
{
    internal class ItemUtils
    {
        public static string[][] ParseMatcherPattern(string pattern)
            => !pattern.IsNullOrEmpty() ? pattern.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(group => group.Split('|', StringSplitOptions.RemoveEmptyEntries))
                .ToArray() : Array.Empty<string[]>();

        public static bool Matches(Item item, string[][] includes, string[][] excludes)
        {
            bool MatchesByItem(string tag) => item.HasTag(tag) || item.Prefab.Identifier == tag;
            if (excludes != null && excludes.Length > 0 && excludes.Any(group => group.All(MatchesByItem))) { return false; }
            if (includes == null || includes.Length == 0 || includes.Any(group => group.All(MatchesByItem))) { return true; }
            return false;
        }
    }
}