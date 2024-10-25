using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Barotrauma;
using Barotrauma.Networking;
using DSSIFactionCraft.Networking;
using HarmonyLib;
using MoonSharp.Interpreter;
using static Barotrauma.Networking.RespawnManager;

namespace DSSIFactionCraft
{
    public partial class Plugin : IAssemblyPlugin
    {
        public static DfcRespawnManager DfcRespawnManager { get; private set; }

        [HarmonyPatch(declaringType: typeof(RespawnManager))]
        [HarmonyPatch(methodType: MethodType.Constructor)]
        [HarmonyPatch(argumentTypes: new Type[] {
            typeof(NetworkMember),
            typeof(SubmarineInfo)
        })]
        class Patch_RespawnManager_Ctor
        {
            static void Postfix(RespawnManager __instance)
            {
                if (DFCModule.OverrideRespawnManager is DynValue { Type: DataType.Boolean, Boolean: true })
                {
                    DfcRespawnManager = new();
                }
            }
        }

        [HarmonyPatch(declaringType: typeof(RespawnManager))]
        [HarmonyPatch(methodName: nameof(RespawnManager.Update))]
        class Patch_RespawnManager_Update
        {
            [HarmonyPrefix]
            static bool Override(RespawnManager __instance)
            {
                if (DFCModule.OverrideRespawnManager is DynValue { Type: DataType.Boolean, Boolean: true })
                {
                    DfcRespawnManager?.Update();

                    return false;
                }

                return true;
            }
        }

    }
}
