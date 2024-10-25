using Barotrauma;
using Barotrauma.Items.Components;
using Barotrauma.Networking;
using MoonSharp.Interpreter.Interop;
using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Diagnostics.CodeAnalysis;
using Barotrauma.Extensions;
using System.Linq;
using System.Net.NetworkInformation;
using MoonSharp.Interpreter.Serialization.Json;
using static MoonSharp.Interpreter.CoreLib.DynamicModule;
using DSSIFactionCraft.Items.Components;

namespace DSSIFactionCraft
{
    [MoonSharpModule(Namespace = "DFC")]
    public partial class DFCModule
    {
        public static DynValue Factions => GameMain.LuaCs.Lua.Globals.RawGet(new object[] { "DFC", "Loaded", "factions" });
        public static DynValue JoinedFaction => GameMain.LuaCs.Lua.Globals.RawGet(new object[] { "DFC", "Loaded", "_joinedFaction" });

        public static void MoonSharpInit(Table globalTable, Table dfcTable)
        {
            DynValue components = DynValue.NewTable(dfcTable.OwnerScript);
            Table componentsTable = components.Table;
            dfcTable.Set("Components", components);
            componentsTable.Set(nameof(DfcNewSpawnPointSet), UserData.CreateStatic<DfcNewSpawnPointSet>());
            componentsTable.Set(nameof(DfcNewFaction), UserData.CreateStatic<DfcNewFaction>());
            componentsTable.Set(nameof(DfcNewJob), UserData.CreateStatic<DfcNewJob>());
            componentsTable.Set(nameof(DfcNewGear), UserData.CreateStatic<DfcNewGear>());

            MoonSharpInitProjSpecific(globalTable, dfcTable);
        }

        public static partial void MoonSharpInitProjSpecific(Table globalTable, Table dfcTable);
    }
}
