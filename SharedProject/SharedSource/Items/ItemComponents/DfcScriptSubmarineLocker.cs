using System;
using System.Collections.Generic;
using System.Linq;
using Barotrauma;
using Barotrauma.Items.Components;
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
                submarineLockerScripts.RemoveAll(script => script.item.Removed);
                if (!submarineLockerScripts.Any()) { return default; }
                submarineLockerScripts.ForEach(script =>
                {
                    Submarine.LockX = script.LockX;
                    Submarine.LockY = script.LockY;
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
}
