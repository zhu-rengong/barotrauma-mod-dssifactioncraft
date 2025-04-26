
using Barotrauma;
using Barotrauma.Networking;
using System;
using System.Linq;
using System.Collections.Generic;
using Barotrauma.Items.Components;
using HarmonyLib;
using MoonSharp.Interpreter;
using static Barotrauma.Networking.RespawnManager;
using System.Collections.Immutable;
using FarseerPhysics.Collision;
using Barotrauma.Extensions;

namespace DSSIFactionCraft.Networking
{
    public class DfcRespawnManager
    {
        public class FactionSpecificState
        {
            public readonly string FactionIdentifier;
            public DateTime RespawnTime;
            public bool RespawnCountdownStarted;

            public bool AllowRespawn => DfcModule.Factions is DynValue { Type: DataType.Table } factions
                && factions.Table.RawGet(FactionIdentifier) is DynValue { Type: DataType.Table } faction
                && faction.Table.RawGet("allowRespawn") is DynValue { Type: DataType.Boolean, Boolean: true };

            public float RespawnIntervalMultiplier => (DfcModule.Factions is DynValue { Type: DataType.Table } factions
                    && factions.Table.RawGet(FactionIdentifier) is DynValue { Type: DataType.Table } faction
                    && faction.Table.RawGet("respawnIntervalMultiplier") is DynValue { Type: DataType.Number } respawnIntervalMultiplier
                ) ? Convert.ToSingle(respawnIntervalMultiplier.Number) : 1.0f;

            public Closure? GetRespawnLimitPerTime => DfcModule.Factions is DynValue { Type: DataType.Table } factions
                    && factions.Table.RawGet(FactionIdentifier) is DynValue { Type: DataType.Table } faction
                    && faction.Table.Get("getRespawnLimitPerTime") is DynValue { Type: DataType.Function } getRespawnLimitPerTime
                        ? getRespawnLimitPerTime.Function
                        : null;

            public int NumberOfParticipatorsByKeyEvenIfNil => DfcModule.Factions is DynValue { Type: DataType.Table } factions
                    && factions.Table.RawGet(FactionIdentifier) is DynValue { Type: DataType.Table } faction
                    && faction.Table.Get("tryGetParticipatorsByKeyEvenIfNil") is DynValue { Type: DataType.Function } tryGetParticipatorsByKeyEvenIfNil
                    && tryGetParticipatorsByKeyEvenIfNil.Function.Call(faction) is DynValue { Type: DataType.Table } participators
                        ? participators.Table.Length
                        : -1;

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

            if (DfcModule.Factions is DynValue { Type: DataType.Table } factions)
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
                && DfcModule.DeathTime.Table is Table deathTimeTable
                && DfcModule.JoinedFaction.Table is Table joinedFactionTable)
            {
                foreach (var factionSpecificState in factionSpecificStates.Values)
                {
                    if (!factionSpecificState.AllowRespawn) { continue; }

                    bool shouldStartCountdown = deathTimeTable.Pairs.Any(HasClientDeathTimeInFactionSpecific);

                    bool HasClientDeathTimeInFactionSpecific(TablePair deathTimePair)
                    {
                        return deathTimePair.Key is DynValue { Type: DataType.String } clientAccountId
                            && deathTimePair.Value is DynValue { Type: DataType.Number } deathTime
                            && joinedFactionTable.RawGet(clientAccountId.String) is DynValue { Type: DataType.Table } faction
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
                                factionSpecificState.RespawnTime = DateTime.Now + new TimeSpan(0, 0, 0, 0, (int)(GameMain.Server.ServerSettings.RespawnInterval * 1000.0f * factionSpecificState.RespawnIntervalMultiplier));

                                float timeLeft = MathF.Ceiling((float)(factionSpecificState.RespawnTime - DateTime.Now).TotalSeconds);

                                if (timeLeft > 0.0f)
                                {
                                    LocalizedString respawnText = TextManager.GetWithVariables(
                                        "dfc.respawningin",
                                        ("[faction]", GetFactionDisplayName()),
                                        ("[time]", ToolBox.SecondsToReadableTime(timeLeft)));
                                    GameMain.Server.SendChatMessage(respawnText.Value, ChatMessageType.Server);
                                }
                            }

                        }
                    }

                    if (factionSpecificState.RespawnCountdownStarted && DateTime.Now > factionSpecificState.RespawnTime)
                    {
                        factionSpecificState.RespawnCountdownStarted = false;

                        bool respawned = false;

                        void TryRespawn(TablePair deathTimePair)
                        {
                            if (HasClientDeathTimeInFactionSpecific(deathTimePair))
                            {
                                deathTimeTable.Set(deathTimePair.Key, DynValue.Nil);
                                respawned = true;
                            }
                        }


                        if (factionSpecificState.GetRespawnLimitPerTime is Closure getRespawnLimitPerTime
                            && getRespawnLimitPerTime.Call(
                                factionSpecificState.FactionIdentifier, // identifier: string
                                deathTimeTable.Pairs.Count(HasClientDeathTimeInFactionSpecific), // dead: integer
                                factionSpecificState.NumberOfParticipatorsByKeyEvenIfNil // total: integer
                            ) is DynValue { Type: DataType.Number } respawnLimitPerTime
                            && Math.Truncate(respawnLimitPerTime.Number) == respawnLimitPerTime.Number // is integer
                            && Convert.ToInt32(respawnLimitPerTime.Number) is int limit
                            && limit > -1)
                        {
                            TablePair[] pairs = deathTimeTable.Pairs.ToArray();
                            Array.Sort(pairs, (p1, p2) =>
                            {
                                if (deathTimeTable.RawGet(p1.Key) is DynValue { Type: DataType.Number } dt1
                                    && deathTimeTable.RawGet(p2.Key) is DynValue { Type: DataType.Number } dt2)
                                {
                                    dt1.Number.CompareTo(dt2.Number);
                                }

                                return 0;
                            });

                            pairs.Take(limit).ForEach(TryRespawn);
                        }
                        else
                        {
                            deathTimeTable.Pairs.ForEach(TryRespawn);
                        }

                        if (respawned)
                        {
                            LocalizedString respawnText = TextManager.GetWithVariable("dfc.respawned", "[faction]", GetFactionDisplayName());
                            GameMain.Server.SendChatMessage(respawnText.Value, ChatMessageType.Server);
                        }
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
