using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;
using Barotrauma;
using Microsoft.Xna.Framework;
using MoonSharp.Interpreter;
using DSSIFactionCraft.Items.Components;

[assembly: IgnoresAccessChecksTo("Barotrauma")]

namespace DSSIFactionCraft
{
    public partial class Plugin : IAssemblyPlugin
    {
        public void Initialize()
        {

        }

        public void OnLoadCompleted()
        {
            GameMain.LuaCs.Lua.Globals["DFC"] = DynValue.NewTable(GameMain.LuaCs.Lua);
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

        }
    }
}
