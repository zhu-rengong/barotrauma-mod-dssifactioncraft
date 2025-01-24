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

namespace DSSIFactionCraft
{
    public partial class DfcModule
    {
        public const string FIELD_NAME_OVERRIDE_RESPAWN_MANAGER = "OverrideRespawnManager";

        public static DynValue OverrideRespawnManager => GameMain.LuaCs.Lua.Globals.RawGet(new object[] { "DFC", FIELD_NAME_OVERRIDE_RESPAWN_MANAGER });
        public static DynValue WaitRespawn => GameMain.LuaCs.Lua.Globals.RawGet(new object[] { "DFC", "Loaded", "_waitRespawn" });
        public static DynValue AllowRespawn => GameMain.LuaCs.Lua.Globals.RawGet(new object[] { "DFC", "Loaded", "allowRespawn" });

        public static partial void MoonSharpInitProjSpecific(Table globalTable, Table dfcTable)
        {
            dfcTable[FIELD_NAME_OVERRIDE_RESPAWN_MANAGER] = DynValue.NewBoolean(false);
        }
    }
}
