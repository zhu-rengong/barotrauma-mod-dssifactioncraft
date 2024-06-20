local log = require "utilbelt.logger" ("DFC")
local l10n = require "utilbelt.l10n"
local chat = require "utilbelt.chat"
local dialog = require "utilbelt.dialog"
local itbu = require "utilbelt.itbu"
local utils = require "utilbelt.csharpmodule.Shared.Utils"

local UniversalFactionParticipateKey = '_'

---@class dfc.inner
---@field dfc dfc

---@class dfc
---@field itemBuilders { [string]:itembuilder }
---@field spawnPointSets { [string]:dfc.spawnpointset }
---@field shouldSortFactions boolean
---@field factions { [string]:dfc.faction }
---@field sortedFactions dfc.faction[]
---@field existAnyFaction boolean
---@field jobs { [string]:dfc.job }
---@field gears { [string]:dfc.gear }
---@field _firstPlayerSteamIDs { [string]:boolean }
---@field _originalSpectateOnly { [string]:boolean }
---@field _remainingLives { [string]:integer }
---@field _joinedFaction { [string|unknown]:dfc.faction }
---@field _promptedJoiningFaction { [string]:boolean }
---@field _assignedJob { [string]:dfc.job }
---@field _waitRespawn { [string]:boolean }
---@field _promptedAssigningJob { [string]:boolean }
---@field _characterFaction { [Barotrauma.Character]:dfc.faction }
---@field _characterJob { [Barotrauma.Character]:dfc.job }
---@field _chosenGear { [Barotrauma.Character]:dfc.gear }
---@field _promptedChoosingGear { [Barotrauma.Character]:boolean }
---@field _characterWaitChooseGearBy { [string]:Barotrauma.Character }
---@field allowMidRoundJoin boolean
---@field allowRespawn? boolean
---@overload fun():self
local m = Class 'dfc'

function m:__init()
    self.itemBuilders = {}
    self.spawnPointSets = {}
    self.factions = {}
    self.jobs = {}
    self.gears = {}
    self:resetRoundDatas()
    self.allowMidRoundJoin = true
end

function m:resetRoundDatas()
    self._firstPlayerSteamIDs = {}
    self._originalSpectateOnly = {}
    self._remainingLives = {}
    self._joinedFaction = {}
    self._promptedJoiningFaction = {}
    self._assignedJob = {}
    self._waitRespawn = {}
    self._promptedAssigningJob = {}
    self._characterFaction = {}
    self._characterJob = {}
    self._chosenGear = {}
    self._promptedChoosingGear = {}
    self._characterWaitChooseGearBy = {}
end

---@param path string
---@param recursive? boolean
function m:runFolder(path, recursive)
    for _, file in ipairs(File.GetFiles(path)) do
        if file:endsWith('.lua') then
            local ret = loadfile(file)
            if type(ret) == "function" then
                ret(self)
            else
                log(("Failed to run file (path '%s') with an error message: %s"):format(file, ret), 'e')
            end
        end
    end
    if recursive then
        for _, directory in ipairs(File.GetDirectories(path)) do
            self:runFolder(directory, true)
        end
    end
end

---@param identifier string
---@param itembuilds itembuilderblock|itembuilderblock[]
function m:newItemBuilder(identifier, itembuilds)
    self.itemBuilders[identifier] = itbu(itembuilds)
end

---@param identifier string
---@return itembuilder
function m:getItemBuilder(identifier)
    return self.itemBuilders[identifier]
end

---@param identifier string
---@param tag? string
---@param spawnType? Barotrauma.SpawnType
---@param assignedJob? string
---@param teamID? Barotrauma.CharacterTeamType
function m:newSpawnPointSet(identifier, tag, spawnType, assignedJob, teamID)
    local spawnPointSet = New 'dfc.spawnpointset' (identifier, tag, spawnType, assignedJob, teamID)
    self.spawnPointSets[identifier] = spawnPointSet
    spawnPointSet.dfc = self
    return spawnPointSet
end

---@param identifier string
---@param teamID Barotrauma.CharacterTeamType
---@param maxLives? integer
---@param onJoined? fun(character:Barotrauma.Character)
function m:newFaction(identifier, teamID, maxLives, onJoined)
    local faction = New 'dfc.faction' (identifier, teamID, maxLives, onJoined)
    self.factions[identifier] = faction
    self.existAnyFaction = true
    faction.dfc = self
    self.shouldSortFactions = true
    return faction
end

---@param identifier string
---@param name string
---@param onAssigned? fun(character:Barotrauma.Character)
---@param liveConsumption? integer
function m:newJob(identifier, name, onAssigned, liveConsumption)
    local job = New 'dfc.job' (identifier, name, onAssigned, liveConsumption)
    self.jobs[identifier] = job
    job.dfc = self
    return job
end

---@param identifier string
---@param action? fun(character:Barotrauma.Character)
function m:newGear(identifier, action)
    local gear = New 'dfc.gear' (identifier, action)
    self.gears[identifier] = gear
    gear.dfc = self
    return gear
end

function m:trySortFactions()
    if self.shouldSortFactions then
        local factions = moses.values(self.factions)
        self.sortedFactions = moses.sort(factions, function(f1, f2)
            if f1.sort == f2.sort then
                return f1.identifier < f2.identifier
            else
                return f1.sort < f2.sort
            end
        end)
        self.shouldSortFactions = false
    end
end

function m:initialize()
    DFC.Loaded = self

    if CLIENT then
        chat.addcommand {
            { "!factionlist", "!fl" },
            help = l10n "ChatCommandFactionList".value,
            callback = function(_, args)
                local factionList = moses.keys(self.factions)
                chat.send { moses.concat(factionList, ', '), color = Color.LightGray }
            end
        }

        ---@param identifier string
        ---@param character Barotrauma.Character
        local function joinFaction(identifier, character)
            local faction = self.factions[identifier]
            if faction then
                faction:addCharacterTagsFor(character)
                if faction.onJoined then faction.onJoined(character) end
            end
        end

        chat.addcommand {
            { "!joinfaction", "!jf" },
            help = l10n "ChatCommandJoinFaction".value,
            callback = function(_, args)
                if args[1] and Character.Controlled then
                    joinFaction(args[1], Character.Controlled)
                end
            end
        }

        chat.addcommand {
            { "!joblist", "!jl" },
            help = l10n "ChatCommandJobList".value,
            callback = function(_, args)
                local jobList = moses.keys(self.jobs)
                chat.send { moses.concat(jobList, ', '), color = Color.LightGray }
            end
        }

        ---@param identifier string
        ---@param character Barotrauma.Character
        local function assignJob(identifier, character)
            local job = self.jobs[identifier]
            if job then
                job:addCharacterTagsFor(character)
                if job.onAssigned then job.onAssigned(character) end
                if job.existAnySpawnPointSet then
                    local spawnPointSet = utils.SelectDynValueWeightedRandom(job.spawnPointSets, job.spawnPointSetWeights)
                    spawnPointSet:addCharacterTagsFor(character)
                    local spawnPoint = spawnPointSet:getRandom()
                    if spawnPoint then
                        character.TeleportTo(spawnPoint.WorldPosition)
                    end
                end
            end
        end

        chat.addcommand {
            { "!assignjob", "!aj" },
            help = l10n "ChatCommandAssignJob".value,
            callback = function(_, args)
                if args[1] and Character.Controlled then
                    assignJob(args[1], Character.Controlled)
                end
            end
        }

        chat.addcommand {
            { "!assignjobs", "!ajs" },
            help = l10n "ChatCommandAssignJobs".value,
            callback = function(_, args)
                if Character.Controlled then
                    moses.forEach(self.jobs, function(_, identifier)
                        assignJob(identifier, Character.Controlled)
                    end)
                end
            end
        }

        chat.addcommand {
            { "!gearlist", "!gl" },
            help = l10n "ChatCommandGearList".value,
            callback = function(_, args)
                local gearList = moses.keys(self.gears)
                chat.send { moses.concat(gearList, ', '), color = Color.LightGray }
            end
        }

        ---@param identifier string
        ---@param character Barotrauma.Character
        local function spawnGear(identifier, character)
            local gear = self.gears[identifier]
            if gear then
                gear:addCharacterTagsFor(character)
                if gear.action then gear.action(character) end
            end
        end

        chat.addcommand {
            { "!spawngear", "!sg" },
            help = l10n "ChatCommandSpawnGear".value,
            callback = function(_, args)
                if args[1] and Character.Controlled then
                    spawnGear(args[1], Character.Controlled)
                end
            end
        }

        chat.addcommand {
            { "!spawngears", "!sgs" },
            help = l10n "ChatCommandSpawnGears".value,
            callback = function(_, args)
                if Character.Controlled then
                    moses.forEach(self.gears, function(_, identifier)
                        spawnGear(identifier, Character.Controlled)
                    end)
                end
            end
        }
    end

    if SERVER then
        if self.allowRespawn ~= nil then
            Game.ServerSettings.RespawnMode = self.allowRespawn and 0 or 2
        end

        chat.addcommand({
            { "!fix", "!fixprompt" },
            help = l10n { "ChatCommandFixPrompt" }.value,
            callback = function(client, args)
                local clientSteamID = client.SteamID
                if not self._joinedFaction[clientSteamID] then
                    self._promptedJoiningFaction[clientSteamID] = nil
                end
                if not self._assignedJob[clientSteamID] then
                    self._promptedAssigningJob[clientSteamID] = nil
                end
                local character = self._characterWaitChooseGearBy[clientSteamID]
                if character and not self._chosenGear[character] then
                    self._promptedChoosingGear[character] = nil
                end
            end
        })

        chat.addcommand({
            "!rejoin",
            callback = function(client, args)
                local clientSteamID = client.SteamID
                self._joinedFaction[clientSteamID] = nil
                self._promptedJoiningFaction[clientSteamID] = nil
                self._assignedJob[clientSteamID] = nil
                self._promptedAssigningJob[clientSteamID] = nil
                client.SpectateOnly = false
            end,
            hidden = true,
            permissions = ClientPermissions.All
        })

        Hook.Patch(
            "DFC",
            "Barotrauma.Networking.RespawnManager", "RespawnCharacters",
            {
                "Microsoft.Xna.Framework.Vector2",
                "out System.Boolean",
            }, function(_, ptable)
                moses.clear(self._waitRespawn)
                ptable.PreventExecution = true
            end, Hook.HookMethodType.Before
        )

        moses.forEachi(Client.ClientList, function(client)
            table.insert(self._firstPlayerSteamIDs, client.SteamID)
            self._originalSpectateOnly[client.SteamID] = client.SpectateOnly
            client.SpectateOnly = true
        end)
    end

    DSSI.Hook("roundStart", "DFC", function()
        if SERVER then
            moses.forEach(self._originalSpectateOnly, function(spectateOnly, clientSteamID)
                local client = DFC.GetConnectedClientBySteamID(clientSteamID)
                if client then client.SpectateOnly = spectateOnly end
            end)
        end

        local spawnPointSetNewList = Util.GetItemsById("dfc_newspawnpointset")
        if spawnPointSetNewList then
            for _, spawnPointSetNew in ipairs(spawnPointSetNewList) do
                local parameters = DFC.Components.DfcNewSpawnPointSet.GetParameterTable(spawnPointSetNew)
                if parameters.identifier then
                    local spawnPointSet = self:newSpawnPointSet(parameters.identifier, parameters.tag, parameters.spawnType, parameters.assignedJob, parameters.teamID)
                    spawnPointSet.characterTags = parameters.characterTags
                end
            end
        end

        local gearNewList = Util.GetItemsById("dfc_newgear")
        if gearNewList then
            for _, gearNew in ipairs(gearNewList) do
                local parameters = DFC.Components.DfcNewGear.GetParameterTable(gearNew)
                if parameters.identifier then
                    local actionClosure
                    local actionChunk = parameters.actionChunk
                    if actionChunk then
                        local ret = dostring(actionChunk)
                        if type(ret) == "function" then
                            actionClosure = ret
                        else
                            log(("Failed to load chunk for gear(%s) with an error message: %s"):format(parameters.identifier, ret), 'e')
                        end
                    end
                    local gear = self:newGear(parameters.identifier, actionClosure)
                    gear.participantTickets = parameters.participantTickets
                    gear.participantNumberLimit = parameters.participantNumberLimit
                    gear.participantWeight = parameters.participantWeight
                    gear.characterTags = parameters.characterTags
                    if parameters.sort then gear.sort = parameters.sort end
                    if parameters.notifyTeammates then gear.notifyTeammates = parameters.notifyTeammates end
                end
            end
        end

        local jobNewList = Util.GetItemsById("dfc_newjob")
        if jobNewList then
            for _, jobNew in ipairs(jobNewList) do
                local parameters = DFC.Components.DfcNewJob.GetParameterTable(jobNew)
                if parameters.identifier and parameters.name then
                    local onAssignedClosure
                    local onAssignedChunk = parameters.onAssignedChunk
                    if onAssignedChunk then
                        local ret = dostring(onAssignedChunk)
                        if type(ret) == "function" then
                            onAssignedClosure = ret
                        else
                            log(("Failed to load chunk for job(%s) with an error message: %s"):format(parameters.identifier, ret), 'e')
                        end
                    end
                    local job = self:newJob(parameters.identifier, parameters.name, onAssignedClosure, parameters.liveConsumption)
                    job.participantTickets = parameters.participantTickets
                    job.participantNumberLimit = parameters.participantNumberLimit
                    job.participantWeight = parameters.participantWeight
                    job.characterTags = parameters.characterTags
                    for _, identifier in ipairs(parameters.gears) do job:addGear(identifier) end
                    for _, identifier in ipairs(parameters.spawnPointSets) do job:addSpawnPointSet(identifier) end
                    if parameters.sort then job.sort = parameters.sort end
                    if parameters.notifyTeammates then job.notifyTeammates = parameters.notifyTeammates end
                end
            end
        end

        local factionNewList = Util.GetItemsById("dfc_newfaction")
        if factionNewList then
            for _, factionNew in ipairs(factionNewList) do
                local parameters = DFC.Components.DfcNewFaction.GetParameterTable(factionNew)
                if parameters.identifier then
                    local onJoinedClosure
                    local onJoinedChunk = parameters.onJoinedChunk
                    if onJoinedChunk then
                        local ret = dostring(onJoinedChunk)
                        if type(ret) == "function" then
                            onJoinedClosure = ret
                        else
                            log(("Failed to load chunk for faction(%s) with an error message: %s"):format(parameters.identifier, ret), 'e')
                        end
                    end
                    local faction = self:newFaction(parameters.identifier, parameters.teamID, parameters.maxLives, onJoinedClosure)
                    faction.participantTickets = parameters.participantTickets
                    faction.participantNumberLimit = parameters.participantNumberLimit
                    faction.participantWeight = parameters.participantWeight
                    faction.characterTags = parameters.characterTags
                    for _, identifier in ipairs(parameters.jobs) do faction:addJob(identifier) end
                    if parameters.sort then faction.sort = parameters.sort end
                    if parameters.notifyTeammates then faction.notifyTeammates = parameters.notifyTeammates end
                end
            end
        end
    end)

    DSSI.Hook("roundEnd", "DFC", function()
        DFC.Loaded = nil

        if CLIENT then
            chat.removecommand("!factionlist")
            chat.removecommand("!joinfaction")
            chat.removecommand("!joblist")
            chat.removecommand("!assignjob")
            chat.removecommand("!assignjobs")
            chat.removecommand("!gearlist")
            chat.removecommand("!spawngear")
            chat.removecommand("!spawngears")
        end

        if SERVER then
            self:resetRoundDatas()
            for _, faction in pairs(self.factions) do faction:resetParticipatoryDatas() end
            for _, job in pairs(self.jobs) do job:resetParticipatoryDatas() end
            for _, gear in pairs(self.gears) do gear:resetParticipatoryDatas() end
            for _, spawnPointSet in pairs(self.spawnPointSets) do spawnPointSet:uncache() end

            chat.removecommand("!fixprompt")
            chat.removecommand("!rejoin")

            Hook.RemovePatch(
                "DFC",
                "Barotrauma.Networking.RespawnManager", "RespawnCharacters",
                {
                    "Microsoft.Xna.Framework.Vector2",
                    "out System.Boolean",
                },
                Hook.HookMethodType.Before
            )
        end
    end)

    if SERVER then
        DSSI.Think {
            identifier = "DFC",
            interval = 60,
            ingame = false,
            function()
                if not Game.RoundStarted or not self.existAnyFaction then return end
                ---@type dfc.faction[]
                local factions = nil
                ---@type string[]
                local factionOptions = nil

                ---@type { [dfc.faction]:dfc.job[] }
                local factionJobs = {}
                ---@type { [dfc.faction]:string[] }
                local factionJobOptions = {}

                ---@type { [dfc.job]:dfc.gear[] }
                local jobGears = {}
                ---@type { [dfc.job]:string[] }
                local jobGearOptions = {}

                local clients = self.allowMidRoundJoin
                    and Client.ClientList
                    or DFC.GetConnectedClientListBySteamIDs(self._firstPlayerSteamIDs)
                moses.forEachi(clients, function(client)
                    if (not Game.ServerSettings.AllowSpectating or not client.SpectateOnly)
                        and client.InGame
                        and not client.NeedsMidRoundSync
                    then
                        local clientCharacter = client.Character
                        local clientSteamID = client.SteamID
                        if clientCharacter == nil or clientCharacter.Removed or clientCharacter.IsDead then
                            local joinedFaction = self._joinedFaction[clientSteamID]
                            if not joinedFaction then
                                if not self._promptedJoiningFaction[clientSteamID] then
                                    self:trySortFactions()
                                    factions = factions or moses.clone(self.sortedFactions, true)
                                    factionOptions = factionOptions or moses.mapi(
                                        factions,
                                        function(faction, index)
                                            return ("[%i] %s"):format(
                                                index,
                                                faction:statistic(self.factions, UniversalFactionParticipateKey, "FactionDisplayName")
                                            )
                                        end
                                    )
                                    dialog.prompt(l10n "PromptJoinFaction".value, factionOptions, client,
                                        function(option, responder)
                                            if option == 255 then
                                                self._promptedJoiningFaction[responder.SteamID] = nil
                                                return
                                            end
                                            local faction = factions[option + 1]
                                            if faction:participatory(self.factions, UniversalFactionParticipateKey) then
                                                faction:addParticipator(UniversalFactionParticipateKey, responder.SteamID)
                                                faction:modifyTickets(UniversalFactionParticipateKey, -1)
                                                self._remainingLives[responder.SteamID] = faction.maxLives
                                                self._joinedFaction[responder.SteamID] = faction

                                                chat.send { l10n "PrivateJoinFactionSuccess":format(
                                                    l10n { "FactionDisplayName", faction.identifier }.altvalue
                                                ), responder, msgtypes = ChatMessageType.Private }

                                                local steamIDs = moses.clone(faction:tryGetParticipatorsByKeyEvenIfNil(UniversalFactionParticipateKey))
                                                moses.remove(steamIDs, responder.SteamID)
                                                local recipients = DFC.GetConnectedClientListBySteamIDs(steamIDs)
                                                if next(recipients) ~= nil then
                                                    local boardcastFactionMessage = l10n "BoardcastTeammateJoinFactionSuccess":format(
                                                        utils.ClientLogName(responder)
                                                    )
                                                    moses.forEachi(recipients, function(recipient)
                                                        chat.send { boardcastFactionMessage, recipient, msgtypes = ChatMessageType.Private }
                                                    end)
                                                end
                                            else
                                                chat.send { l10n "PrivateJoinFactionFailure":format(
                                                    l10n { "FactionDisplayName", faction.identifier }.altvalue
                                                ), responder, msgtypes = ChatMessageType.Private }
                                                self._promptedJoiningFaction[responder.SteamID] = nil
                                            end
                                        end, nil, true)
                                    self._promptedJoiningFaction[clientSteamID] = true
                                end
                            elseif joinedFaction.allowRespawn
                                and joinedFaction.existAnyJob
                                and not self._assignedJob[clientSteamID]
                                and not self._waitRespawn[clientSteamID]
                                and not self._promptedAssigningJob[clientSteamID]
                                and self._remainingLives[clientSteamID] > 0
                                and moses.include(joinedFaction.jobs, function(job)
                                    return job.liveConsumption <= self._remainingLives[clientSteamID]
                                end) then
                                joinedFaction:trySortJobs()
                                ---@type dfc.job[]
                                local jobs = factionJobs[joinedFaction] or moses.clone(joinedFaction.sortedJobs, true)
                                local jobOptions = factionJobOptions[joinedFaction] or moses.mapi(
                                    jobs,
                                    function(job, index)
                                        return ("[%i] %s (%s)"):format(
                                            index,
                                            job:statistic(joinedFaction.jobs, joinedFaction, "JobDisplayName"),
                                            l10n "JobLiveConsumption":format(job.liveConsumption)
                                        )
                                    end)
                                factionJobs[joinedFaction] = jobs
                                factionJobOptions[joinedFaction] = jobOptions
                                dialog.prompt(l10n "PromptAssignJob":format(self._remainingLives[clientSteamID]), jobOptions, client,
                                    function(option, responder)
                                        if option == 255 then
                                            self._promptedAssigningJob[responder.SteamID] = nil
                                            return
                                        end
                                        local job = jobs[option + 1]
                                        local remainingLives = self._remainingLives[responder.SteamID]
                                        if joinedFaction.allowRespawn
                                            and joinedFaction.existAnyJob
                                            and joinedFaction.jobs[job.identifier]
                                            and remainingLives >= job.liveConsumption
                                            and job:participatory(joinedFaction.jobs, joinedFaction)
                                        then
                                            ---@type dfc.spawnpointset
                                            local spawnPointSet
                                            ---@type Barotrauma.WayPoint
                                            local spawnPoint
                                            if job.existAnySpawnPointSet then
                                                spawnPointSet = utils.SelectDynValueWeightedRandom(job.spawnPointSets, job.spawnPointSetWeights)
                                                spawnPoint = spawnPointSet:getRandom()
                                            end
                                            local spawnPosition = spawnPoint and spawnPoint.WorldPosition or Submarine.MainSub.WorldPosition

                                            ---@type Barotrauma.Character
                                            local spawnedCharacter
                                            if job.human then
                                                local variant = job.jobPrefab and math.random(0, job.jobPrefab.Variants - 1) or 0
                                                local characterInfo = CharacterInfo(CharacterPrefab.HumanSpeciesName, responder.Name, nil, job.jobPrefab, nil, variant, nil, nil)
                                                spawnedCharacter = Character.Create(characterInfo, spawnPosition, ToolBox.RandomSeed(8))
                                            else
                                                spawnedCharacter = Character.Create(job.characterPrefab, spawnPosition, ToolBox.RandomSeed(8))
                                            end
                                            if spawnedCharacter.Info then spawnedCharacter.Info.Name = responder.Name end

                                            joinedFaction:addCharacterTagsFor(spawnedCharacter)
                                            job:addCharacterTagsFor(spawnedCharacter)
                                            if spawnPointSet then
                                                spawnPointSet:addCharacterTagsFor(spawnedCharacter)
                                            end

                                            spawnedCharacter.GiveJobItems(spawnPoint)
                                            spawnedCharacter.GiveIdCardTags(spawnPoint, true)
                                            spawnedCharacter.TeamID = joinedFaction.teamID
                                            spawnedCharacter.SetOriginalTeam(joinedFaction.teamID)
                                            spawnedCharacter.UpdateTeam()
                                            responder.Character = spawnedCharacter
                                            spawnedCharacter.SetOwnerClient(responder)
                                            responder.SetClientCharacter(spawnedCharacter)
                                            if joinedFaction.onJoined then joinedFaction.onJoined(spawnedCharacter) end
                                            if job.onAssigned then job.onAssigned(spawnedCharacter) end

                                            job:addParticipator(joinedFaction, spawnedCharacter)
                                            job:modifyTickets(joinedFaction, -1)
                                            self._remainingLives[responder.SteamID] = math.max(0, remainingLives - job.liveConsumption)
                                            self._assignedJob[responder.SteamID] = job
                                            self._characterWaitChooseGearBy[responder.SteamID] = spawnedCharacter
                                            self._characterFaction[spawnedCharacter] = joinedFaction
                                            self._characterJob[spawnedCharacter] = job

                                            chat.send { l10n "PrivateAssignJobSuccess":format(
                                                ("[%i] %s"):format(
                                                    option + 1,
                                                    l10n { "JobDisplayName", job.identifier }.altvalue
                                                )
                                            ), responder, msgtypes = ChatMessageType.Private }

                                            local steamIDs = moses.clone(joinedFaction:tryGetParticipatorsByKeyEvenIfNil(UniversalFactionParticipateKey))
                                            moses.remove(steamIDs, responder.SteamID)
                                            local recipients = DFC.GetConnectedClientListBySteamIDs(steamIDs)
                                            if next(recipients) ~= nil then
                                                local boardcastJobMessage = l10n "BoardcastTeammateAssignJobSuccess":format(
                                                    utils.ClientLogName(responder),
                                                    ("[%i] %s"):format(
                                                        option + 1,
                                                        l10n { "JobDisplayName", job.identifier }.altvalue
                                                    )
                                                )
                                                moses.forEachi(recipients, function(recipient)
                                                    chat.send { boardcastJobMessage, recipient, msgtypes = ChatMessageType.Private }
                                                end)
                                            end
                                        else
                                            chat.send { l10n "PrivateAssignJobFailure":format(
                                                l10n { "JobDisplayName", job.identifier }.altvalue
                                            ), responder, msgtypes = ChatMessageType.Private }
                                            self._promptedAssigningJob[responder.SteamID] = nil
                                        end
                                    end, nil, true)
                                self._promptedAssigningJob[clientSteamID] = true
                            end
                        elseif self._characterWaitChooseGearBy[clientSteamID] == clientCharacter then
                            local job = self._characterJob[clientCharacter]
                            if job and job.existAnyGear
                                and not self._chosenGear[clientCharacter]
                                and not self._promptedChoosingGear[clientCharacter]
                            then
                                local faction = self._characterFaction[clientCharacter]
                                job:trySortGears()
                                ---@type dfc.gear[]
                                local gears = jobGears[job] or moses.clone(job.sortedGears, true)
                                local gearOptions = jobGearOptions[job] or moses.mapi(
                                    gears,
                                    function(gear, index)
                                        return ("[%i] %s"):format(
                                            index,
                                            gear:statistic(job.gears, faction, "GearDisplayName")
                                        )
                                    end
                                )
                                jobGears[job] = gears
                                jobGearOptions[job] = gearOptions
                                dialog.prompt(l10n "PromptChooseGear".value, gearOptions, client,
                                    function(option, responder)
                                        if option == 255 then
                                            self._promptedChoosingGear[clientCharacter] = nil
                                            return
                                        end
                                        local gear = gears[option + 1]
                                        if job.existAnyGear
                                            and job.gears[gear.identifier]
                                            and gear:participatory(job.gears, faction)
                                        then
                                            gear:addCharacterTagsFor(clientCharacter)
                                            if gear.action then gear.action(clientCharacter) end

                                            gear:addParticipator(faction, clientCharacter)
                                            gear:modifyTickets(faction, -1)
                                            self._chosenGear[clientCharacter] = gear

                                            chat.send { l10n "PrivateChooseGearSuccess":format(
                                                ("[%i] %s"):format(
                                                    option + 1,
                                                    l10n { "GearDisplayName", gear.identifier }.altvalue
                                                )
                                            ), responder, msgtypes = ChatMessageType.Private }
                                            local steamIDs = moses.clone(faction:tryGetParticipatorsByKeyEvenIfNil(UniversalFactionParticipateKey))
                                            moses.remove(steamIDs, responder.SteamID)
                                            local recipients = DFC.GetConnectedClientListBySteamIDs(steamIDs)
                                            if next(recipients) ~= nil then
                                                local boardcastGearMessage = l10n "BoardcastTeammateChooseGearSuccess":format(
                                                    utils.ClientLogName(responder),
                                                    ("[%i] %s"):format(
                                                        option + 1,
                                                        l10n { "GearDisplayName", gear.identifier }.altvalue
                                                    )
                                                )
                                                moses.forEachi(recipients, function(recipient)
                                                    chat.send { boardcastGearMessage, recipient, msgtypes = ChatMessageType.Private }
                                                end)
                                            end
                                        else
                                            chat.send { l10n "PrivateChooseGearFailure":format(
                                                l10n { "GearDisplayName", gear.identifier }.altvalue
                                            ), responder, msgtypes = ChatMessageType.Private }
                                            self._promptedChoosingGear[clientCharacter] = nil
                                        end
                                    end, nil, false)
                                self._promptedChoosingGear[clientCharacter] = true
                            end
                        end
                    end
                end)
            end
        }

        DSSI.Hook("character.death", "DFC",
            ---@param dead Barotrauma.Character
            function(dead)
                for _, job in pairs(self.jobs) do
                    for _, characters in pairs(job._participators) do
                        for i = #characters, 1, -1 do
                            local character = characters[i]
                            if dead == character then
                                table.remove(characters, i)
                                self._characterFaction[dead] = nil
                                self._characterJob[dead] = nil
                                self._chosenGear[dead] = nil
                                self._promptedChoosingGear[dead] = nil
                                local clientSteamID = moses.invert(self._characterWaitChooseGearBy)[dead]
                                if clientSteamID then
                                    self._assignedJob[clientSteamID] = nil
                                    self._promptedAssigningJob[clientSteamID] = nil
                                    self._waitRespawn[clientSteamID] = true
                                end
                            end
                        end
                    end
                end
                for _, gear in pairs(self.gears) do
                    for _, characters in pairs(gear._participators) do
                        for i = #characters, 1, -1 do
                            local character = characters[i]
                            if dead == character then
                                table.remove(characters, i)
                            end
                        end
                    end
                end
            end
        )

        DSSI.Hook("client.disconnected", "DFC",
            ---@param client Barotrauma.Networking.Client
            function(client)
                local clientSteamID = client.SteamID
                self._promptedJoiningFaction[clientSteamID] = nil
                self._promptedAssigningJob[clientSteamID] = nil
                local character = self._characterWaitChooseGearBy[clientSteamID]
                if character then
                    self._promptedChoosingGear[character] = nil
                end
            end
        )
    end
end
