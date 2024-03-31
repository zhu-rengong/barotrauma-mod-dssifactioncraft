using System;
using System.Collections.Generic;
using Barotrauma;
using Barotrauma.Items.Components;
using TargetType = DSSIFactionCraft.CharacterUtils.TargetType;

namespace DSSIFactionCraft.Items.Components
{
    internal class DfcCharacterSingleChecker : ItemComponent
    {
        protected static bool IsMultiplayerClient => GameMain.NetworkMember?.IsClient ?? false;

        [InGameEditable, Serialize(TargetType.Any, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public TargetType CharacterTargetType { get; set; }

        [InGameEditable, Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string CharacterSpeciesNames { get; set; }

        [InGameEditable, Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string CharacterGroup { get; set; }

        [InGameEditable, Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string CharacterTags { get; set; }

        readonly record struct EventMessage(string SignalValue, Character Character, Guid Guid, string Name, Identifier SpeciesName, Identifier Group, string[] Tags);

        private readonly Queue<EventMessage> queueMessages;

        public DfcCharacterSingleChecker(Item item, ContentXElement element) : base(item, element)
        {
            if (IsMultiplayerClient) { return; }

            queueMessages = new();
        }

        public override void Update(float deltaTime, Camera cam)
        {
            if (IsMultiplayerClient) { IsActive = false; return; }

            if (queueMessages.TryDequeue(out EventMessage message))
            {
                var character = message.Character;
                item.SendSignal(new Signal(message.SignalValue, sender: character), "signal_out");
                item.SendSignal(new Signal(message.Guid.ToString(), sender: character), $"output_guid");
                item.SendSignal(new Signal(message.Name, sender: character), $"output_name");
                item.SendSignal(new Signal(message.SpeciesName.Value, sender: character), $"output_speciesname");
                item.SendSignal(new Signal(message.Group.Value, sender: character), $"output_group");
                item.SendSignal(new Signal(string.Join(',', message.Tags), sender: character), $"output_tags");
            }
            else
            {
                IsActive = false;
            }
        }

        public override void ReceiveSignal(Signal signal, Connection connection)
        {
            if (IsMultiplayerClient) { return; }

            switch (connection.Name)
            {
                case "signal_in":
                    if (signal.sender is Character sender && CharacterUtils.Matches(sender,
                        CharacterTargetType, CharacterSpeciesNames, CharacterGroup, CharacterTags))
                    {
                        queueMessages.Enqueue(new(signal.value, sender, CharacterUtils.GetGuid(sender),
                            sender.Name, sender.SpeciesName, sender.Group, CharacterUtils.GetTags(sender)));
                        IsActive = true;
                    }
                    break;
                default:
                    break;
            }
        }
    }
}