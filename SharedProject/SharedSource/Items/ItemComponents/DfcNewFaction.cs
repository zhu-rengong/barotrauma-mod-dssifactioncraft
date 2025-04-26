using System;
using Barotrauma;
using Barotrauma.Items.Components;
using MoonSharp.Interpreter;

namespace DSSIFactionCraft.Items.Components
{
    internal class DfcNewFaction : ItemComponent, IDfcParticipatory, IDfcTaggable
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

        [InGameEditable, Serialize(CharacterTeamType.None, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public CharacterTeamType TeamID { get; set; }

        [InGameEditable(MinValueInt = 0), Serialize(100, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public int MaxLives { get; set; }

        [InGameEditable, Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string OnJoinedChunk { get; set; }

        [InGameEditable, Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string Jobs { get; set; }

        [InGameEditable(MinValueInt = int.MinValue, MaxValueInt = int.MaxValue), Serialize(0, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public int Sort { get; set; }

        [InGameEditable, Serialize(true, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public bool NotifyTeammates { get; set; }

        [InGameEditable, Serialize(true, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public bool AllowRespawn { get; set; }

        [InGameEditable, Serialize(1.0f, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public float RespawnIntervalMultiplier { get; set; }

        [InGameEditable, Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.", description: "return a closure with the prototype of fun(identifier: string, dead: integer, total: integer):integer")]
        public string GetRespawnLimitPerTime { get; set; }

        public static DynValue GetParameterTable(Item item)
        {
            var component = item.GetComponent<DfcNewFaction>();
            if (component == null) { return DynValue.Nil; }
            var dynValue = DynValue.NewTable(GameMain.LuaCs.Lua);
            dynValue.Table["identifier"] = component.Identifier.IsNullOrEmpty() ? DynValue.Nil : component.Identifier;
            dynValue.Table["teamID"] = component.TeamID;
            dynValue.Table["maxLives"] = component.MaxLives;
            dynValue.Table["onJoinedChunk"] = component.OnJoinedChunk.IsNullOrEmpty() ? DynValue.Nil : component.OnJoinedChunk;
            dynValue.Table["jobs"] = LuaUtils.SplitToTable(component.Jobs, ',', StringSplitOptions.RemoveEmptyEntries);
            dynValue.Table["participantTickets"] = component.ParticipantTickets;
            dynValue.Table["participantNumberLimit"] = component.ParticipantNumberLimit;
            dynValue.Table["participantWeight"] = component.ParticipantWeight;
            dynValue.Table["characterTags"] = LuaUtils.SplitToTable(component.CharacterTags, ',', StringSplitOptions.RemoveEmptyEntries);
            dynValue.Table["sort"] = component.Sort;
            dynValue.Table["notifyTeammates"] = component.NotifyTeammates;
            dynValue.Table["allowRespawn"] = component.AllowRespawn;
            dynValue.Table["respawnIntervalMultiplier"] = component.RespawnIntervalMultiplier;
            dynValue.Table["getRespawnLimitPerTime"] = component.GetRespawnLimitPerTime;
            return dynValue;
        }

        public DfcNewFaction(Item item, ContentXElement element) : base(item, element) { }
    }
}