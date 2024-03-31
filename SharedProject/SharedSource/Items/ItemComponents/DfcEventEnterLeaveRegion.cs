using System;
using System.Collections.Generic;
using System.Linq;
using Barotrauma;
using Barotrauma.Items.Components;
using TargetType = DSSIFactionCraft.CharacterUtils.TargetType;

namespace DSSIFactionCraft.Items.Components
{
    internal class DfcEventEnterLeaveRegion : ItemComponent
    {
        readonly record struct EventMessage(Character Character, Guid Guid, string Name, Identifier SpeciesName, Identifier Group, string[] Tags, bool Entered);

        protected static bool IsMultiplayerClient => GameMain.NetworkMember?.IsClient ?? false;

        [InGameEditable, Serialize(TargetType.Any, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public TargetType CharacterTargetType { get; set; }

        [InGameEditable, Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string CharacterSpeciesNames { get; set; }

        [InGameEditable, Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string CharacterGroup { get; set; }

        [InGameEditable, Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string CharacterTags { get; set; }

        [InGameEditable(DecimalCount = 3), Serialize(0.0f, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public float MinimumVelocity { get; set; }

        [InGameEditable, Serialize(true, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public bool MatchAfterLeave { get; set; }

        private IList<Item> includes;
        private IList<Item> excludes;
        private readonly Queue<EventMessage> queueMessages;
        private readonly List<Character> enteredMatchedCharacters;

        public DfcEventEnterLeaveRegion(Item item, ContentXElement element) : base(item, element)
        {
            if (IsMultiplayerClient) { IsActive = false; return; }

            IsActive = true;
            queueMessages = new();
            enteredMatchedCharacters = new();
        }

        public bool Matches(Character character)
            => character.CurrentSpeed >= MinimumVelocity
                && CharacterUtils.Matches(character, CharacterTargetType, CharacterSpeciesNames, CharacterGroup, CharacterTags);

        public override void Update(float deltaTime, Camera cam)
        {
            if (IsMultiplayerClient) { IsActive = false; return; }

            includes ??= RegionUtils.GetFromLinkedTo(item, "dfc_regionincluded");
            excludes ??= RegionUtils.GetFromLinkedTo(item, "dfc_regionexcluded");

            enteredMatchedCharacters.RemoveAll(character => character.Removed);

            var unenteredMatchedCharacters = Character.CharacterList.Where(character => !enteredMatchedCharacters.Contains(character) && Matches(character));
            var charactersToAdd = unenteredMatchedCharacters.Where(character => RegionUtils.Contains(character.WorldPosition, includes, excludes));
            var charactersToRemove = enteredMatchedCharacters.Where(character => !RegionUtils.Contains(character.WorldPosition, includes, excludes));
            foreach (var character in charactersToRemove.ToArray())
            {
                enteredMatchedCharacters.Remove(character);
                if (MatchAfterLeave && !Matches(character)) { continue; }
                queueMessages.Enqueue(CreateEventMessage(character, false));
            }
            foreach (var character in charactersToAdd)
            {
                enteredMatchedCharacters.Add(character);
                queueMessages.Enqueue(CreateEventMessage(character, true));
            }

            if (queueMessages.TryDequeue(out EventMessage message))
            {
                item.SendSignal(new Signal(message.Guid.ToString(), sender: message.Character), "output_guid");
                item.SendSignal(new Signal(message.Name, sender: message.Character), "output_name");
                item.SendSignal(new Signal(message.SpeciesName.ToString(), sender: message.Character), "output_speciesname");
                item.SendSignal(new Signal(message.Group.ToString(), sender: message.Character), "output_group");
                item.SendSignal(new Signal(string.Join(',', message.Tags), sender: message.Character), "output_tags");
                item.SendSignal(new Signal(message.Entered ? "1" : "0", sender: message.Character), "output_entered");
            }

            static EventMessage CreateEventMessage(Character character, bool entered)
                => new(character, CharacterUtils.GetGuid(character),
                    character.Name, character.SpeciesName, character.Group, CharacterUtils.GetTags(character), entered);
        }

        public override void ReceiveSignal(Signal signal, Connection connection)
        {
            if (IsMultiplayerClient) { return; }

            switch (connection.Name)
            {
                case "toggle":
                    if (signal.value != "0")
                    {
                        IsActive = !IsActive;
                    }
                    break;
                case "set_active":
                case "set_state":
                    IsActive = signal.value != "0";
                    break;
                default:
                    break;
            }
        }
    }
}