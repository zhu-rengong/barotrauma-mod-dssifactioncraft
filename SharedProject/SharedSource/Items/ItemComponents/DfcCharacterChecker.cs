using System;
using System.Collections.Generic;
using System.Linq;
using Barotrauma;
using Barotrauma.Items.Components;
using TargetType = DSSIFactionCraft.CharacterUtils.TargetType;

namespace DSSIFactionCraft.Items.Components
{
    internal class DfcCharacterChecker : ItemComponent
    {
        [InGameEditable, Serialize(TargetType.Any, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public TargetType CharacterTargetType { get; set; }

        [InGameEditable, Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string CharacterSpeciesNames { get; set; }

        [InGameEditable, Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string CharacterGroup { get; set; }

        [InGameEditable, Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string CharacterTags { get; set; }

        [InGameEditable(DecimalCount = 3), Serialize(0.0f, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public float MinimumVelocity { get; set; }

        private IList<Item> includes;
        private IList<Item> excludes;

        public DfcCharacterChecker(Item item, ContentXElement element) : base(item, element) { IsActive = false; }

        public override void Update(float deltaTime, Camera cam)
        {
            includes ??= RegionUtils.GetFromLinkedTo(item, "dfc_regionincluded");
            excludes ??= RegionUtils.GetFromLinkedTo(item, "dfc_regionexcluded");
            int matches = Character.CharacterList.Count(character
                => character.CurrentSpeed >= MinimumVelocity
                    && CharacterUtils.Matches(character, CharacterTargetType, CharacterSpeciesNames, CharacterGroup, CharacterTags)
                    && RegionUtils.Contains(character.WorldPosition, includes, excludes));
            item.SendSignal(matches.ToString(), "signal_out");
            IsActive = false;
        }

        public override void ReceiveSignal(Signal signal, Connection connection)
        {
            if (GameMain.NetworkMember?.IsClient ?? false) { return; }

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