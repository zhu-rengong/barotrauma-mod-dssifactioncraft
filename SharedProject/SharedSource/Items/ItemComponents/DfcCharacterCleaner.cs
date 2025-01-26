using System;
using System.Collections.Generic;
using System.Linq;
using Barotrauma;
using Barotrauma.Items.Components;
using TargetType = DSSIFactionCraft.CharacterUtils.TargetType;

namespace DSSIFactionCraft.Items.Components
{
    internal class DfcCharacterCleaner : ItemComponent
    {
        protected static bool IsMultiplayerClient => GameMain.NetworkMember?.IsClient ?? false;

        [InGameEditable(MinValueInt = 1, MaxValueInt = 100), Serialize(1, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public int ToleranceThreshold { get; set; }

        [InGameEditable, Serialize(TargetType.Any, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public TargetType CharacterTargetType { get; set; }

        [InGameEditable, Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string CharacterSpeciesNames { get; set; }

        [InGameEditable, Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string CharacterGroup { get; set; }

        [InGameEditable, Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string CharacterTags { get; set; }
        
        [InGameEditable, Serialize(true, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public bool DespawnRatherThanRemoveDirectly { get; set; }

        private Dictionary<Character, int> Tolerance;

        private IList<Item> includes;
        private IList<Item> excludes;

        public DfcCharacterCleaner(Item item, ContentXElement element) : base(item, element)
        {
            IsActive = false;
            if (IsMultiplayerClient) { return; }
            Tolerance = new();
        }

        public override void Update(float deltaTime, Camera cam)
        {
            includes ??= RegionUtils.GetFromLinkedTo(item, "dfc_regionincluded");
            excludes ??= RegionUtils.GetFromLinkedTo(item, "dfc_regionexcluded");
            List<Character> charactersToRemove = new();
            foreach (var character in Character.CharacterList)
            {
                if (CharacterUtils.Matches(character, CharacterTargetType, CharacterSpeciesNames, CharacterGroup, CharacterTags)
                    && RegionUtils.Contains(character.WorldPosition, includes, excludes))
                {
                    charactersToRemove.Add(character);
                }
            }
            foreach (var pair in Tolerance.ToArray())
            {
                if (pair.Key.Removed)
                {
                    Tolerance.Remove(pair.Key);
                }
            }
            foreach (var character in charactersToRemove.ToArray())
            {
                if (!Tolerance.TryGetValue(character, out int tolerance)) { tolerance = 0; }
                if (++tolerance >= ToleranceThreshold)
                {
                    Tolerance.Remove(character);
                    if (DespawnRatherThanRemoveDirectly)
                    {
                        character.Despawn();
                    }
                    else
                    {
                        Entity.Spawner.AddEntityToRemoveQueue(character);
                    }
                    continue;
                }
                else
                {
                    Tolerance[character] = tolerance;
                }
            }
            IsActive = false;
        }

        public override void ReceiveSignal(Signal signal, Connection connection)
        {
            if (IsMultiplayerClient) { return; }

            switch (connection.Name)
            {
                case "signal_in":
                    IsActive = true;
                    break;
                default:
                    break;
            }
        }
    }
}
