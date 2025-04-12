using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using Barotrauma;
using Barotrauma.Items.Components;
using Microsoft.Xna.Framework;

namespace DSSIFactionCraft.Items.Components
{
    [Obsolete]
    internal class DfcDataGetter : ItemComponent
    {
        protected static bool IsMultiplayerClient => GameMain.NetworkMember?.IsClient ?? false;

        private static Dictionary<string, string> datas;
        public static Dictionary<string, string> Datas => datas;

        static DfcDataGetter()
        {
            if (IsMultiplayerClient) { return; }

            GameMain.LuaCs.Hook.Add("roundStart", nameof(DfcDataGetter), initialize);
            object initialize(params object[] args)
            {
                datas = new();
                return default;
            }
        }

        [InGameEditable, Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string DataIndex { get; set; }

        [InGameEditable, Serialize("0", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string DefaultData { get; set; }

        public DfcDataGetter(Item item, ContentXElement element) : base(item, element) { }

        private string Get(string index)
        {
            if (!datas.TryGetValue(index, out string value)) { value = DefaultData; }
            return value;
        }

        public override void ReceiveSignal(Signal signal, Connection connection)
        {
            if (IsMultiplayerClient) { return; }

            switch (connection.Name)
            {
                case "get_data":
                    signal.value = Get(DataIndex);
                    item.SendSignal(signal, "signal_out");
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