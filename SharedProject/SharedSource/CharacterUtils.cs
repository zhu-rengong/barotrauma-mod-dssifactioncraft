using System;
using System.Collections.Generic;
using System.Linq;
using Barotrauma;
using Barotrauma.Extensions;
using Barotrauma.Items.Components;
using MoonSharp.Interpreter;

namespace DSSIFactionCraft
{
    internal class CharacterUtils
    {
        [Flags]
        public enum TargetType
        {
            None = 0x001,
            Human = 0x002,
            Monster = 0x004,
            Pet = 0x008,
            Team1 = 0x010,
            Team2 = 0x020,
            FriendlyNPC = 0x40,
            Alive = 0x080,
            Dead = 0x100,
            OnlyIndoor = 0x200,
            Any = Human | Monster | Pet | Team1 | Team2 | FriendlyNPC | Alive | Dead | OnlyIndoor,
        }

        public static Dictionary<Character, Guid> GUID;
        public static Dictionary<Character, HashSet<string>> Tags;

        static CharacterUtils()
        {
            GUID = new();
            Tags = new();
            GameMain.LuaCs.Lua.Globals["DFC", "AddCharacterTags"] = (Action<Character, string[]>)AddTags;
            GameMain.LuaCs.Lua.Globals["DFC", "GetCharacterTags"] = (Func<Character, string[]>)GetTags;
        }

        public static Guid GetGuid(Character character)
        {
            Guid guid;
            foreach (var pair in GUID.ToArray()) { if (pair.Key.Removed) { GUID.Remove(pair.Key); } }
            if (GUID.TryGetValue(character, out Guid value))
            {
                guid = value;
            }
            else
            {
                guid = Guid.NewGuid();
                GUID.Add(character, guid);
            }
            return guid;
        }

        public static void RemoveRemovedTags()
        {
            foreach (var pair in Tags.ToArray()) { if (pair.Key.Removed) { Tags.Remove(pair.Key); } }
        }

        public static void AddTags(Character character, string[] tagsToAdd)
        {
            RemoveRemovedTags();
            if (!Tags.TryGetValue(character, out HashSet<string> tags))
            {
                tags = new();
                Tags.Add(character, tags);
            }
            tagsToAdd.ForEach(tag => tags.Add(tag));
        }

        public static string[] GetTags(Character character)
        {
            RemoveRemovedTags();
            if (Tags.TryGetValue(character, out HashSet<string> tags)) { return tags.ToArray(); }
            return Array.Empty<string>();
        }

        public static bool Matches(Character character, TargetType targetType, string speciesNames, string group, string tags)
        {
            if (!targetType.HasFlag(TargetType.Human) && character.IsHuman) { return false; }
            if (!targetType.HasFlag(TargetType.Pet) && character.IsPet) { return false; }
            if (!targetType.HasFlag(TargetType.Monster) && !character.IsHuman && !character.IsPet) { return false; }
            if (!targetType.HasFlag(TargetType.Team1) && character.TeamID == CharacterTeamType.Team1) { return false; }
            if (!targetType.HasFlag(TargetType.Team2) && character.TeamID == CharacterTeamType.Team2) { return false; }
            if (!targetType.HasFlag(TargetType.FriendlyNPC) && character.TeamID == CharacterTeamType.FriendlyNPC) { return false; }
            if (!targetType.HasFlag(TargetType.Alive) && !character.IsDead) { return false; }
            if (!targetType.HasFlag(TargetType.Dead) && character.IsDead) { return false; }
            if (targetType.HasFlag(TargetType.OnlyIndoor) && character.CurrentHull == null) { return false; }
            if (!speciesNames.IsNullOrEmpty())
            {
                var allSpecies = speciesNames.Split(',').Select(s => s.Trim().ToIdentifier()).ToArray();
                if (!allSpecies.Any(s => s == character.SpeciesName)) { return false; }
            }
            if (!group.IsNullOrEmpty() && character.Group != group.ToIdentifier()) { return false; }
            if (!tags.IsNullOrEmpty())
            {
                var tagArray = tags.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (!GetTags(character).Any(tag => tagArray.Contains(tag)))
                {
                    return false;
                }
            }
            return true;
        }
    }
}