using System;
using Barotrauma;
using Barotrauma.Items.Components;
using MoonSharp.Interpreter;

namespace DSSIFactionCraft.Items.Components
{
    internal class DfcAllowRespawn : ItemComponent
    {
        protected static bool IsMultiplayerClient => GameMain.NetworkMember?.IsClient ?? false;

        private string[] factions = Array.Empty<string>();
        [InGameEditable(), Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string Factions
        {
            get
            {
                return string.Join(',', factions);
            }
            set
            {
                factions = value.Split(',', StringSplitOptions.RemoveEmptyEntries);
            }
        }

        private static Table _G { get => GameMain.LuaCs.Lua.Globals; }

        public DfcAllowRespawn(Item item, ContentXElement element) : base(item, element) { }

        public override void ReceiveSignal(Signal signal, Connection connection)
        {
            if (IsMultiplayerClient) { return; }

            switch (connection.Name)
            {
                case "signal_in":
                    try
                    {
                        var dfc = _G.Get(new object[] { "DFC", "Loaded" });
                        if (dfc.IsNil()) { return; }
                        for (int i = 0; i < factions.Length; i++)
                        {
                            if (dfc.Table.Get(new object[] { "factions", factions[i] }) is DynValue { } iDynValue
                                && iDynValue.IsNotNil() && iDynValue.Table is Table faction)
                            {
                                faction.Set("allowRespawn", DynValue.NewBoolean(signal.value == "0" ? false : true));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        GameMain.LuaCs.HandleException(ex, LuaCsMessageOrigin.LuaCs);
                    }

                    break;
                default:
                    break;
            }
        }
    }
}