using System;
using System.Collections.Generic;
using System.Linq;
using Barotrauma;
using Barotrauma.Items.Components;
using TargetType = DSSIFactionCraft.CharacterUtils.TargetType;

namespace DSSIFactionCraft.Items.Components
{
    internal class DfcEventCharacterDeath : ItemComponent
    {
        protected static bool IsMultiplayerClient => GameMain.NetworkMember?.IsClient ?? false;

        private static List<DfcEventCharacterDeath> triggers;

        [InGameEditable, Serialize(TargetType.Any, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public TargetType DeathTargetType { get; set; }

        [InGameEditable, Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string DeathSpeciesNames { get; set; }

        [InGameEditable, Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string DeathGroup { get; set; }

        [InGameEditable, Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string DeathTags { get; set; }

        [InGameEditable, Serialize(TargetType.Any, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public TargetType KillerTargetType { get; set; }

        [InGameEditable, Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string KillerSpeciesNames { get; set; }

        [InGameEditable, Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string KillerGroup { get; set; }

        [InGameEditable, Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string KillerTags { get; set; }

        readonly record struct EventMessage(Character Character, Guid Guid, string Name, Identifier SpeciesName, Identifier Group, string[] Tags, string Type);

        private readonly Queue<Queue<EventMessage>> queueQueueMessage;

        static DfcEventCharacterDeath()
        {
            if (IsMultiplayerClient) { return; }

            triggers = new();
            GameMain.LuaCs.Hook.Add("character.death", nameof(DfcEventCharacterDeath), TriggerEvent);
            static object TriggerEvent(params object[] args)
            {
                triggers.RemoveAll(t => t.item.Removed);
                if (!triggers.Any()) { return default; }
                var dead = args[0] as Character;
                foreach (var trigger in triggers)
                {
                    Queue<EventMessage> queueMessage = new();

                    if (CharacterUtils.Matches(dead,
                        trigger.DeathTargetType,
                        trigger.DeathSpeciesNames,
                        trigger.DeathGroup,
                        trigger.DeathTags))
                    {
                        EnqueueEventMessage(dead, "dead");
                    }

                    if (dead.CauseOfDeath?.Killer is Character killer
                        && CharacterUtils.Matches(killer,
                            trigger.KillerTargetType,
                            trigger.KillerSpeciesNames,
                            trigger.KillerGroup,
                            trigger.KillerTags))
                    {
                        EnqueueEventMessage(killer, "killer");
                    }

                    if (queueMessage.Count > 0)
                    {
                        trigger.queueQueueMessage.Enqueue(queueMessage);
                        trigger.IsActive = true;
                    }

                    void EnqueueEventMessage(Character character, string type)
                    {
                        queueMessage.Enqueue(new(character, CharacterUtils.GetGuid(character),
                            character.Name, character.SpeciesName, character.Group, CharacterUtils.GetTags(character), type));
                    }
                }
                return default;
            }
        }

        public DfcEventCharacterDeath(Item item, ContentXElement element) : base(item, element)
        {
            if (IsMultiplayerClient) { return; }

            queueQueueMessage = new();
            triggers.Add(this);
        }

        public override void Update(float deltaTime, Camera cam)
        {
            if (IsMultiplayerClient) { IsActive = false; return; }

            if (queueQueueMessage.TryDequeue(out Queue<EventMessage> queueMessage))
            {
                while (queueMessage.TryDequeue(out EventMessage message))
                {
                    var character = message.Character;
                    var type = message.Type;
                    item.SendSignal(new Signal(message.Guid.ToString(), sender: character), $"output_{type}_guid");
                    item.SendSignal(new Signal(message.Name, sender: character), $"output_{type}_name");
                    item.SendSignal(new Signal(message.SpeciesName.Value, sender: character), $"output_{type}_speciesname");
                    item.SendSignal(new Signal(message.Group.Value, sender: character), $"output_{type}_group");
                    item.SendSignal(new Signal(string.Join(',', message.Tags), sender: character), $"output_{type}_tags");
                }
            }
            else
            {
                IsActive = false;
            }
        }
    }
}