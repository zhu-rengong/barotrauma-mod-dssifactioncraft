using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using Barotrauma;
using MoonSharp.Interpreter;
using Barotrauma.Items.Components;
using MoonSharp.Interpreter.Serialization.Json;
using System;
using System.Text.RegularExpressions;

namespace DSSIFactionCraft.Items.Components
{
    internal class DfcLuaComponent : DfcSynchronous
    {
        private readonly static Script luaScript;

        private bool executable = false;
        private Closure func;
        private int[] states;
        private (DynValue DynValue, Table Table) pendingSenders;
        private (DynValue DynValue, Table Table) inputs;
        private (DynValue DynValue, Table Table) outputs;
        private (DynValue DynValue, Table Table) memories;
        private Dictionary<Connection, int> inputMapLidx;

        private static Regex regexInvalidNumberStyle = new(@"^0\d+\.?", RegexOptions.Compiled);

        [InGameEditable, Serialize(true, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public bool Isolation { get; set; }

        private string chunk;
        [InGameEditable, Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string Chunk
        {
            get => chunk;
            set
            {
                if (chunk == value) { return; }
                chunk = value;
                if (!IsMultiplayerClient && !chunk.IsNullOrEmpty()) { TryComplieChunk(); }
            }
        }

        [InGameEditable, Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string JsonStoreData { get; set; }

        private Script environment;

        static DfcLuaComponent()
        {
            if (IsMultiplayerClient) { return; }
            luaScript = new Script(CoreModules.Preset_HardSandbox | CoreModules.OS_Time);
            luaScript.Options.DebugPrint = (o) => { LuaCsLogger.LogMessage(o); };
            luaScript.Options.CheckThreadAccess = false;
        }

        public DfcLuaComponent(Item item, ContentXElement element) : base(item, element) { }

        public override void OnItemLoaded()
        {
            base.OnItemLoaded();

            if (IsMultiplayerClient) { return; }

            inputMapLidx = new();
            for (int lidx = 1; lidx <= signalCount; lidx++)
            {
                inputMapLidx[item.Connections
                    .FirstOrDefault(c =>
                        c.Name == $"signal_in{lidx}")
                    ] = lidx;
            }

            TryComplieChunk();
        }

        public bool TryComplieChunk()
        {
            IsActive = false;

            if (Chunk.IsNullOrEmpty()) { return false; }

            try
            {
                environment = Isolation ? luaScript : GameMain.LuaCs.Lua;

                func = environment.DoString(@$"return function(inp, out, mem, flip, sender)
{Chunk}
end").Function;
                states = new int[signalCount];
                Array.Fill(states, -1);

                Table pendingSenders = new(environment);
                this.pendingSenders = new(DynValue.NewTable(pendingSenders), pendingSenders);

                Table inputs = new(environment);
                this.inputs = new(DynValue.NewTable(inputs), inputs);
                Table outputs = new(environment);
                this.outputs = new(DynValue.NewTable(outputs), outputs);

                Table memories = JsonStoreData.IsNullOrEmpty() ?
                    new(environment) : JsonTableConverter.JsonToTable(JsonStoreData, environment);
                this.memories = new(DynValue.NewTable(memories), memories);

                executable = true;
                IsActive = true;
                return true;
            }
            catch (Exception e)
            {
                LuaCsLogger.HandleException(e, LuaCsMessageOrigin.LuaCs);
                executable = false;
                return false;
            }
        }

        public override void UpdateHeir(float deltaTime, Camera cam)
        {
            if (!executable) { return; }

            for (int i = 0; i < signalCount; i++)
            {
                if (states[i] == -1) { continue; }
                else if (states[i] == 0)
                {
                    states[i] = -1;
                    inputs.Table.Set(i + 1, DynValue.Nil);
                    pendingSenders.Table.Set(i + 1, DynValue.Nil);
                }
                else
                {
                    states[i] -= 1;
                }
            }

            outputs.Table.Clear();

            try
            {
                bool? flippedX = Item.Submarine?.FlippedX;
                func.Call(inputs.DynValue, outputs.DynValue, memories.DynValue,
                    flippedX.HasValue ? DynValue.NewBoolean(flippedX.Value) : DynValue.Nil,
                    environment == luaScript ? DynValue.Nil : pendingSenders.DynValue);

                foreach (var pair in outputs.Table.Pairs)
                {
                    DynValue key = pair.Key;
                    if (key.Type != DataType.Number) { continue; }
                    int lidx = (int)key.Number;
                    if (lidx < 1 || lidx > signalCount || lidx != key.Number) { continue; }
                    DynValue value = pair.Value;
                    string signalValue = null;
                    switch (value.Type)
                    {
                        case DataType.Number: signalValue = value.Number.ToString(CultureInfo.InvariantCulture); break;
                        case DataType.Boolean: signalValue = value.Boolean ? "1" : "0"; break;
                        case DataType.String: signalValue = value.String; break;
                    }
                    if (signalValue is not null)
                    {
                        SendSynchronousSignal(lidx - 1, signalValue, pendingSenders.Table.Get(lidx).ToObject(typeof(Character)) is Character character ? character : null);
                    }
                }
            }
            catch (Exception e)
            {
                LuaCsLogger.LogError(
                    $"Error in {Item}, Submarine:{Item.Submarine?.Info.DisplayName ?? string.Empty}" +
                    $"\r\n, Hull:{Item.CurrentHull?.DisplayName ?? string.Empty}" +
                    $"\r\n, Code: {Chunk}", LuaCsMessageOrigin.LuaMod);
                LuaCsLogger.HandleException(e, LuaCsMessageOrigin.LuaMod);
            }
        }

        public override void ReceiveSignal(Signal signal, Connection connection)
        {
            if (IsMultiplayerClient) { return; }

            if (executable && inputMapLidx.TryGetValue(connection, out int lidx))
            {
                inputs.Table.Set(lidx,
                    !regexInvalidNumberStyle.IsMatch(signal.value)
                        && double.TryParse(signal.value, NumberStyles.AllowDecimalPoint, default(IFormatProvider), out double value)
                            ? DynValue.NewNumber(value)
                            : DynValue.NewString(signal.value));
                pendingSenders.Table[lidx] = signal.sender;
                states[lidx - 1] = 1;
            }
            else
            {
                switch (connection.Name)
                {
                    case "complie":
                        TryComplieChunk();
                        break;
                }
            }
        }
    }
}
