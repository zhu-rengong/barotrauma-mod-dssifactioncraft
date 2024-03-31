
using Barotrauma;
using System;
using Barotrauma.Items.Components;
using HarmonyLib;

namespace DSSIFactionCraft.Items.Components
{
    internal abstract partial class DfcSynchronous : ItemComponent
    {
        private IEventData[] eventDatas;
        private SerializableProperty signals;
        private string[] outputSignals;
        private float synchTimer = 0.0f;
        private bool needSync = false;

        protected string GetSynchronousOutput(int index)
        {
            if (index < 0 || index >= signalCount) { return null; };
            return outputSignals[index];
        }

        protected void SetSynchronousOutput(int index, string value)
        {
            if (value is null || index < 0 || index >= signalCount) { return; };
            outputSignals[index] = value;
        }

        partial void OnItemLoadedProjSpecific()
        {
            var ctor = AccessTools
                .TypeByName(@"Barotrauma.Items.Components.ButtonTerminal+EventData")
                .GetConstructor(new Type[] { typeof(int) });
            eventDatas = new IEventData[signalCount];
            for (int i = 0; i < signalCount; i++) { eventDatas[i] = (IEventData)ctor.Invoke(new object[] { i }); }
            signals = buttonTerminal.SerializableProperties["Signals".ToIdentifier()];
            outputSignals = new string[signalCount];
        }

        partial void UpdateProjSpecific(float deltaTime)
        {
            if (!AllowSynchronization) { return; }
            if (synchTimer > 0.0f) { synchTimer -= deltaTime; return; }
            if (!needSync) { return; }

            GameMain.NetworkMember.CreateEntityEvent(item, new Item.ChangePropertyEventData(signals, buttonTerminal));
            for (int i = 0; i < signalCount; i++)
            {
                string value = GetSynchronousOutput(i);
                if (value is null)
                {
                    buttonTerminal.Signals[i] = string.Empty;
                }
                else
                {
                    buttonTerminal.Signals[i] = value;
                    item.CreateServerEvent(buttonTerminal, eventDatas[i]);
                }
            }

            Array.Clear(outputSignals);
            synchTimer = SynchInterval;
            needSync = false;
        }
    }
}
