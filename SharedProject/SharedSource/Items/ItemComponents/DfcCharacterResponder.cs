using System;
using System.Collections.Generic;
using System.Linq;
using Barotrauma;
using Barotrauma.Extensions;
using Barotrauma.Items.Components;
using MoonSharp.Interpreter;
using TargetType = DSSIFactionCraft.CharacterUtils.TargetType;

namespace DSSIFactionCraft.Items.Components
{
    internal class DfcCharacterResponder : ItemComponent
    {
        protected static bool IsMultiplayerClient => GameMain.NetworkMember?.IsClient ?? false;

        private string chunk;
        [InGameEditable, Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string Chunk
        {
            get => chunk;
            set
            {
                if (chunk == value) { return; }
                chunk = value;
                if (!IsMultiplayerClient && !chunk.IsNullOrEmpty()) { TryLoadChunk(); }
            }
        }

        [InGameEditable, Serialize(true, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public bool CanResponse { get; set; }

        [InGameEditable, Serialize(true, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public bool ApplyToSender { get; set; }

        [InGameEditable, Serialize(TargetType.Any, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public TargetType CharacterTargetType { get; set; }

        [InGameEditable, Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string CharacterSpeciesNames { get; set; }

        [InGameEditable, Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string CharacterGroup { get; set; }

        [InGameEditable, Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string CharacterTags { get; set; }

        [InGameEditable, Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string Group { get; set; }

        [InGameEditable, Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string Identifier { get; set; }

        private static Dictionary<string, List<Character>> groupTriggers;

        private string DebugName => $"{item}";

        private Closure responseCallback;

        public bool TryLoadChunk()
        {
            try
            {
                var dynValue = GameMain.LuaCs.Lua.DoString(chunk);
                if (dynValue.IsNil() || dynValue.Type != DataType.Function)
                    throw new ArgumentException($"Failed to parse chunk for {DebugName}, expected a 'Function' as returned value, but got {responseCallback}!");
                responseCallback = dynValue.Function;
                return true;
            }
            catch (System.Exception e)
            {
                LuaCsLogger.HandleException(e, LuaCsMessageOrigin.LuaCs);
                responseCallback = null;
                return false;
            }
        }

        private IList<Item> includes;
        private IList<Item> excludes;

        static DfcCharacterResponder()
        {
            if (IsMultiplayerClient) { return; }

            GameMain.LuaCs.Hook.Add("roundStart", nameof(DfcCharacterResponder), initialize);
            object initialize(params object[] args)
            {
                groupTriggers = new();
                return default;
            }
        }

        public DfcCharacterResponder(Item item, ContentXElement element) : base(item, element) { }

        public bool Matches(Character character)
            => CharacterUtils.Matches(character, CharacterTargetType, CharacterSpeciesNames, CharacterGroup, CharacterTags);

        public override void ReceiveSignal(Signal signal, Connection connection)
        {
            if (IsMultiplayerClient) { return; }

            switch (connection.Name)
            {
                case "signal_in":
                    if (responseCallback is null || !CanResponse) { return; }
                    List<Character> charactersToApply = new();
                    if (ApplyToSender && signal.sender is Character sender && Matches(sender))
                    {
                        charactersToApply.Add(sender);
                    }
                    includes ??= RegionUtils.GetFromLinkedTo(item, "dfc_regionincluded");
                    excludes ??= RegionUtils.GetFromLinkedTo(item, "dfc_regionexcluded");
                    if (includes.Count > 0 || excludes.Count > 0)
                    {
                        Character.CharacterList.Where(character => Matches(character)
                            && RegionUtils.Contains(character.WorldPosition, includes, excludes))
                            .ForEach(character => charactersToApply.Add(character));
                    }
                    charactersToApply.ForEach(character =>
                    {
                        if (!Group.IsNullOrEmpty())
                        {
                            if (!groupTriggers.TryGetValue(Group, out List<Character>? triggers))
                            {
                                triggers = new();
                                groupTriggers.Add(Group, triggers);
                            }
                            triggers.RemoveAll(trigger => trigger.Removed);
                            if (triggers.Contains(character)) { return; }
                            triggers.Add(character);
                        }

                        try
                        {
                            responseCallback.Call(new object[] { character });
                        }
                        catch (Exception e)
                        {
                            LuaCsLogger.LogError($"Error in {Item}::{nameof(responseCallback)}, chunk:\r\n{Chunk}", LuaCsMessageOrigin.LuaCs);
                            LuaCsLogger.HandleException(e, LuaCsMessageOrigin.LuaCs);
                        }
                    });
                    break;
                case "toggle":
                    CanResponse = !CanResponse;
                    break;
                case "set_state":
                    CanResponse = signal.value != "0";
                    break;
                default:
                    break;
            }
        }
    }
}
