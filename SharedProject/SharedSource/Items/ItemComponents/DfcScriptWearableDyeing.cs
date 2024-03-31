using System;
using System.Collections.Generic;
using System.Linq;
using Barotrauma;
using Barotrauma.Items.Components;
using Microsoft.Xna.Framework;
using TargetType = DSSIFactionCraft.CharacterUtils.TargetType;

namespace DSSIFactionCraft.Items.Components
{
    internal partial class DfcScriptWearableDyeing : ItemComponent
    {
        protected static bool IsMultiplayerClient => GameMain.NetworkMember?.IsClient ?? false;

        [InGameEditable, Serialize("1.0,1.0,1.0,1.0", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public Color SpriteColor { get; set; }

        [InGameEditable, Serialize("1.0,1.0,1.0,1.0", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public Color InventoryIconColor { get; set; }

        private string matcherPatternIncludes;
        [InGameEditable, Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string MatcherPatternIncludes
        {
            get => matcherPatternIncludes;
            set
            {
                matcherPatternIncludes = value;
                if (!IsMultiplayerClient) { UpdateMatcherPattern(); }
            }
        }

        private string matcherPatternExcludes;
        [InGameEditable, Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string MatcherPatternExcludes
        {
            get => matcherPatternExcludes;
            set
            {
                matcherPatternExcludes = value;
                if (!IsMultiplayerClient) { UpdateMatcherPattern(); }
            }
        }

        [InGameEditable, Serialize(InvSlotType.InnerClothes | InvSlotType.Head | InvSlotType.OuterClothes, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public InvSlotType InvSlotTypes { get; set; }

        public string[][] includes = Array.Empty<string[]>();
        public string[][] excludes = Array.Empty<string[]>();

        public void UpdateMatcherPattern()
        {
            includes = ItemUtils.ParseMatcherPattern(matcherPatternIncludes);
            excludes = ItemUtils.ParseMatcherPattern(matcherPatternExcludes);
        }

        [InGameEditable, Serialize(TargetType.Any, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public TargetType WearerTargetType { get; set; }

        [InGameEditable, Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string WearerSpeciesNames { get; set; }

        [InGameEditable, Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string WearerGroup { get; set; }

        [InGameEditable, Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string WearerTags { get; set; }

        private static List<DfcScriptWearableDyeing> wearableDyeingScripts;

        static DfcScriptWearableDyeing()
        {
            if (IsMultiplayerClient) { return; }

            wearableDyeingScripts = new();
            GameMain.LuaCs.Hook.Add("item.equip", nameof(DfcScriptWearableDyeing), dyeing);
            object dyeing(params object[] args)
            {
                wearableDyeingScripts.RemoveAll(script => script.item.Removed);
                if (!wearableDyeingScripts.Any()) { return default; }

                if (args[0] is Item equipped && args[1] is Character wearer)
                {
                    wearableDyeingScripts.ForEach(script =>
                    {
                        if (CharacterUtils.Matches(wearer, script.WearerTargetType, script.WearerSpeciesNames, script.WearerGroup, script.WearerTags)
                            && ItemUtils.Matches(equipped, script.includes, script.excludes)
                            && equipped.AllowedSlots.Any(slotType => script.InvSlotTypes.HasFlag(slotType)))
                        {
                            var sp_spritecolor = equipped.SerializableProperties["spritecolor"];
                            var sp_inventoryiconcolor = equipped.SerializableProperties["inventoryiconcolor"];
                            sp_spritecolor.SetValue(equipped, script.SpriteColor);
                            sp_inventoryiconcolor.SetValue(equipped, script.InventoryIconColor);
#if SERVER
                            script.IsActive = true;
                            script.queueMessages.Enqueue(new(equipped, sp_spritecolor, sp_inventoryiconcolor));
#endif
                        }
                    });
                }
                return default;
            }
        }

        public DfcScriptWearableDyeing(Item item, ContentXElement element) : base(item, element)
        {
            IsActive = false;
            if (IsMultiplayerClient) { return; }

            wearableDyeingScripts.Add(this);
            UpdateMatcherPattern();
        }
    }
}
