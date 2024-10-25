
using Barotrauma;
using Barotrauma.Networking;
using System;
using Barotrauma.Items.Components;
using HarmonyLib;
using MoonSharp.Interpreter;
using static Barotrauma.Networking.RespawnManager;
using System.Linq;

namespace DSSIFactionCraft.Networking
{
    public class DfcRespawnManager
    {
        public class FactionSpecificState
        {
            public readonly string FactionIdentifier;
            public DateTime RespawnTime;
            public bool RespawnCountdownStarted;
            public DynValue Table;

            public FactionSpecificState(string factionIdentifier)
            {
                FactionIdentifier = factionIdentifier;
            }
        }

        public readonly Dictionary<string, FactionSpecificState> factionSpecificStates = new Dictionary<string, FactionSpecificState>();

        public DfcRespawnManager() { }

        public void Update()
        {
            if (GameMain.GameSession is { RoundDuration: < 3.0f }) { return; }

            if (DFCModule.Factions is DynValue { Type: DataType.Table } factions)
            {
                foreach (var dynValueFactionIdentifier in factions.Table.Keys)
                {
                    if (dynValueFactionIdentifier is DynValue { Type: DataType.String })
                    {
                        string factionIdentifier = dynValueFactionIdentifier.String;
                        if (!factionSpecificStates.ContainsKey(factionIdentifier))
                        {
                            factionSpecificStates.Add(factionIdentifier, new FactionSpecificState(factionIdentifier));
                        }
                    }
                }
            }

            if (factionSpecificStates.Any()
                && DFCModule.WaitRespawn is DynValue { Type: DataType.Table }
                && DFCModule.JoinedFaction is DynValue { Type: DataType.Table })
            {
                foreach (var factionSpecificState in factionSpecificStates.Values)
                {
                    bool shouldStartCountdown = DFCModule.WaitRespawn.Table.Pairs.Any(IsClientWaitingForRespawnInFactionSpecific);

                    bool IsClientWaitingForRespawnInFactionSpecific(TablePair waitRespawnPair)
                    {
                        return waitRespawnPair.Key is DynValue { Type: DataType.String } clientAccountId
                            && waitRespawnPair.Value is DynValue { Type: DataType.Boolean, Boolean: true } isWaiting
                            && DFCModule.JoinedFaction.Table.RawGet(clientAccountId.String) is DynValue { Type: DataType.Table } faction
                            && faction.Table.RawGet("identifier") is DynValue { Type: DataType.String } dynValueFactionIdentifier
                            && dynValueFactionIdentifier.String == factionSpecificState.FactionIdentifier;
                    };

                    if (factionSpecificState.RespawnCountdownStarted)
                    {
                        if (!shouldStartCountdown)
                        {
                            factionSpecificState.RespawnCountdownStarted = false;
                        }
                    }
                    else
                    {
                        if (shouldStartCountdown)
                        {
                            factionSpecificState.RespawnCountdownStarted = true;
                            if (factionSpecificState.RespawnTime < DateTime.Now)
                            {
                                factionSpecificState.RespawnTime = DateTime.Now + new TimeSpan(0, 0, 0, 0, (int)(GameMain.Server.ServerSettings.RespawnInterval * 1000.0f));

                                float timeLeft = (float)(factionSpecificState.RespawnTime - DateTime.Now).TotalSeconds;
                                LocalizedString respawnText = TextManager.GetWithVariables(
                                    "dfc.respawningin",
                                    ("[faction]", GetFactionDisplayName()),
                                    ("[time]", ToolBox.SecondsToReadableTime(timeLeft)));
                                GameMain.Server.SendChatMessage(respawnText.Value, ChatMessageType.Server);
                            }

                        }
                    }

                    if (factionSpecificState.RespawnCountdownStarted && DateTime.Now > factionSpecificState.RespawnTime)
                    {
                        factionSpecificState.RespawnCountdownStarted = false;

                        foreach (var waitRespawnPair in DFCModule.WaitRespawn.Table.Pairs)
                        {
                            if (IsClientWaitingForRespawnInFactionSpecific(waitRespawnPair))
                            {
                                DFCModule.WaitRespawn.Table.Set(waitRespawnPair.Key, DynValue.Nil);
                            }
                        }

                        LocalizedString respawnText = TextManager.GetWithVariable("dfc.respawned", "[faction]", GetFactionDisplayName());
                        GameMain.Server.SendChatMessage(respawnText.Value, ChatMessageType.Server);
                    }

                    string GetFactionDisplayName()
                    {
                        if (LuaCsInterop.GetLocalizedText is Closure getLocalizedText
                            && getLocalizedText.Call(
                                DynValue.Nil,
                                DynValue.NewTable(
                                    getLocalizedText.OwnerScript,
                                    DynValue.NewString("FactionDisplayName"),
                                    DynValue.NewString(factionSpecificState.FactionIdentifier)
                                )) is DynValue { Type: DataType.Table } result
                            && result.Table.RawGet("altvalue") is DynValue { Type: DataType.String } factionDisplayName)
                        {
                            return factionDisplayName.String;
                        }

                        return factionSpecificState.FactionIdentifier;
                    }
                }
            }
        }
    }
}
