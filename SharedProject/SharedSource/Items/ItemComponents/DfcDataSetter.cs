using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using Barotrauma;
using Barotrauma.Items.Components;
using Microsoft.Xna.Framework;

namespace DSSIFactionCraft.Items.Components
{
    internal class DfcDataSetter : ItemComponent
    {
        protected static bool IsMultiplayerClient => GameMain.NetworkMember?.IsClient ?? false;

        [InGameEditable, Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string DataIndex { get; set; }

        public DfcDataSetter(Item item, ContentXElement element) : base(item, element) { }

        private void Set(string index, string value)
        {
            DfcDataGetter.Datas[index] = value;
        }

        public override void ReceiveSignal(Signal signal, Connection connection)
        {
            if (IsMultiplayerClient) { return; }

            switch (connection.Name)
            {
                case "set_data":
                    Set(DataIndex, signal.value);
                    break;
                case "set_index":
                    DataIndex = signal.value;
                    break;
                default:
                    break;
            }
        }
    }
}