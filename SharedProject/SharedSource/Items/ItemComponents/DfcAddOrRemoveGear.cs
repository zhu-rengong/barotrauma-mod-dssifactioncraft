using System;
using Barotrauma;
using Barotrauma.Items.Components;
using MoonSharp.Interpreter;

namespace DSSIFactionCraft.Items.Components
{
    internal class DfcAddOrRemoveGear : ItemComponent
    {
        protected static bool IsMultiplayerClient => GameMain.NetworkMember?.IsClient ?? false;

        private string[] jobs = Array.Empty<string>();
        [InGameEditable(), Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
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

        private string[] gears = Array.Empty<string>();
        [InGameEditable, Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string Gears
        {
            get
            {
                return string.Join(',', gears);
            }
            set
            {
                gears = value.Split(',', StringSplitOptions.RemoveEmptyEntries);
            }
        }

        private static Table _G { get => GameMain.LuaCs.Lua.Globals; }

        public DfcAddOrRemoveGear(Item item, ContentXElement element) : base(item, element) { }

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
                        var lClassJob = _G.Get("Class").Function.Call(DynValue.NewString("dfc.job")).Tuple[0].Table;
                        var addGear = lClassJob.Get("addGear").Function;
                        var removeGear = lClassJob.Get("removeGear").Function;
                        for (int i = 0; i < jobs.Length; i++)
                        {
                            if (dfc.Table.Get(new object[] { "jobs", jobs[i] }) is DynValue { } iDynValue
                                && iDynValue.IsNotNil() && iDynValue.Table is Table job)
                            {
                                for (int j = 0; j < gears.Length; j++)
                                {
                                    switch (signal.value)
                                    {
                                        case "0":
                                            removeGear.Call(iDynValue, gears[j]);
                                            break;
                                        default:
                                            addGear.Call(iDynValue, gears[j]);
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