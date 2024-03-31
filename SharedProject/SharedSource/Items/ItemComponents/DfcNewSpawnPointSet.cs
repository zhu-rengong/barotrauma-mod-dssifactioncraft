using System;
using System.Linq;
using Barotrauma;
using Barotrauma.Items.Components;
using MoonSharp.Interpreter;

namespace DSSIFactionCraft.Items.Components
{
    internal class DfcNewSpawnPointSet : ItemComponent, IDfcTaggable
    {
        [InGameEditable(), Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string CharacterTags { get; set; }

        [InGameEditable, Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string Identifier { get; set; }

        [InGameEditable, Serialize(true, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public bool FilterByTag { get; set; }

        [InGameEditable, Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string Tag { get; set; }

        [InGameEditable, Serialize(false, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public bool FilterBySpawnType { get; set; }

        [InGameEditable, Serialize(SpawnType.Human | SpawnType.Enemy, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public SpawnType SpawnType { get; set; }

        [InGameEditable, Serialize(false, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public bool FilterByAssignedJob { get; set; }

        [InGameEditable, Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string AssignedJob { get; set; }

        [InGameEditable, Serialize(false, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public bool FilterByTeamID { get; set; }

        [InGameEditable, Serialize(CharacterTeamType.None, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public CharacterTeamType TeamID { get; set; }

        public static DynValue GetParameterTable(Item item)
        {
            var component = item.GetComponent<DfcNewSpawnPointSet>();
            if (component == null) { return DynValue.Nil; }
            var dynValue = DynValue.NewTable(GameMain.LuaCs.Lua);
            dynValue.Table["identifier"] = component.Identifier.IsNullOrEmpty() ? DynValue.Nil : component.Identifier;
            dynValue.Table["tag"] = component.FilterByTag ? component.Tag : DynValue.Nil;
            dynValue.Table["spawnType"] = component.FilterBySpawnType ? component.SpawnType : DynValue.Nil;
            dynValue.Table["assignedJob"] = component.FilterByAssignedJob ? component.AssignedJob : DynValue.Nil;
            dynValue.Table["teamID"] = component.FilterByTeamID ? component.TeamID : DynValue.Nil;
            dynValue.Table["characterTags"] = LuaUtils.SplitToTable(component.CharacterTags, ',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var pair in dynValue.Table.Pairs.ToArray())
            {
                if (pair.Value.Type == DataType.String && pair.Value.String.IsNullOrEmpty())
                {
                    dynValue.Table.Remove(pair.Key);
                }
            }
            return dynValue;
        }

        public DfcNewSpawnPointSet(Item item, ContentXElement element) : base(item, element) { }
    }
}