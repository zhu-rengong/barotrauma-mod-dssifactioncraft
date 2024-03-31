using System;
using System.Collections.Generic;
using System.Linq;
using Barotrauma;
using Barotrauma.Items.Components;
using Barotrauma.Networking;
using TargetType = DSSIFactionCraft.CharacterUtils.TargetType;

namespace DSSIFactionCraft.Items.Components
{
    internal class DfcActionSendChatMessage : ItemComponent
    {
        public enum SenderReceiverRelationType
        {
            None,
            OnlySameTeam,
            OnlyDiffTeam
        }

        protected static bool IsMultiplayerClient => GameMain.NetworkMember?.IsClient ?? false;

        [InGameEditable, Serialize(false, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public bool OnlySenderReceivable { get; set; }

        [InGameEditable, Serialize(SenderReceiverRelationType.None, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public SenderReceiverRelationType SenderReceiverRelation { get; set; }

        [InGameEditable, Serialize(TargetType.Any, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public TargetType SenderTargetType { get; set; }

        [InGameEditable, Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string SenderSpeciesNames { get; set; }

        [InGameEditable, Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string SenderGroup { get; set; }

        [InGameEditable, Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string SenderTags { get; set; }

        [InGameEditable, Serialize(TargetType.Any, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public TargetType ReceiverTargetType { get; set; }

        [InGameEditable, Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string ReceiverSpeciesNames { get; set; }

        [InGameEditable, Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string ReceiverGroup { get; set; }

        [InGameEditable, Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string ReceiverTags { get; set; }

        [InGameEditable, Serialize(true, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public bool SpectatorReceivable { get; set; }

        [InGameEditable, Serialize(true, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public bool SpectatorOnlyReceivable { get; set; }

        private IList<Item> includes;
        private IList<Item> excludes;

        public DfcActionSendChatMessage(Item item, ContentXElement element) : base(item, element) { }

        public override void Update(float deltaTime, Camera cam)
        {
            IsActive = false;
            if (IsMultiplayerClient) { return; }
            includes ??= RegionUtils.GetFromLinkedTo(item, "dfc_regionincluded");
            excludes ??= RegionUtils.GetFromLinkedTo(item, "dfc_regionexcluded");
        }

        public override void ReceiveSignal(Signal signal, Connection connection)
        {
            if (IsMultiplayerClient) { return; }

            Character sender = signal.sender;
            if (sender is not null && !CharacterUtils.Matches(sender, SenderTargetType, SenderSpeciesNames, SenderGroup, SenderTags)) { return; }

            switch (connection.Name)
            {
                case "signal_in":
#if SERVER
                    var viableReceivers = Client.ClientList.Where(client =>
                    {
                        if (SpectatorReceivable && client.Spectating) { return true; }
                        if (SpectatorOnlyReceivable && client.SpectateOnly) { return true; }
                        Character receiver = client.Character;
                        if (receiver is null) { return false; }
                        if (sender is not null)
                        {
                            if (SenderReceiverRelation == SenderReceiverRelationType.OnlySameTeam
                                && sender.TeamID != receiver.TeamID) { return false; }
                            else if (SenderReceiverRelation == SenderReceiverRelationType.OnlyDiffTeam
                                && sender.TeamID == receiver.TeamID) { return false; }
                        }
                        if (!CharacterUtils.Matches(receiver, ReceiverTargetType, ReceiverSpeciesNames, ReceiverGroup, ReceiverTags)) { return false; }
                        if (OnlySenderReceivable && receiver != sender) { return false; }
                        return true;
                    });
                    foreach (var client in viableReceivers)
                    {
                        GameMain.Server.SendDirectChatMessage(signal.value, client, ChatMessageType.Default);
                    }
#elif CLIENT
                    if (Character.Controlled is Character controlled)
                    {
                        if (sender is not null)
                        {
                            if (SenderReceiverRelation == SenderReceiverRelationType.OnlySameTeam
                                && sender.TeamID != controlled.TeamID) { return; }
                            else if (SenderReceiverRelation == SenderReceiverRelationType.OnlyDiffTeam
                                && sender.TeamID == controlled.TeamID) { return; }
                        }
                        if (!CharacterUtils.Matches(controlled, ReceiverTargetType, ReceiverSpeciesNames, ReceiverGroup, ReceiverTags)) { return; }
                        if (OnlySenderReceivable && controlled != sender) { return; }
                    }
                    else if (!SpectatorReceivable)
                    {
                        return;
                    }
                    GameMain.GameSession?.CrewManager?.ChatBox.AddMessage(
                        ChatMessage.Create("", signal.value, ChatMessageType.Default, null));
#endif
                    break;
                default:
                    break;
            }
        }
    }
}