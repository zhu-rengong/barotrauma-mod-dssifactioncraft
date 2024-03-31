using Barotrauma;
using System.Linq;
using System.Reflection;
using Barotrauma.Items.Components;
using System;
using System.ComponentModel;

namespace DSSIFactionCraft.Items.Components
{
    internal abstract partial class DfcSynchronous : ItemComponent
    {
        protected static bool IsMultiplayerClient => GameMain.NetworkMember?.IsClient ?? false;

        private ButtonTerminal buttonTerminal;
        protected int signalCount = 0;
        private Connection[] signalOutConnections;

        [Editable, Serialize(false, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public bool AllowSynchronization { get; set; }

        private float synchInterval;
        [Editable(MinValueFloat = 0.1f, DecimalCount = 2)]
        [Serialize(1.0f, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public float SynchInterval
        {
            get { return synchInterval; }
#if SERVER
            set
            {
                synchInterval = value;
                synchTimer = Math.Clamp(synchTimer, 0, synchInterval);
            }
#else
            set { synchInterval = value; }
#endif
        }

        public DfcSynchronous(Item item, ContentXElement element) : base(item, element) { }

        partial void OnItemLoadedProjSpecific();

        public override void OnItemLoaded()
        {
            base.OnItemLoaded();

            if (IsMultiplayerClient) { return; }

            buttonTerminal = item.GetComponent<ButtonTerminal>();

            signalCount = (typeof(ButtonTerminal)
                .GetProperty("RequiredSignalCount", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetProperty)
                .GetValue(buttonTerminal) as int?).Value;

            signalOutConnections = new Connection[signalCount];
            for (int i = 0; i < signalCount; i++)
            {
                signalOutConnections[i] = item.Connections.FirstOrDefault(
                    connection => connection.Name == $"signal_out{i + 1}");
            }

            OnItemLoadedProjSpecific();
        }

        protected void SendSynchronousSignal(int index, string value, Character sender = null)
        {
            if (value is null) { return; }
            item.SendSignal(new Signal(value, source: item, sender: sender), signalOutConnections[index]);
#if SERVER
            SetSynchronousOutput(index, value);
            needSync = true;
#endif
        }

        public abstract void UpdateHeir(float deltaTime, Camera cam);

        partial void UpdateProjSpecific(float deltaTime);

        sealed public override void Update(float deltaTime, Camera cam)
        {
            if (IsMultiplayerClient) { IsActive = false; return; }
            UpdateHeir(deltaTime, cam);
            UpdateProjSpecific(deltaTime);
        }
    }
}
