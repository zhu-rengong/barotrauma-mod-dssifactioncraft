// SPDX-FileCopyrightText: 2023 Matheus Izvekov <mizvekov@gmail.com>
// SPDX-License-Identifier: ISC

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

namespace DSSIFactionCraft
{
    partial class DfcLua2Component : ItemComponent
    {
        private const string PARAMETER_NAME_OUT = "out";
        private const string PARAMETER_NAME_LUA_ITEM = "luaItem";
        private const string PARAMETER_NAME_CLEAR = nameof(clear);
        private const string PARAMETER_NAME_SYNC = nameof(sync);
        private const string UPVALUE_NAME_UPD = nameof(upd);
        private const string UPVALUE_NAME_INP = nameof(inp);
        private readonly static Regex inpRegex = new Regex(@"^signal_in(\d+)$");
        private readonly static Regex outRegex = new Regex(@"^signal_out(\d+)$");

        private ButtonTerminal networkComponent;
        private SerializableProperty signals;
        private int signalsLength;
        private ButtonTerminal.EventData[] syncEventDatas;
        private Item.ChangePropertyEventData signalsChangePropertyEventData;

        private Dictionary<int, Connection> outPinMapConnection = new();
        private Dictionary<Connection, int> inpConnectionMapPin = new();

        private Script? script;
        private DynValue? upd;
        private DynValue? inp;

        private void HandleException(string source, InterpreterException e, bool stop = false)
        {
            LuaCsLogger.LogError($"[{Item}] [{source}] {e.DecoratedMessage}", LuaCsMessageOrigin.LuaMod);
            if (stop) { Stop(); }
        }

        private void Stop()
        {
            script = null;
            upd = null;
            inp = null;
            IsActive = false;
        }

        private string chunk = string.Empty;
        [InGameEditable, Serialize("", IsPropertySaveable.Yes, description: "A lua chunk to be complied.", alwaysUseInstanceValues: true)]
        public string Chunk
        {
            get => chunk;
            set
            {
                if (chunk == value) { return; }
                chunk = value;

                if (GameMain.NetworkMember?.IsClient ?? false) { return; }
#if CLIENT
                if (SubEditorScreen.IsSubEditor()) { return; }
#endif
                Stop();

                if (chunk.IsNullOrEmpty()) { return; }

                script = GameMain.LuaCs.Lua;

                try
                {
                    var externalFunction = script.DoString($@"
return function({PARAMETER_NAME_LUA_ITEM}, {PARAMETER_NAME_OUT}, {PARAMETER_NAME_CLEAR}, {PARAMETER_NAME_SYNC}) local {UPVALUE_NAME_UPD}, {UPVALUE_NAME_INP}
{chunk}
return function() return {UPVALUE_NAME_UPD}, {UPVALUE_NAME_INP} end
end", codeFriendlyName: null);

                    var outUserData = UserData.Create(new Out(this));
                    var internalClosure = script.Call(externalFunction,
                        item,
                        outUserData,
                        DynValue.NewCallback(clear),
                        DynValue.NewCallback(sync)).Function;

                    Dictionary<string, DynValue> nameMapUpvalue = new(
                        Enumerable.Range(0, internalClosure.GetUpvaluesCount())
                        .Select(i =>
                            KeyValuePair.Create(internalClosure.GetUpvalueName(i), internalClosure.GetUpvalue(i))
                        )
                    );
                    upd = nameMapUpvalue[UPVALUE_NAME_UPD];
                    inp = nameMapUpvalue[UPVALUE_NAME_INP];

                    if (upd.Type == DataType.Function)
                    {
                        IsActive = true;
                    }
                }
                catch (SyntaxErrorException e)
                {
                    HandleException("syntax", e, stop: true);
                }
                catch (ScriptRuntimeException e)
                {
                    HandleException("runtime", e, stop: true);
                }
            }
        }

        static DfcLua2Component()
        {
            UserData.RegisterType<Out>(InteropAccessMode.NoReflectionAllowed, "out");
        }

        public DfcLua2Component(Item item, ContentXElement element) : base(item, element)
        {

        }

        class Out : IUserDataType
        {
            private Item item;
            private Dictionary<int, Connection> outPinMapConnection;

            public Out([DisallowNull] DfcLua2Component luaComponent)
            {
                item = luaComponent.Item;
                outPinMapConnection = luaComponent.outPinMapConnection;
            }

            public DynValue Index(Script script, DynValue index, bool isDirectIndexing)
            {
                throw new ScriptRuntimeException("__index metamethod not implemented");
            }

            public bool SetIndex(Script script, DynValue index, DynValue value, bool isDirectIndexing)
            {
                if (index.Type != DataType.Number)
                {
                    throw new ScriptRuntimeException($"pin must be an 'integer', but got '{index.Type}'!");
                }

                var indexNumber = index.Number;
                if (indexNumber % 1 != 0)
                {
                    throw new ScriptRuntimeException($"pin must be an 'integer', but got '{indexNumber}'!");
                }

                var pin = (int)indexNumber;

                var connection = outPinMapConnection[pin] ?? throw new ScriptRuntimeException($"invalid pin {pin}!");
                item.SendSignal(new Signal(value.Type switch
                {
                    DataType.Number => value.Number.ToString(CultureInfo.InvariantCulture),
                    DataType.String => value.String,
                    _ => throw new ScriptRuntimeException($"pin {pin} outputs an invalid value type '{value.Type}'!")
                }), connection);

                return true;
            }

            public DynValue MetaIndex(Script script, string metaname)
            {
                throw new ScriptRuntimeException($"{metaname} metamethod not implemented");
            }
        };

        private static DynValue clear(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            args.AsType(0, nameof(clear), DataType.Table, false).Table.Clear();
            return DynValue.Nil;
        }

        private DynValue sync(ScriptExecutionContext executionContext, CallbackArguments args)
        {
#if SERVER
            Table syncTable = args.AsType(0, nameof(sync), DataType.Table, false).Table;
            GameMain.NetworkMember.CreateEntityEvent(item, signalsChangePropertyEventData);
            for (int i = 0; i < signalsLength; i++)
            {
                int pin = i + 1;
                var value = syncTable.Get(pin);
                networkComponent.Signals[i] = value.Type switch
                {
                    DataType.Nil => string.Empty,
                    DataType.Number => value.Number.ToString(CultureInfo.InvariantCulture),
                    DataType.String => value.String,
                    _ => throw new ScriptRuntimeException($"sync pin {pin} with an invalid value type '{value.Type}'!")
                };
                item.CreateServerEvent(networkComponent, syncEventDatas[i]);
            }
#endif
            return DynValue.Nil;
        }

        public override void OnItemLoaded()
        {
            base.OnItemLoaded();

            networkComponent = item.GetComponent<ButtonTerminal>();
            signals = networkComponent.SerializableProperties[nameof(ButtonTerminal.Signals).ToIdentifier()];
            signalsLength = networkComponent.Signals.Length;
            syncEventDatas = Enumerable.Range(0, signalsLength)
                .Select(i => new ButtonTerminal.EventData(i))
                .ToArray();
            signalsChangePropertyEventData = new(signals, networkComponent);

            foreach (var connection in item.Connections)
            {
                if (outRegex.Match(connection.Name) is { Success: true } outMatch)
                {
                    outPinMapConnection.Add(int.Parse(outMatch.Groups[1].Value), connection);
                }
                else if (inpRegex.Match(connection.Name) is { Success: true } inpMatch)
                {
                    inpConnectionMapPin.Add(connection, int.Parse(inpMatch.Groups[1].Value));
                }
            }
        }

        public override void Update(float deltaTime, Camera cam)
        {
            try { script.Call(upd, DynValue.NewNumber(deltaTime)); }
            catch (ScriptRuntimeException e) { HandleException("upd", e, stop: true); }
        }

        public override void ReceiveSignal(Signal signal, Connection connection)
        {
            if (script == null) { return; }

            if (inpConnectionMapPin.TryGetValue(connection, out int pin))
            {
                var value = double.TryParse(
                    signal.value,
                    NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture,
                    out double num
                ) ? DynValue.NewNumber(num) : DynValue.NewString(signal.value);

                switch (inp.Type)
                {
                    case DataType.Table:
                        inp.Table.Set(pin, value);
                        break;
                    case DataType.Function:
                        try { script.Call(inp, DynValue.NewNumber(pin), value); }
                        catch (ScriptRuntimeException e) { HandleException("inp", e, stop: true); }
                        break;
                    default:
                        break;
                }
            }

        }


    }
}
