using System;
using Barotrauma;
using Barotrauma.Items.Components;
using MoonSharp.Interpreter;

namespace DSSIFactionCraft.Items.Components
{
    internal class DfcNewJob : ItemComponent, IDfcParticipatory, IDfcTaggable
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
        public string JobOrCharacterName { get; set; }

        [InGameEditable, Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string OnAssignedChunk { get; set; }

        [InGameEditable(MinValueInt = 0), Serialize(1, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public int LiveConsumption { get; set; }

        [InGameEditable, Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string Gears { get; set; }

        [InGameEditable, Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string SpawnPointSets { get; set; }

        [InGameEditable(MinValueInt = int.MinValue, MaxValueInt = int.MaxValue), Serialize(0, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public int Sort { get; set; }

        [InGameEditable, Serialize(true, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public bool NotifyTeammates { get; set; }

        [InGameEditable, Serialize(false, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public bool InhertCharacterInfo { get; set; }

        public static DynValue GetParameterTable(Item item)
        {
            var component = item.GetComponent<DfcNewJob>();
            if (component == null) { return DynValue.Nil; }
            var dynValue = DynValue.NewTable(GameMain.LuaCs.Lua);
            dynValue.Table["identifier"] = component.Identifier.IsNullOrEmpty() ? DynValue.Nil : component.Identifier;
            dynValue.Table["name"] = component.JobOrCharacterName.IsNullOrEmpty() ? DynValue.Nil : component.JobOrCharacterName;
            dynValue.Table["onAssignedChunk"] = component.OnAssignedChunk.IsNullOrEmpty() ? DynValue.Nil : component.OnAssignedChunk;
            dynValue.Table["liveConsumption"] = component.LiveConsumption;
            dynValue.Table["gears"] = LuaUtils.SplitToTable(component.Gears, ',', StringSplitOptions.RemoveEmptyEntries);
            dynValue.Table["spawnPointSets"] = LuaUtils.SplitToTable(component.SpawnPointSets, ',', StringSplitOptions.RemoveEmptyEntries);
            dynValue.Table["participantTickets"] = component.ParticipantTickets;
            dynValue.Table["participantNumberLimit"] = component.ParticipantNumberLimit;
            dynValue.Table["participantWeight"] = component.ParticipantWeight;
            dynValue.Table["characterTags"] = LuaUtils.SplitToTable(component.CharacterTags, ',', StringSplitOptions.RemoveEmptyEntries);
            dynValue.Table["sort"] = component.Sort;
            dynValue.Table["notifyTeammates"] = component.NotifyTeammates;
            dynValue.Table["inhertCharacterInfo"] = component.InhertCharacterInfo;
            return dynValue;
        }

        public DfcNewJob(Item item, ContentXElement element) : base(item, element) { }
    }
}