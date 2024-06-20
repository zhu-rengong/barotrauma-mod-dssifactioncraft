using System.Diagnostics;
using Barotrauma;
using Barotrauma.Items.Components;
using Microsoft.Xna.Framework;

namespace DSSIFactionCraft.Items.Components
{
    internal class DfcTeleporter : ItemComponent
    {
        [InGameEditable, Serialize(false, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public bool DisableTeleportDraggedCharacter { get; set; }

        [InGameEditable, Serialize(true, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public bool TeleportAble { get; set; }

        [InGameEditable(MinValueFloat = 0.0f, MaxValueFloat = 60.0f, DecimalCount = 2)]
        [Serialize(1.0f, IsPropertySaveable.Yes, description: "How long will it take to teleport again (in seconds)?", alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public float Cooldown { get; set; }

        private double LastTeleportTime { get; set; } = 0.0f;

        public DfcTeleporter(Item item, ContentXElement element) : base(item, element) { }

        public override void ReceiveSignal(Signal signal, Connection connection)
        {
            if (GameMain.NetworkMember?.IsClient ?? false) { return; }
            switch (connection.Name)
            {
                case "signal_in":
                    if (!TeleportAble) { return; }
                    if (signal.sender is Character sender)
                    {
                        if (Timing.TotalTime - LastTeleportTime < Cooldown) { return; }
                        sender.TeleportTo(item.WorldPosition);
                        if (!DisableTeleportDraggedCharacter && sender.SelectedCharacter is Character { IsDraggable: true } selectedCharacter)
                        {
                            selectedCharacter.TeleportTo(sender.WorldPosition);
                        }
                        LastTeleportTime = Timing.TotalTime;
                    }
                    break;
                case "toggle":
                    TeleportAble = !TeleportAble;
                    break;
                case "set_state":
                    TeleportAble = signal.value != "0";
                    break;
                default:
                    break;
            }
        }
    }
}