using System;
using System.Collections.Generic;
using System.Linq;
using Barotrauma;
using Barotrauma.Extensions;
using Barotrauma.Items.Components;
using Microsoft.Xna.Framework;
using MoonSharp.Interpreter;
using TargetType = DSSIFactionCraft.CharacterUtils.TargetType;

namespace DSSIFactionCraft.Items.Components
{
    internal class DfcItemBuilder : ItemComponent
    {
        protected static bool IsMultiplayerClient => GameMain.NetworkMember?.IsClient ?? false;

        private string itemBuilds;
        [InGameEditable, Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string ItemBuilds
        {
            get => itemBuilds;
            set
            {
                if (itemBuilds == value) { return; }
                itemBuilds = value;
                if (!IsMultiplayerClient && !itemBuilds.IsNullOrEmpty()) { TryParseItemBuilds(); }
            }
        }

        [InGameEditable, Serialize(true, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public bool CanSpawn { get; set; }

        [InGameEditable, Serialize(false, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public bool OnlySpawnInLinkedTo { get; set; }

        [InGameEditable(MinValueInt = -1, MaxValueInt = int.MaxValue), Serialize(-1, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public int Amount { get; set; }

        [InGameEditable, Serialize(false, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public bool ForceSpawnAtPositions { get; set; }

        [InGameEditable, Serialize(false, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public bool RequiredSender { get; set; }

        [InGameEditable, Serialize(TargetType.Any, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public TargetType SenderTargetType { get; set; }

        [InGameEditable, Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string SenderSpeciesNames { get; set; }

        [InGameEditable, Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string SenderGroup { get; set; }

        [InGameEditable, Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string SenderTags { get; set; }

        [InGameEditable, Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string Group { get; set; }

        [InGameEditable, Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string Identifier { get; set; }

        private static Dictionary<string, List<Character>> groupTriggers;

        private string DebugName => $"{item}";

        private static Table ItemBuilderMetatable
        {
            get
            {
                var dynValue = GameMain.LuaCs.Lua.Globals.Get(new object[] { "LuaUtilityBelt", "ItemBuilder" }) ?? DynValue.Nil;
                if (dynValue.IsNotNil() && dynValue.Type == DataType.Table) { return dynValue.Table; }
                return null;
            }
        }

        private DynValue itemBuilder;
        private Closure module_functionColon_spawnat;
        private Closure module_functionColon_spawnin;
        private Closure module_functionColon_give;

        public bool TryParseItemBuilds()
        {
            try
            {
                if (ItemBuilderMetatable is null)
                    throw new NullReferenceException("Required nothing from 'utilbelt.itbu' module!");
                DynValue itemBuilderBlocks = GameMain.LuaCs.Lua.DoString(itemBuilds);
                if (itemBuilderBlocks.IsNil() || itemBuilderBlocks.Type != DataType.Table)
                    throw new ArgumentException($"Failed to parse item builds for {DebugName}, expected a 'Table' as returned value, but got {itemBuilderBlocks}!");
                itemBuilder = ItemBuilderMetatable.MetaTable.RawGet(@"__call").Function.Call(
                    new DynValue[] { DynValue.Nil, itemBuilderBlocks });
                module_functionColon_spawnat = ItemBuilderMetatable.RawGet("spawnat").Function;
                module_functionColon_spawnin = ItemBuilderMetatable.RawGet("spawnin").Function;
                module_functionColon_give = ItemBuilderMetatable.RawGet("give").Function;
                return true;
            }
            catch (System.Exception e)
            {
                LuaCsLogger.HandleException(e, LuaCsMessageOrigin.LuaCs);
                itemBuilder = null;
                module_functionColon_spawnat = null;
                module_functionColon_spawnin = null;
                module_functionColon_give = null;
                return false;
            }
        }

        private IList<Item> includes;

        static DfcItemBuilder()
        {
            if (IsMultiplayerClient) { return; }

            GameMain.LuaCs.Hook.Add("roundStart", nameof(DfcItemBuilder), initialize);
            object initialize(params object[] args)
            {
                groupTriggers = new();
                return default;
            }
        }

        public DfcItemBuilder(Item item, ContentXElement element) : base(item, element) { }

        public override void ReceiveSignal(Signal signal, Connection connection)
        {
            if (IsMultiplayerClient) { return; }

            switch (connection.Name)
            {
                case "signal_in":
                    if (itemBuilder is null || !CanSpawn || Amount == 0) { return; }
                    Character sender = signal.sender;
                    if (RequiredSender && sender is null) { return; }

                    if (!OnlySpawnInLinkedTo)
                    {
                        if (sender is not null)
                        {
                            if (!CharacterUtils.Matches(sender, SenderTargetType, SenderSpeciesNames, SenderGroup, SenderTags)) { return; }

                            if (!Group.IsNullOrEmpty())
                            {
                                if (!groupTriggers.TryGetValue(Group, out List<Character>? triggers))
                                {
                                    triggers = new();
                                    groupTriggers.Add(Group, triggers);
                                }
                                triggers.RemoveAll(trigger => trigger.Removed);
                                if (triggers.Contains(sender)) { return; }
                                triggers.Add(sender);
                            }

                            if (ForceSpawnAtPositions)
                            {
                                try
                                {
                                    module_functionColon_spawnat.Call(new object[] { itemBuilder, GetSpawnPoint() });
                                }
                                catch (Exception e)
                                {
                                    LuaCsLogger.LogError($"Error in {Item}::{nameof(module_functionColon_spawnat)} and sender is not null, builds:\r\n{ItemBuilds}", LuaCsMessageOrigin.LuaCs);
                                    LuaCsLogger.HandleException(e, LuaCsMessageOrigin.LuaCs);
                                }
                            }
                            else
                            {
                                try
                                {
                                    module_functionColon_give.Call(new object[] { itemBuilder, sender });
                                }
                                catch (Exception e)
                                {
                                    LuaCsLogger.LogError($"Error in {Item}::{nameof(module_functionColon_give)}, builds:\r\n{ItemBuilds}", LuaCsMessageOrigin.LuaCs);
                                    LuaCsLogger.HandleException(e, LuaCsMessageOrigin.LuaCs);
                                }
                            }
                        }
                        else
                        {
                            try
                            {
                                module_functionColon_spawnat.Call(new object[] { itemBuilder, GetSpawnPoint() });
                            }
                            catch (Exception e)
                            {
                                LuaCsLogger.LogError($"Error in {Item}::{nameof(module_functionColon_spawnat)} and sender is null, builds:\r\n{ItemBuilds}", LuaCsMessageOrigin.LuaCs);
                                LuaCsLogger.HandleException(e, LuaCsMessageOrigin.LuaCs);
                            }
                        }
                    }

                    var linkedTo = GetItemsLinkedTo();
                    if (linkedTo.Any())
                    {
                        foreach (var item in linkedTo)
                        {
                            try
                            {
                                module_functionColon_spawnin.Call(new object[] { itemBuilder, item });
                            }
                            catch (Exception e)
                            {
                                LuaCsLogger.LogError($"Error in {Item}::{nameof(module_functionColon_spawnin)}, builds:\r\n{ItemBuilds}", LuaCsMessageOrigin.LuaCs);
                                LuaCsLogger.HandleException(e, LuaCsMessageOrigin.LuaCs);
                            }
                        }
                    }

                    Amount--;

                    Vector2 GetSpawnPoint()
                    {
                        includes ??= RegionUtils.GetFromLinkedTo(item, "dfc_regionincluded");
                        if (includes.Count == 0) { return item.WorldPosition; }
                        Item region = includes.GetRandomUnsynced();
                        return new Vector2(
                            Convert.ToSingle(Rand.Range(region.WorldRect.X, region.WorldRect.X + region.WorldRect.Width)),
                            Convert.ToSingle(Rand.Range(region.WorldRect.Y - region.WorldRect.Height, region.WorldRect.Y)));
                    }

                    IList<Item> GetItemsLinkedTo() => item.linkedTo.OfType<Item>().ToList();
                    break;
                case "toggle":
                    CanSpawn = !CanSpawn;
                    break;
                case "set_state":
                    CanSpawn = signal.value != "0";
                    break;
                default:
                    break;
            }
        }
    }
}
