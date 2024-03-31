using System;
using Barotrauma;
using Barotrauma.Items.Components;
using MoonSharp.Interpreter;

namespace DSSIFactionCraft.Items.Components
{
    internal class DfcAddOrRemoveJob : ItemComponent
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

        private string[] jobs = Array.Empty<string>();
        [InGameEditable, Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string Jobs
        {
            get
            {
                return string.Join(',', jobs);
            }
            set
            {
                jobs = value.Split(',', StringSplitOptions.RemoveEmptyEntries);
            }
        }

        private static Table _G { get => GameMain.LuaCs.Lua.Globals; }

        public DfcAddOrRemoveJob(Item item, ContentXElement element) : base(item, element) { }

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
                        var lClassFaction = _G.Get("Class").Function.Call(DynValue.NewString("dfc.faction")).Tuple[0].Table;
                        var addJob = lClassFaction.Get("addJob").Function;
                        var removeJob = lClassFaction.Get("removeJob").Function;
                        for (int i = 0; i < factions.Length; i++)
                        {
                            if (dfc.Table.Get(new object[] { "factions", factions[i] }) is DynValue { } iDynValue
                                && iDynValue.IsNotNil() && iDynValue.Table is Table faction)
                            {
                                for (int j = 0; j < jobs.Length; j++)
                                {
                                    switch (signal.value)
                                    {
                                        case "0":
                                            removeJob.Call(iDynValue, jobs[j]);
                                            break;
                                        default:
                                            addJob.Call(iDynValue, jobs[j]);
                                            break;
                                    }
                                }
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