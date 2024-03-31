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
    internal class DfcItemBatch : ItemComponent
    {
        protected static bool IsMultiplayerClient => GameMain.NetworkMember?.IsClient ?? false;

        private string itemBatchUnit;
        [InGameEditable, Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string ItemBatchUnit
        {
            get => itemBatchUnit;
            set
            {
                if (itemBatchUnit == value) { return; }
                itemBatchUnit = value;
                if (!IsMultiplayerClient && !itemBatchUnit.IsNullOrEmpty()) { TryParseItemBatchUnit(); }
            }
        }

        [InGameEditable, Serialize(true, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public bool CanBatch { get; set; }

        [InGameEditable(MinValueInt = -1, MaxValueInt = int.MaxValue), Serialize(-1, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public int Amount { get; set; }

        [InGameEditable, Serialize(false, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public bool OnlyBatchLinkedTo { get; set; }

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

        private static Table ItemBatchMetatable
        {
            get
            {
                var dynValue = GameMain.LuaCs.Lua.Globals.Get(new object[] { "LuaUtilityBelt", "ItemBatch" }) ?? DynValue.Nil;
                if (dynValue.IsNotNil() && dynValue.Type == DataType.Table) { return dynValue.Table; }
                return null;
            }
        }

        private DynValue itemBatch;
        private Closure module_functionColon_run;
        private Closure module_functionColon_runforinv;

        public bool TryParseItemBatchUnit()
        {
            try
            {
                if (ItemBatchMetatable is null)
                    throw new NullReferenceException("Required nothing from 'utilbelt.itbu' module!");
                DynValue itemBatchBlocks = GameMain.LuaCs.Lua.DoString(itemBatchUnit);
                if (itemBatchBlocks.IsNil() || itemBatchBlocks.Type != DataType.Table)
                    throw new ArgumentException($"Failed to parse item batch for {DebugName}, expected a 'Table' as returned value, but got {itemBatchBlocks}!");
                itemBatch = ItemBatchMetatable.MetaTable.RawGet(@"__call").Function.Call(
                    new DynValue[] { DynValue.Nil, itemBatchBlocks });
                module_functionColon_run = ItemBatchMetatable.RawGet("run").Function;
                module_functionColon_runforinv = ItemBatchMetatable.RawGet("runforinv").Function;
                return true;
            }
            catch (System.Exception e)
            {
                LuaCsLogger.HandleException(e, LuaCsMessageOrigin.LuaCs);
                itemBatch = null;
                module_functionColon_run = null;
                module_functionColon_runforinv = null;
                return false;
            }
        }

        private IList<Item> itemsLinkedTo;

        static DfcItemBatch()
        {
            if (IsMultiplayerClient) { return; }

            GameMain.LuaCs.Hook.Add("roundStart", nameof(DfcItemBatch), initialize);
            object initialize(params object[] args)
            {
                groupTriggers = new();
                return default;
            }
        }

        public DfcItemBatch(Item item, ContentXElement element) : base(item, element) { }

        public override void ReceiveSignal(Signal signal, Connection connection)
        {
            if (IsMultiplayerClient) { return; }

            switch (connection.Name)
            {
                case "signal_in":
                    if (itemBatch is null || !CanBatch || Amount == 0) { return; }
                    Character sender = signal.sender;
                    if (RequiredSender && sender is null) { return; }
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

                        module_functionColon_run.Call(new object[] { itemBatch, GetItemsLinkedTo() });
                        if (!OnlyBatchLinkedTo && sender.Inventory != null)
                        {
                            try
                            {
                                module_functionColon_runforinv.Call(new object[] { itemBatch, sender.Inventory });
                            }
                            catch (Exception e)
                            {
                                LuaCsLogger.LogError($"Error in {Item}::{nameof(module_functionColon_runforinv)}, unit:\r\n{ItemBatchUnit}", LuaCsMessageOrigin.LuaCs);
                                LuaCsLogger.HandleException(e, LuaCsMessageOrigin.LuaCs);
                            }
                        }
                    }
                    else
                    {
                        try
                        {
                            module_functionColon_run.Call(new object[] { itemBatch, GetItemsLinkedTo() });
                        }
                        catch (Exception e)
                        {
                            LuaCsLogger.LogError($"Error in {Item}::{nameof(module_functionColon_run)}, unit:\r\n{ItemBatchUnit}", LuaCsMessageOrigin.LuaCs);
                            LuaCsLogger.HandleException(e, LuaCsMessageOrigin.LuaCs);
                        }
                    }

                    Amount--;
                    IList<Item> GetItemsLinkedTo() => item.linkedTo.OfType<Item>().ToList();
                    break;
                case "toggle":
                    CanBatch = !CanBatch;
                    break;
                case "set_state":
                    CanBatch = signal.value != "0";
                    break;
                default:
                    break;
            }
        }
    }
}
