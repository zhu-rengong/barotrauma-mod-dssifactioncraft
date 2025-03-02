using System;
using System.Collections.Generic;
using System.Linq;
using Barotrauma;
using Barotrauma.Items.Components;
using HarmonyLib;
using Microsoft.Xna.Framework;

namespace DSSIFactionCraft.Items.Components
{
    internal partial class DfcScriptSubmarineLocker : ItemComponent
    {
        protected static bool IsMultiplayerClient => GameMain.NetworkMember?.IsClient ?? false;

        [InGameEditable, Serialize(true, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public bool LockX { get; set; }

        [InGameEditable, Serialize(true, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public bool LockY { get; set; }

        private static List<DfcScriptSubmarineLocker> submarineLockerScripts;

        static DfcScriptSubmarineLocker()
        {
            if (IsMultiplayerClient) { return; }

            submarineLockerScripts = new();

            GameMain.LuaCs.Hook.Add("roundStart", nameof(DfcScriptSubmarineLocker), roundStart);
            object roundStart(params object[] args)
            {
                Patch_SubmarineBody_Update.LockX.Clear();
                Patch_SubmarineBody_Update.LockY.Clear();
                submarineLockerScripts.RemoveAll(script => script.item.Removed);
                if (!submarineLockerScripts.Any()) { return default; }
                submarineLockerScripts.ForEach(script =>
                {
                    if (script.item.Submarine?.SubBody is SubmarineBody subBody)
                    {
                        if (script.LockX) { Patch_SubmarineBody_Update.LockX.Add(subBody); }
                        if (script.LockY) { Patch_SubmarineBody_Update.LockY.Add(subBody); }
                    }
                });
                return default;
            }
        }

        public DfcScriptSubmarineLocker(Item item, ContentXElement element) : base(item, element)
        {
            IsActive = false;
            if (IsMultiplayerClient) { return; }

            submarineLockerScripts.Add(this);
        }
    }

    [HarmonyPatch(declaringType: typeof(SubmarineBody))]
    [HarmonyPatch(methodName: nameof(SubmarineBody.Update))]
    class Patch_SubmarineBody_Update
    {
        public static HashSet<SubmarineBody> LockX { get; set; } = new();
        public static HashSet<SubmarineBody> LockY { get; set; } = new();

        static void Prefix(SubmarineBody __instance)
        {
            __instance.Body.LinearVelocity = new Vector2(
                LockX.Contains(__instance) ? 0.0f : __instance.Body.LinearVelocity.X,
                LockY.Contains(__instance) ? 0.0f : __instance.Body.LinearVelocity.Y);
        }
    }
}
