using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;
using Barotrauma;
using Microsoft.Xna.Framework;
using MoonSharp.Interpreter;
using DSSIFactionCraft.Items.Components;
using HarmonyLib;

#if CLIENT
[assembly: IgnoresAccessChecksTo("Barotrauma")]
#endif
#if SERVER
[assembly: IgnoresAccessChecksTo("DedicatedServer")]
#endif
[assembly: IgnoresAccessChecksTo("BarotraumaCore")]

namespace DSSIFactionCraft
{
    public partial class Plugin : IAssemblyPlugin
    {
        private Harmony harmony;

        public void Initialize()
        {
            harmony = new Harmony("dfc");
            harmony.PatchAll();
        }

        public void OnLoadCompleted()
        {
            UserData.RegisterType<DfcNewSpawnPointSet>();
            UserData.RegisterType<DfcNewFaction>();
            UserData.RegisterType<DfcNewJob>();
            UserData.RegisterType<DfcNewGear>();

            ModuleRegister.RegisterModuleType<DFCModule>(GameMain.LuaCs.Lua.Globals);

            RuntimeHelpers.RunClassConstructor(typeof(CharacterUtils).TypeHandle);
            RuntimeHelpers.RunClassConstructor(typeof(XMLExtensions).TypeHandle);
            RuntimeHelpers.RunClassConstructor(typeof(DfcScriptWifiInitializer).TypeHandle);
            RuntimeHelpers.RunClassConstructor(typeof(DfcScriptSubmarineLocker).TypeHandle);
        }

        public void PreInitPatching()
        {
            // Not yet supported: Called during the Barotrauma startup phase before vanilla content is loaded.
        }

        public void Dispose()
        {
            harmony?.UnpatchAll();
            harmony = null;
        }
    }
}
