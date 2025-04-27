// Credit: MicroLua (https://steamcommunity.com/sharedfiles/filedetails/?id=3018125421)

using Barotrauma;
using Barotrauma.Items.Components;
using Barotrauma.Networking;
using MoonSharp.Interpreter.Interop;
using MoonSharp.Interpreter;
using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Diagnostics.CodeAnalysis;
using Barotrauma.Extensions;
using System.Linq;
using System.Net.NetworkInformation;
using HarmonyLib;
using MoonSharp.Interpreter.Compatibility;
using MoonSharp.Interpreter.Debugging;
using System.Xml.Linq;

namespace DSSIFactionCraft
{
    partial class DfcLua2Component : ItemComponent
    {
#if SERVER
        public const bool IsServer = true;
        public const bool IsClient = false;
#else
        public const bool IsServer = false;
        public const bool IsClient = true;
#endif

        private const string LOCAL_NAME_IS_CLIENT = "isClient";
        private const string LOCAL_NAME_IS_SERVER = "isServer";
        private const string LOCAL_NAME_IS_SINGLEPLAYER = "isSingleplayer";
        private const string LOCAL_NAME_IS_MULTIPLAYER = "isMultiplayer";
        private const string LOCAL_NAME_GET_TOTAL_TIME = "GetTotalTime";
        private const string LOCAL_NAME_LUA_ITEM = "luaItem";
        private const string LOCAL_NAME_OUT = "out";
        private const string LOCAL_NAME_CLEAR = nameof(clear);
        private const string LOCAL_NAME_SYNC = nameof(sync);
        private const string UPVALUE_NAME_LOADED = nameof(loaded);
        private const string UPVALUE_NAME_UPD = nameof(upd);
        private const string UPVALUE_NAME_INP = nameof(inp);
        private const string UPVALUE_NAME_SENDER = nameof(sender);
        private const string UPVALUE_NAME_SENDERS = nameof(senders);
        private readonly static Regex inpRegex = new Regex(@"^signal_in(\d+)$");
        private readonly static Regex outRegex = new Regex(@"^signal_out(\d+)$");

        private ButtonTerminal? networkComponent;
        private SerializableProperty? signals;
        private int signalsLength;
        private ButtonTerminal.EventData[]? syncEventDatas;
        private Item.ChangePropertyEventData signalsChangePropertyEventData;

        private Dictionary<int, Connection> outPinMappingConnection = new();
        private Dictionary<Connection, int> inpConnectionMappingPin = new();

        private string chunk = string.Empty;
        private Script? script;
        private DynValue loaded = DynValue.Nil;
        private DynValue upd = DynValue.Nil;
        private DynValue inp = DynValue.Nil;
        private DynValue sender = DynValue.Nil;
        private DynValue senders = DynValue.Nil;

        private void HandleException(string source, InterpreterException e, bool terminate = false)
        {
            void LogError(string? location = null, string? lines = null)
            {
                string message = location is null
                    ? $"[{Item}] [{source}] {e.Message}"
                    : lines is null
                        ? $"[{Item}] [{source}] {e.Message}\n{new string(' ', 4)}at {location}"
                        : $"[{Item}] [{source}] {e.Message}\n{new string(' ', 4)}at {location}, source:\n{lines}";

                LuaCsLogger.LogError(message, LuaCsMessageOrigin.LuaMod);
            }

            if (script is not null
                && Traverse.Create(script).Field("m_MainProcessor").GetValue<MoonSharp.Interpreter.Execution.VM.Processor>() is var scriptMainProcessor
                && scriptMainProcessor is not null
                && scriptMainProcessor.GetCurrentSourceRef(e.InstructionPtr) is var sref
                && sref is not null)
            {
                int chunkRelativeToCodeStartLineOffset = 16;
                int chunkRelativeToCodeEndLineOffset = 9;
                string? location;
                string? errorSource;
                SourceCode sc = script.GetSourceCode(sref.SourceIdx);

                string GetLinesToPrint(int fromLine, int toLine, int extendedLines = 5)
                {
                    int extendedForwardLines = extendedLines / 2;
                    int extendedBackwardLines = extendedLines - extendedForwardLines;
                    int discardLines = Math.Max(1 + chunkRelativeToCodeStartLineOffset - (sref.FromLine - extendedBackwardLines), 0);
                    extendedBackwardLines -= discardLines;
                    extendedForwardLines += discardLines;
                    int startLine = sref.FromLine - extendedBackwardLines;
                    int endLine = Math.Min(sref.ToLine + extendedForwardLines, (sc.Lines.Length - 1) - chunkRelativeToCodeEndLineOffset); // Line 0 of sc.Lines is an auto-generated comment, so we need to account for the extra line in sc.Lines.
                    int lineColumnWidth = Math.Max(
                        (startLine - chunkRelativeToCodeStartLineOffset).ToString().Length,
                        (endLine - chunkRelativeToCodeStartLineOffset).ToString().Length);
                    return string.Join(
                        '\n',
                        Enumerable.Range(startLine, endLine - startLine + 1)
                            .Select(line => $"{(line - chunkRelativeToCodeStartLineOffset).ToString().PadLeft(lineColumnWidth, '0')}.{new string(' ', 4)}{sc.Lines[line]}")
                    );
                }

                if (sref.IsClrLocation)
                {
                    location = "[clr]";
                    errorSource = null;
                }
                else if (sref.FromLine == sref.ToLine)
                {
                    if (sref.FromChar == sref.ToChar)
                    {
                        location = $"{sc.Name}:({sref.FromLine - chunkRelativeToCodeStartLineOffset},{sref.FromChar})";
                    }
                    else
                    {
                        location = $"{sc.Name}:({sref.FromLine - chunkRelativeToCodeStartLineOffset},{sref.FromChar}-{sref.ToChar})";
                    }
                    errorSource = GetLinesToPrint(sref.FromLine, sref.ToLine);
                }
                else
                {
                    location = $"{sc.Name}:({sref.FromLine - chunkRelativeToCodeStartLineOffset},{sref.FromChar}-{sref.ToLine - chunkRelativeToCodeStartLineOffset},{sref.ToChar})";
                    errorSource = GetLinesToPrint(sref.FromLine, sref.ToLine);
                }
                LogError(location, errorSource);
            }
            else
            {
                LogError();
            }

            if (terminate) { Terminate(); }
        }

        private void Terminate()
        {
            script = null;
            loaded = DynValue.Nil;
            upd = DynValue.Nil;
            inp = DynValue.Nil;
            sender = DynValue.Nil;
            senders = DynValue.Nil;
        }

        public override bool UpdateWhenInactive => true;

        [InGameEditable, Serialize(true, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public bool CompileInSingleplayer { get; set; }

        [InGameEditable, Serialize(false, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public bool CompileOnClientInMultiplayer { get; set; }

        [InGameEditable, Serialize(true, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public bool CompileOnServerInMultiplayer { get; set; }

        private bool AllowCompile => GameMain.IsSingleplayer
            ? CompileInSingleplayer
            : GameMain.NetworkMember.IsClient
                ? CompileOnClientInMultiplayer
                : CompileOnServerInMultiplayer;

        [Serialize(false, IsPropertySaveable.Yes, description: "Can the properties of the component be edited in-game in multiplayer. Use to prevent clients uploading a malicious code to the server."), Editable()]
        public bool AllowInGameEditingInMultiplayer
        {
            get;
            set;
        }

        [InGameEditable, Serialize("", IsPropertySaveable.Yes, description: "A lua chunk to be complied.", alwaysUseInstanceValues: true)]
        public string Chunk
        {
            get => chunk;
            set
            {
                if (chunk == value) { return; }
                Terminate();
                chunk = value;
                if (chunk.IsNullOrEmpty()) { return; }

#if CLIENT
                if (SubEditorScreen.IsSubEditor()) { return; }
#endif

                if (!AllowCompile) { return; }

                try
                {
                    script = GameMain.LuaCs.Lua;

                    var initialize = script.DoString($@"
return function(_)
    local {LOCAL_NAME_IS_CLIENT} = _.{LOCAL_NAME_IS_CLIENT}
    local {LOCAL_NAME_IS_SERVER} = _.{LOCAL_NAME_IS_SERVER}
    local {LOCAL_NAME_IS_SINGLEPLAYER} = _.{LOCAL_NAME_IS_SINGLEPLAYER}
    local {LOCAL_NAME_IS_MULTIPLAYER} = _.{LOCAL_NAME_IS_MULTIPLAYER}
    local {LOCAL_NAME_GET_TOTAL_TIME} = _.{LOCAL_NAME_GET_TOTAL_TIME}
    local {LOCAL_NAME_LUA_ITEM} = _.{LOCAL_NAME_LUA_ITEM}
    local {LOCAL_NAME_OUT} = _.{LOCAL_NAME_OUT}
    local {LOCAL_NAME_CLEAR} = _.{LOCAL_NAME_CLEAR}
    local {LOCAL_NAME_SYNC} = _.{LOCAL_NAME_SYNC}
    local {UPVALUE_NAME_LOADED}
    local {UPVALUE_NAME_UPD}
    local {UPVALUE_NAME_INP}
    local {UPVALUE_NAME_SENDER}
    local {UPVALUE_NAME_SENDERS}
{chunk}
    return function()
        return
            {UPVALUE_NAME_LOADED},
            {UPVALUE_NAME_UPD},
            {UPVALUE_NAME_INP},
            {UPVALUE_NAME_SENDER},
            {UPVALUE_NAME_SENDERS}
    end
end", codeFriendlyName: null);

                    var args = new Table(script);
                    args[LOCAL_NAME_IS_CLIENT] = IsClient;
                    args[LOCAL_NAME_IS_SERVER] = IsServer;
                    args[LOCAL_NAME_IS_SINGLEPLAYER] = GameMain.IsSingleplayer;
                    args[LOCAL_NAME_IS_MULTIPLAYER] = GameMain.IsMultiplayer;
                    args[LOCAL_NAME_GET_TOTAL_TIME] = DynValue.NewCallback((ctx, args) => DynValue.NewNumber(Timing.TotalTime));
                    args[LOCAL_NAME_LUA_ITEM] = this.item;
                    args[LOCAL_NAME_OUT] = UserData.Create(this, new OutDescriptor());
                    args[LOCAL_NAME_CLEAR] = DynValue.NewCallback(clear);
                    args[LOCAL_NAME_SYNC] = DynValue.NewCallback(sync);
                    var internalClosure = script.Call(initialize, DynValue.NewTable(args)).Function;

                    Dictionary<string, DynValue> nameMapUpvalue = new(
                        Enumerable.Range(0, internalClosure.GetUpvaluesCount())
                        .Select(i =>
                            KeyValuePair.Create(internalClosure.GetUpvalueName(i), internalClosure.GetUpvalue(i))
                        )
                    );
                    loaded = nameMapUpvalue[UPVALUE_NAME_LOADED];
                    upd = nameMapUpvalue[UPVALUE_NAME_UPD];
                    inp = nameMapUpvalue[UPVALUE_NAME_INP];
                    sender = nameMapUpvalue[UPVALUE_NAME_SENDER];
                    senders = nameMapUpvalue[UPVALUE_NAME_SENDERS];
                }
                catch (SyntaxErrorException e)
                {
                    HandleException("compile", e, terminate: true);
                }
                catch (ScriptRuntimeException e)
                {
                    HandleException("compile", e, terminate: true);
                }
            }
        }

        static DfcLua2Component()
        {
            if (!UserData.IsTypeRegistered<DfcLua2Component>())
            {
                UserData.RegisterType<DfcLua2Component>();
            }
        }

        public DfcLua2Component(Item item, ContentXElement element) : base(item, element) { }

        public class OutDescriptor : IUserDataDescriptor
        {
            public string Name => "out";

            public Type Type => typeof(DfcLua2Component);

            public DynValue Index(Script script, object obj, DynValue index, bool isDirectIndexing)
            {
                throw new ScriptRuntimeException("__index metamethod not implemented");
            }

            public bool SetIndex(Script script, object obj, DynValue index, DynValue value, bool isDirectIndexing)
            {
                DfcLua2Component? luaComponent = obj as DfcLua2Component;

                if (luaComponent is null)
                {
                    throw new ScriptRuntimeException("unknown behavior!");
                }

                if (luaComponent.script is null)
                {
                    throw new ScriptRuntimeException("out is prohibited when no script!");
                }

                if (index.Type != DataType.Number)
                {
                    throw new ScriptRuntimeException($"pin must be an 'integer', but got '{index.Type}'!");
                }

                if (index.Number != Math.Truncate(index.Number))
                {
                    throw new ScriptRuntimeException($"pin must be an 'integer', but got '{index.Number}'!");
                }

                int pin = (int)index.Number;

                if (!luaComponent.outPinMappingConnection.TryGetValue(pin, out var connection))
                {
                    throw new ScriptRuntimeException($"invalid pin {pin}!");
                }

                luaComponent.item.SendSignal(new Signal(value.Type switch
                {
                    DataType.Number => value.Number.ToString(CultureInfo.InvariantCulture),
                    DataType.String => value.String,
                    _ => throw new ScriptRuntimeException($"pin {pin} outputs an invalid value type '{value.Type}'!")
                }, sender: luaComponent.sender.IsNotNil() ? luaComponent.sender.ToObject<Character>() : null), connection);

                return true;
            }

            public string AsString(object obj)
            {
                return nameof(DfcLua2Component);
            }

            public DynValue MetaIndex(Script script, object obj, string metaname)
            {
                throw new ScriptRuntimeException($"{metaname} metamethod not implemented");
            }

            public bool IsTypeCompatible(Type type, object obj)
            {
                return Framework.Do.IsInstanceOfType(type, obj);
            }
        }

        private static DynValue clear(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            args.AsType(0, nameof(clear), DataType.Table, false).Table.Clear();
            return DynValue.Nil;
        }

        private DynValue sync(ScriptExecutionContext executionContext, CallbackArguments args)
        {
#if SERVER
            if (networkComponent is null || syncEventDatas is null) { return DynValue.Nil; }

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

                if (value.Type != DataType.Nil)
                {
                    item.CreateServerEvent(networkComponent, syncEventDatas[i]);
                }
            }
#endif
            return DynValue.Nil;
        }

        public override void OnItemLoaded()
        {
            base.OnItemLoaded();

            if (GameMain.IsMultiplayer && !AllowInGameEditingInMultiplayer)
            {
                AllowInGameEditing = false;
            }

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
                    outPinMappingConnection.Add(int.Parse(outMatch.Groups[1].Value), connection);
                }
                else if (inpRegex.Match(connection.Name) is { Success: true } inpMatch)
                {
                    inpConnectionMappingPin.Add(connection, int.Parse(inpMatch.Groups[1].Value));
                }
            }
        }

        public override void OnMapLoaded()
        {
            base.OnMapLoaded();

            if (script is null || loaded.Type != DataType.Function) { return; }

            try
            {
                script.Call(loaded);
            }
            catch (ScriptRuntimeException e)
            {
                HandleException(nameof(loaded), e, terminate: true);
            }
        }

        public override void Update(float deltaTime, Camera cam)
        {
            if (script is null || upd.Type != DataType.Function) { return; }

            try
            {
                script.Call(upd, DynValue.NewNumber(deltaTime));
            }
            catch (ScriptRuntimeException e)
            {
                HandleException(nameof(upd), e, terminate: true);
            }
        }

        public override void ReceiveSignal(Signal signal, Connection connection)
        {
            if (script is null || inp.Type == DataType.Nil) { return; }

            if (inpConnectionMappingPin.TryGetValue(connection, out int pin))
            {
                if (senders.Type == DataType.Table && signal.sender is Character signalSender)
                {
                    senders.Table.Set(pin, DynValue.FromObject(script, signalSender));
                }

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
                        try
                        {
                            script.Call(inp, DynValue.NewNumber(pin), value);
                        }
                        catch (ScriptRuntimeException e)
                        {
                            HandleException(nameof(inp), e, terminate: true);
                        }
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
