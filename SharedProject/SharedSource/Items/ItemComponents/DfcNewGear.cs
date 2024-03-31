using System;
using Barotrauma;
using Barotrauma.Items.Components;
using MoonSharp.Interpreter;

namespace DSSIFactionCraft.Items.Components
{
    internal class DfcNewGear : ItemComponent, IDfcParticipatory, IDfcTaggable
    {
        [InGameEditable(MinValueInt = -1), Serialize(-1, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public int ParticipantTickets { get; set; }

        [InGameEditable(MinValueInt = -1), Serialize(-1, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public int ParticipantNumberLimit { get; set; }

        [InGameEditable(MinValueInt = 0), Serialize(0, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public int ParticipantWeight { get; set; }

        [InGameEditable(), Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string CharacterTags { get; set; }

        [InGameEditable, Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string Identifier { get; set; }

        [InGameEditable, Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string ActionChunk { get; set; }

        [InGameEditable(MinValueInt = int.MinValue, MaxValueInt = int.MaxValue), Serialize(0, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public int Sort { get; set; }

        [InGameEditable, Serialize(true, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public bool NotifyTeammates { get; set; }

        public static DynValue GetParameterTable(Item item)
        {
            var component = item.GetComponent<DfcNewGear>();
            if (component == null) { return DynValue.Nil; }
            var dynValue = DynValue.NewTable(GameMain.LuaCs.Lua);
            dynValue.Table["identifier"] = component.Identifier.IsNullOrEmpty() ? DynValue.Nil : component.Identifier;
            dynValue.Table["actionChunk"] = component.ActionChunk.IsNullOrEmpty() ? DynValue.Nil : component.ActionChunk;
            dynValue.Table["participantTickets"] = component.ParticipantTickets;
            dynValue.Table["participantNumberLimit"] = component.ParticipantNumberLimit;
            dynValue.Table["participantWeight"] = component.ParticipantWeight;
            dynValue.Table["characterTags"] = LuaUtils.SplitToTable(component.CharacterTags, ',', StringSplitOptions.RemoveEmptyEntries);
            dynValue.Table["sort"] = component.Sort;
            dynValue.Table["notifyTeammates"] = component.NotifyTeammates;
            return dynValue;
        }

        public DfcNewGear(Item item, ContentXElement element) : base(item, element) { }
    }
}