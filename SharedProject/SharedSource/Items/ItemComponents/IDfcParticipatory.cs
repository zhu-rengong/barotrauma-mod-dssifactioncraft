using Barotrauma;

namespace DSSIFactionCraft.Items.Components
{
    internal interface IDfcParticipatory
    {
        [InGameEditable(MinValueInt = -1), Serialize(-1, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public int ParticipantTickets { get; set; }

        [InGameEditable(MinValueInt = -1), Serialize(-1, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public int ParticipantNumberLimit { get; set; }

        [InGameEditable(MinValueInt = 0), Serialize(0, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public int ParticipantWeight { get; set; }
    }
}