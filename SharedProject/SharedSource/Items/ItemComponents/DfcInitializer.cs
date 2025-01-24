using System.Diagnostics;
using Barotrauma;
using Barotrauma.Items.Components;
using Microsoft.Xna.Framework;

namespace DSSIFactionCraft.Items.Components
{
    internal class DfcInitializer : ItemComponent
    {
        [InGameEditable, Serialize(true, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public bool AllowMidRoundJoin { get; set; }

        [InGameEditable, Serialize(true, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public bool AllowRespawn { get; set; }
        
        [InGameEditable, Serialize(true, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public bool AutoParticipateWhenNoChoices { get; set; }

        public DfcInitializer(Item item, ContentXElement element) : base(item, element) { }
    }
}