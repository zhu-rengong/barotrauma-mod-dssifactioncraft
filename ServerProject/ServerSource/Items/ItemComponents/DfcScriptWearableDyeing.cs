using System;
using System.Collections.Generic;
using Barotrauma;
using Barotrauma.Items.Components;
using Microsoft.Xna.Framework;
using TargetType = DSSIFactionCraft.CharacterUtils.TargetType;

namespace DSSIFactionCraft.Items.Components
{
    internal partial class DfcScriptWearableDyeing : ItemComponent
    {
        readonly record struct EventMessage(Item EquippedItem, SerializableProperty SP_spritecolor, SerializableProperty SP_inventoryiconcolor);

        private readonly Queue<EventMessage> queueMessages = new();

        public override void Update(float deltaTime, Camera cam)
        {
            if (queueMessages.TryDequeue(out EventMessage message))
            {
                if (!message.EquippedItem.Removed)
                {
                    GameMain.NetworkMember.CreateEntityEvent(message.EquippedItem, new Item.ChangePropertyEventData(message.SP_spritecolor, message.EquippedItem));
                    GameMain.NetworkMember.CreateEntityEvent(message.EquippedItem, new Item.ChangePropertyEventData(message.SP_inventoryiconcolor, message.EquippedItem));
                }
            }
            else
            {
                IsActive = false;
            }
        }
    }
}
