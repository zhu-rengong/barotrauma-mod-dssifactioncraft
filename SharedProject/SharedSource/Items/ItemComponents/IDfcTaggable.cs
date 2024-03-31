using Barotrauma;

namespace DSSIFactionCraft.Items.Components
{
    internal interface IDfcTaggable
    {
        [InGameEditable(), Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string CharacterTags { get; set; }
    }
}