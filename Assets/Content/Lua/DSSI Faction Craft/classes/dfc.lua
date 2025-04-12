local log = require "utilbelt.logger" ("DFC")
local l10n = require "utilbelt.l10n"
local chat = require "utilbelt.chat"
local dialog = require "utilbelt.dialog"
local itbu = require "utilbelt.itbu"
local utils = require "utilbelt.csharpmodule.Shared.Utils"

---@class dfc.inner
---@field dfc dfc

local selectionModeNone = 0
local selectionModeManual = 1
local selectionModeRandom = 2

---@class dfc
---@field itemBuilders { [string]:itembuilder }
---@field spawnPointSets { [string]:dfc.spawnpointset }
---@field shouldSortFactions boolean
---@field factions { [string]:dfc.faction }
---@field sortedFactions dfc.faction[]
---@field existAnyFaction boolean
---@field factionCount integer
---@field jobs { [string]:dfc.job }
---@field gears { [string]:dfc.gear }
---@field _firstClientAccountIds string[]
---@field _shuffledConnectedClients Barotrauma.Networking.Client[]
---@field _shuffledFirstClients Barotrauma.Networking.Client[]
---@field _originalSpectateOnly { [string]:boolean }
---@field _promptedVoteForSelectionModes { [string]:boolean }
---@field _remainingLives { [string]:integer }
---@field _joinedFaction { [string|unknown]:dfc.faction }
---@field _promptedJoinFaction { [string]:boolean }
---@field _assignedJob { [string]:dfc.job }
---@field _waitRespawn { [string]:boolean }
---@field _promptedAssignJob { [string]:boolean }
---@field _characterFaction { [Barotrauma.Character]:dfc.faction }
---@field _characterJob { [Barotrauma.Character]:dfc.job }
---@field _chosenGear { [Barotrauma.Character]:dfc.gear }
---@field _promptedChooseGear { [Barotrauma.Character]:boolean }
---@field _proxyCharacters { [string]: { [Barotrauma.Character]: boolean } }
---@field _clientCharacterInfoRegistries { [string]: { [dfc.faction]: { [dfc.job]: Barotrauma.CharacterInfo } } }
---@field _clientUnchangeableJobs { [string]: { [dfc.faction]: dfc.job } }
---@field _cachedFactions dfc.faction[]
---@field _cachedFactionOptions string[]
---@field _cachedJobs { [dfc.faction]:dfc.job[] }
---@field _cachedJobOptions { [dfc.faction]:string[] }
---@field _cachedGears { [dfc.faction]: { [dfc.job]:dfc.gear[] } }
---@field _cachedGearOptions { [dfc.faction]: { [dfc.job]:string[] } }
---@field _selectionModeManualVotes integer
---@field _selectionModeRandomVotes integer
---@field _selectionMode integer
---@field _selectionModeTimer number
---@field allowMidRoundJoin boolean
---@field allowRespawn boolean
---@field autoParticipateWhenNoChoices boolean
---@overload fun():self
local m = Class 'dfc'

function m:__init()
    self.itemBuilders = {}
    self.spawnPointSets = {}
    self.factions = {}
    self.sortedFactions = {}
    self.existAnyFaction = false
    self.factionCount = 0
    self.jobs = {}
    self.gears = {}
    self:resetRoundDatas()
    self.allowMidRoundJoin = true
    self.allowRespawn = true
    self.autoParticipateWhenNoChoices = true
end

function m:resetRoundDatas()
    self._firstClientAccountIds = {}
    self._shuffledConnectedClients = {}
    self._shuffledFirstClients = {}
    self._originalSpectateOnly = {}
    self._promptedVoteForSelectionModes = {}
    self._remainingLives = {}
    self._joinedFaction = {}
    self._promptedJoinFaction = {}
    self._assignedJob = {}
    self._waitRespawn = {}
    self._promptedAssignJob = {}
    self._characterFaction = {}
    self._characterJob = {}
    self._chosenGear = {}
    self._promptedChooseGear = {}
    self._proxyCharacters = {}
    self._clientCharacterInfoRegistries = {}
    self._clientUnchangeableJobs = {}
    self._cachedFactions = {}
    self._cachedFactionOptions = {}
    self._cachedJobs = {}
    self._cachedJobOptions = {}
    self._cachedGears = {}
    self._cachedGearOptions = {}
    self._selectionModeManualVotes = 0
    self._selectionModeRandomVotes = 0
    self._selectionMode = selectionModeNone
    self._selectionModeTimer = nil
end

---@param shuffled table
---@param original table
local function clearAndShuffleArray(shuffled, original)
    moses.clear(shuffled)
    moses.forEachi(
        moses.shuffle(original),
        function(value)
            moses.addTop(shuffled, value)
        end
    )
end

function m:refreshCacheClientList()
    clearAndShuffleArray(self._shuffledConnectedClients, Client.ClientList)
    clearAndShuffleArray(self._shuffledFirstClients, DFC.GetClientListByAccountIds(self._firstClientAccountIds, Client.ClientList))
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

function m:refreshCacheFactions()
    self:trySortFactions()
    self._cachedFactions = moses.clone(self.sortedFactions, true)
    self._cachedFactionOptions = moses.mapi(
        self._cachedFactions,
        function(faction, index)
            return ("[%i] %s"):format(
                index,
                faction:statistic(l10n { "FactionDisplayName", faction.identifier }.altvalue, self.factions)
            )
        end
    )
end

---@param faction dfc.faction
function m:refreshCacheJobs(faction)
    faction:trySortJobs()
    local jobs = moses.clone(faction.sortedJobs, true)
    local jobOptions = moses.mapi(
        jobs,
        function(job, index)
            return ("[%i] %s (%s)"):format(
                index,
                job:statistic(l10n { "JobDisplayName", job.identifier }.altvalue, faction.jobs, faction),
                l10n "JobLiveConsumption":format(job.liveConsumption)
            )
        end
    )
    self._cachedJobs[faction] = jobs
    self._cachedJobOptions[faction] = jobOptions
end

---@param faction dfc.faction
---@param job dfc.job
function m:refreshCacheGears(faction, job)
    job:trySortGears()
    local gears = moses.clone(job.sortedGears, true)
    local gearOptions = moses.mapi(
        gears,
        function(gear, index)
            return ("[%i] %s"):format(
                index,
                gear:statistic(l10n { "GearDisplayName", gear.identifier }.altvalue, job.gears, faction)
            )
        end
    )
    self._cachedGears[faction] = self._cachedGears[faction] or {}
    self._cachedGears[faction][job] = gears
    self._cachedGearOptions[faction] = self._cachedGearOptions[faction] or {}
    self._cachedGearOptions[faction][job] = gearOptions
end

function m:refreshCacheFJGs()
    self:refreshCacheFactions()
    for _, faction in pairs(self.factions) do
        self:refreshCacheJobs(faction)
        for _, job in pairs(faction.jobs) do
            self:refreshCacheGears(faction, job)
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
    self.factionCount = self.factionCount + 1
    faction.dfc = self
    self.shouldSortFactions = true
    self:refreshCacheFactions()
    return faction
end

---@param identifier string
---@param name? string
---@param onAssigned? fun(character:Barotrauma.Character)
---@param liveConsumption? integer
---@param jobName? string
---@param speciesName? string
function m:newJob(identifier, name, onAssigned, liveConsumption, jobName, speciesName)
    local job = New 'dfc.job' (identifier, name, onAssigned, liveConsumption, jobName, speciesName)
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

    DSSI.Hook("roundStart", "DFC", function()
        if SERVER then
            moses.forEach(self._originalSpectateOnly, function(spectateOnly, clientAccountId)
                local client = DFC.GetClientByAccountId(clientAccountId)
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
                if parameters.identifier then
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
                    local job = self:newJob(parameters.identifier, parameters.name, onAssignedClosure, parameters.liveConsumption, parameters.jobName, parameters.speciesName)
                    job.participantTickets = parameters.participantTickets
                    job.participantNumberLimit = parameters.participantNumberLimit
                    job.participantWeight = parameters.participantWeight
                    job.characterTags = parameters.characterTags
                    for _, identifier in ipairs(parameters.gears) do job:addGear(identifier, false) end
                    for _, identifier in ipairs(parameters.spawnPointSets) do job:addSpawnPointSet(identifier) end
                    if parameters.sort then job.sort = parameters.sort end
                    if parameters.notifyTeammates then job.notifyTeammates = parameters.notifyTeammates end
                    if parameters.inhertCharacterInfo then job.inhertCharacterInfo = parameters.inhertCharacterInfo end
                    if parameters.disallowChangeJob then job.disallowChangeJob = parameters.disallowChangeJob end
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
                    for _, identifier in ipairs(parameters.jobs) do faction:addJob(identifier, false) end
                    if parameters.sort then faction.sort = parameters.sort end
                    if parameters.notifyTeammates then faction.notifyTeammates = parameters.notifyTeammates end
                    faction.allowRespawn = parameters.allowRespawn
                    faction.respawnIntervalMultiplier = parameters.respawnIntervalMultiplier
                end
            end
        end

        self:refreshCacheFJGs()
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

            DFC.OverrideRespawnManager = false

            Hook.RemovePatch(
                "DFC",
                "Barotrauma.Character",
                "Remove",
                Hook.HookMethodType.After
            )
        end
    end)

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
            Game.ServerSettings.RespawnMode = self.allowRespawn and 0 or 1
            if self.allowRespawn then
                DFC.OverrideRespawnManager = true
            end
        end

        ---@param client Barotrauma.Networking.Client
        local function fixPrompt(client)
            local clientAccountId = client.AccountId.StringRepresentation
            local clientCharacter = client.Character
            if not self._joinedFaction[clientAccountId] then
                self._promptedJoinFaction[clientAccountId] = nil
            end
            if not self._assignedJob[clientAccountId] then
                self._promptedAssignJob[clientAccountId] = nil
            end
            if clientCharacter and not self._chosenGear[clientCharacter] then
                self._promptedChooseGear[clientCharacter] = nil
            end
            if self._proxyCharacters[clientAccountId] then
                for proxyCharacter, _ in pairs(self._proxyCharacters[clientAccountId]) do
                    if not self._chosenGear[proxyCharacter] then
                        self._promptedChooseGear[proxyCharacter] = nil
                    end
                end
            end
        end

        chat.addcommand({
            { "!fix", "!fixprompt" },
            help = l10n { "ChatCommandFixPrompt" }.value,
            callback = function(client)
                fixPrompt(client)
            end
        })

        chat.addcommand({
            "!rejoin",
            callback = function(client, args)
                local clientAccountId = client.AccountId.StringRepresentation
                self._joinedFaction[clientAccountId] = nil
                self._promptedJoinFaction[clientAccountId] = nil
                self._assignedJob[clientAccountId] = nil
                self._promptedAssignJob[clientAccountId] = nil
                client.SpectateOnly = false
            end,
            hidden = true,
            permissions = ClientPermissions.All
        })

        moses.forEachi(Client.ClientList, function(client)
            table.insert(self._firstClientAccountIds, client.AccountId.StringRepresentation)
            self._originalSpectateOnly[client.AccountId.StringRepresentation] = client.SpectateOnly
            client.SpectateOnly = true
        end)

        self:refreshCacheClientList()

        self._selectionModeTimer = Timer.Time + 30.0

        DSSI.Think {
            identifier = "DFC",
            interval = 60,
            ingame = false,
            function()
                if self._selectionMode == selectionModeNone then
                    self:selectionModeVote()
                else
                    self:update()
                end
            end
        }

        ---@param character Barotrauma.Character
        local function tryClearCharacter(character)
            local characterFaction = self._characterFaction[character]
            local characterJob = self._characterJob[character]
            self._characterFaction[character] = nil
            self._characterJob[character] = nil
            self._chosenGear[character] = nil
            self._promptedChooseGear[character] = nil

            for _, job in pairs(self.jobs) do
                if job:tryRemoveParticipator(character) and characterFaction then
                    self:refreshCacheJobs(characterFaction)
                end
            end

            for _, gear in pairs(self.gears) do
                if gear:tryRemoveParticipator(character) and characterFaction and characterJob then
                    self:refreshCacheGears(characterFaction, characterJob)
                end
            end

            for clientAccountId, proxyCharacters in pairs(self._proxyCharacters) do
                for proxyCharacter, _ in pairs(proxyCharacters) do
                    if proxyCharacter == character then
                        self._assignedJob[clientAccountId] = nil
                        self._promptedAssignJob[clientAccountId] = nil
                        self._waitRespawn[clientAccountId] = true
                        local client = DFC.GetClientByAccountId(clientAccountId)
                        if client and client.Character == character then
                            client.SetClientCharacter(nil)
                        end
                        proxyCharacters[character] = nil
                    end
                end
            end
        end

        Hook.Patch(
            "DFC",
            "Barotrauma.Character",
            "Remove",
            tryClearCharacter,
            Hook.HookMethodType.After
        )

        DSSI.Hook("character.death", "DFC", tryClearCharacter)

        DSSI.Hook("client.disconnected", "DFC",
            ---@param client Barotrauma.Networking.Client
            function(client)
                fixPrompt(client)
                self:refreshCacheClientList()
            end
        )

        DSSI.Hook("client.connected", "DFC",
            ---@param newClient Barotrauma.Networking.Client
            function(newClient)
                self:refreshCacheClientList()
            end
        )
    end
end

function m:selectionModeVote()
    if not Game.RoundStarted or not self.existAnyFaction then return false end

    local function getVoters()
        local disallowSpectating = not Game.ServerSettings.AllowSpectating
        return moses.filter(
            self.allowMidRoundJoin and self._shuffledConnectedClients or self._shuffledFirstClients,
            function(client)
                return disallowSpectating or not client.SpectateOnly
            end
        )
    end

    local voters = getVoters()

    if self._selectionModeManualVotes > #voters / 2 then
        self._selectionMode = selectionModeManual
        Lub.Chat.boardcast { l10n "SelectionModeManualApprovedViaOverHalfVoters".value, msgtypes = { ChatMessageType.Default, ChatMessageType.ServerMessageBoxInGame }, color = Color(255, 255, 255) }
    elseif self._selectionModeRandomVotes > #voters / 2 then
        self._selectionMode = selectionModeRandom
        Lub.Chat.boardcast { l10n "SelectionModeRandomApprovedViaOverHalfVoters".value, msgtypes = { ChatMessageType.Default, ChatMessageType.ServerMessageBoxInGame }, color = Color(255, 255, 255) }
    elseif self._selectionModeManualVotes + self._selectionModeRandomVotes == #voters
        or Timer.Time > self._selectionModeTimer
    then
        if self._selectionModeManualVotes > self._selectionModeRandomVotes then
            self._selectionMode = selectionModeManual
            Lub.Chat.boardcast { l10n "SelectionModeManualApprovedViaMajorityVoters".value, msgtypes = { ChatMessageType.Default, ChatMessageType.ServerMessageBoxInGame }, color = Color(255, 255, 255) }
        elseif self._selectionModeManualVotes < self._selectionModeRandomVotes then
            self._selectionMode = selectionModeRandom
            Lub.Chat.boardcast { l10n "SelectionModeRandomApprovedViaMajorityVoters".value, msgtypes = { ChatMessageType.Default, ChatMessageType.ServerMessageBoxInGame }, color = Color(255, 255, 255) }
        else
            if math.random() > 0.5 then
                self._selectionMode = selectionModeManual
                Lub.Chat.boardcast { l10n "SelectionModeManualApprovedViaRiggedMatch".value, msgtypes = { ChatMessageType.Default, ChatMessageType.ServerMessageBoxInGame }, color = Color(255, 255, 255) }
            else
                self._selectionMode = selectionModeRandom
                Lub.Chat.boardcast { l10n "SelectionModeRandomApprovedViaRiggedMatch".value, msgtypes = { ChatMessageType.Default, ChatMessageType.ServerMessageBoxInGame }, color = Color(255, 255, 255) }
            end
        end
    else
        moses.forEachi(voters, function(voter)
            local voterAccountId = voter.AccountId.StringRepresentation
            if voter.InGame and not voter.NeedsMidRoundSync and not self._promptedVoteForSelectionModes[voterAccountId] then
                dialog.prompt(
                    l10n "PromptVoteForSelectionModes".value,
                    {
                        l10n "SelectionModeManual".value,
                        l10n "SelectionModeRandom".value
                    },
                    voter,
                    ---@param option_index integer
                    ---@param responder Barotrauma.Networking.Client
                    function(option_index, responder)
                        if self._selectionMode ~= selectionModeNone then return end

                        local responderAccountId = responder.AccountId.StringRepresentation
                        if option_index == 255 then
                            self._promptedVoteForSelectionModes[responderAccountId] = nil
                            return
                        end

                        if option_index == 0 then
                            self._selectionModeManualVotes = self._selectionModeManualVotes + 1
                        elseif option_index == 1 then
                            self._selectionModeRandomVotes = self._selectionModeRandomVotes + 1
                        end

                        local maxVotes = #getVoters()

                        local boardcastMessage = l10n "BoardcastSlectionModeVotes":format(
                            ("%.0f"):format(100 * self._selectionModeManualVotes / maxVotes),
                            ("%.0f"):format(100 * self._selectionModeRandomVotes / maxVotes)
                        )

                        Lub.Chat.boardcast { boardcastMessage, msgtypes = ChatMessageType.Private }
                    end,
                    "noteopened",
                    false
                )
                self._promptedVoteForSelectionModes[voterAccountId] = true
            end
        end)
    end
end

function m:update()
    if not Game.RoundStarted or not self.existAnyFaction then return end

    local disallowSpectating = not Game.ServerSettings.AllowSpectating
    moses.forEachi(self.allowMidRoundJoin and self._shuffledConnectedClients or self._shuffledFirstClients, function(client)
        if (disallowSpectating or not client.SpectateOnly)
            and client.InGame
            and not client.NeedsMidRoundSync
        then
            local clientCharacter = client.Character
            local clientAccountId = client.AccountId.StringRepresentation
            if clientCharacter == nil or clientCharacter.Removed or clientCharacter.IsDead then
                local joinedFaction = self._joinedFaction[clientAccountId]
                if not joinedFaction then
                    ---@type dfc.faction[]
                    local contextFactions
                    ---@type string[]
                    local contextFactionOptions
                    local function pretaskJoinFaction()
                        contextFactions = moses.clone(self._cachedFactions, true)
                        contextFactionOptions = moses.clone(self._cachedFactionOptions, true)
                    end

                    ---@param option_index integer
                    ---@param responder Barotrauma.Networking.Client
                    local function chooseCallback(option_index, responder)
                        local responderAccountId = responder.AccountId.StringRepresentation
                        if option_index == 255 then
                            self._promptedJoinFaction[responderAccountId] = nil
                            return
                        end
                        local option = option_index + 1
                        ---@type dfc.faction
                        local faction = contextFactions[option]
                        if self.factions[faction.identifier]
                            and not self._joinedFaction[responderAccountId]
                            and faction:participatory(self.factions)
                        then
                            faction:addParticipator(responderAccountId)
                            faction:modifyTickets(-1)
                            self:refreshCacheFactions()
                            self._remainingLives[responderAccountId] = faction.maxLives
                            self._joinedFaction[responderAccountId] = faction

                            chat.send { l10n "PrivateJoinFactionSuccess":format(
                                l10n { "FactionDisplayName", faction.identifier }.altvalue
                            ), responder, msgtypes = ChatMessageType.Private }

                            if faction.notifyTeammates then
                                local boardcastFactionMessage = l10n "BoardcastTeammateJoinFactionSuccess":format(
                                    utils.ClientLogName(responder),
                                    l10n { "FactionDisplayName", faction.identifier }.altvalue
                                )
                                Lub.Chat.boardcast {
                                    boardcastFactionMessage,
                                    msgtypes = ChatMessageType.Private,
                                    filter = function(client)
                                        return client ~= responder
                                    end
                                }
                            end
                        else
                            chat.send { l10n "PrivateJoinFactionFailure":format(
                                l10n { "FactionDisplayName", faction.identifier }.altvalue
                            ), responder, msgtypes = ChatMessageType.Private }
                            self._promptedJoinFaction[responderAccountId] = nil
                        end
                    end

                    if self._selectionMode == selectionModeManual then
                        if self.factionCount == 1 and self.autoParticipateWhenNoChoices then
                            pretaskJoinFaction()
                            if contextFactions[1]:participatory(self.factions) then
                                chooseCallback(0, client)
                            end
                        elseif not self._promptedJoinFaction[clientAccountId] then
                            pretaskJoinFaction()
                            if moses.any(contextFactions, function(faction)
                                    return faction:participatory(self.factions)
                                end)
                            then
                                dialog.prompt(l10n "PromptJoinFaction".value, contextFactionOptions, client, chooseCallback, "group", true)
                                self._promptedJoinFaction[clientAccountId] = true
                            end
                        end
                    else
                        pretaskJoinFaction()
                        local option_index
                        for _, i in ipairs(moses.shuffle(moses.range(#contextFactions))) do
                            if contextFactions[i]:participatory(self.factions) then
                                option_index = i - 1
                                break
                            end
                        end
                        if option_index then
                            chooseCallback(option_index, client)
                        end
                    end
                elseif joinedFaction.allowRespawn
                    and joinedFaction.existAnyJob
                    and not self._assignedJob[clientAccountId]
                    and not self._waitRespawn[clientAccountId]
                    and self._remainingLives[clientAccountId] > 0
                then
                    ---@type dfc.job[]
                    local contextFactionJobs
                    ---@type string[]
                    local contextFactionJobOptions
                    local function pretaskAssignJob()
                        contextFactionJobs = moses.clone(self._cachedJobs[joinedFaction], true)
                        contextFactionJobOptions = moses.clone(self._cachedJobOptions[joinedFaction], true)
                    end

                    ---@param option_index integer
                    ---@param responder Barotrauma.Networking.Client
                    local chooseCallback = function(option_index, responder)
                        ---@type string
                        local responderAccountId = responder.AccountId.StringRepresentation
                        if option_index == 255 then
                            self._promptedAssignJob[responderAccountId] = nil
                            return
                        end
                        local option = option_index + 1
                        local job = contextFactionJobs[option]
                        local remainingLives = self._remainingLives[responderAccountId]
                        if joinedFaction.allowRespawn
                            and joinedFaction.jobs[job.identifier]
                            and not self._assignedJob[responderAccountId]
                            and not self._waitRespawn[responderAccountId]
                            and remainingLives > 0
                            and remainingLives >= job.liveConsumption
                            and job:participatory(joinedFaction.jobs, joinedFaction)
                        then
                            ---@type dfc.spawnpointset?
                            local spawnPointSet
                            ---@type Barotrauma.WayPoint?
                            local spawnPoint
                            if job.existAnySpawnPointSet then
                                spawnPointSet = utils.SelectDynValueWeightedRandom(job.spawnPointSets, job.spawnPointSetWeights)
                                spawnPoint = spawnPointSet:getRandom()
                            end
                            local spawnPosition = spawnPoint and spawnPoint.WorldPosition or Submarine.MainSub.WorldPosition

                            ---@type Barotrauma.Character
                            local spawnedCharacter

                            -- TODO: all characters that has info will still have registry of inheritance rather only than human
                            local characterInfosRegistry
                            if job.inhertCharacterInfo then
                                characterInfosRegistry = self._clientCharacterInfoRegistries[responderAccountId]
                                if characterInfosRegistry then
                                    local jobMapCharacterInfo = characterInfosRegistry[joinedFaction]
                                    if jobMapCharacterInfo then
                                        local characterInfo = jobMapCharacterInfo[job]
                                        if characterInfo then
                                            for _, character in ipairs(Character.CharacterList) do
                                                if character.Info == characterInfo then
                                                    character.Info = nil
                                                end
                                            end
                                            spawnedCharacter = Character.Create(characterInfo, spawnPosition, ToolBox.RandomSeed(8))
                                            spawnedCharacter.LoadTalents()
                                        end
                                    end
                                else
                                    characterInfosRegistry = {}
                                    self._clientCharacterInfoRegistries[responderAccountId] = characterInfosRegistry
                                end
                            end

                            if spawnedCharacter == nil then
                                ---@type Barotrauma.CharacterInfo?
                                local characterInfo

                                if job.characterPrefab.HasCharacterInfo then
                                    local variant = job.jobPrefab and math.random(0, job.jobPrefab.Variants - 1) or 0
                                    characterInfo = CharacterInfo(job.characterPrefab.Name, responder.Name, nil, job.jobPrefab, variant, RandSync.Unsynced, nil)
                                    characterInfo.TeamID = joinedFaction.teamID
                                    spawnedCharacter = Character.Create(characterInfo, spawnPosition, ToolBox.RandomSeed(8))
                                else
                                    spawnedCharacter = Character.Create(job.characterPrefab, spawnPosition, ToolBox.RandomSeed(8))
                                end

                                if characterInfo and job.inhertCharacterInfo then
                                    local jobMapCharacterInfo = characterInfosRegistry[joinedFaction]
                                    if jobMapCharacterInfo == nil then
                                        jobMapCharacterInfo = {}
                                        characterInfosRegistry[joinedFaction] = jobMapCharacterInfo
                                    end
                                    jobMapCharacterInfo[job] = characterInfo
                                end
                            end
                            if spawnedCharacter.Info then spawnedCharacter.Info.Name = responder.Name end

                            joinedFaction:addCharacterTagsFor(spawnedCharacter)
                            job:addCharacterTagsFor(spawnedCharacter)
                            if spawnPointSet then
                                spawnPointSet:addCharacterTagsFor(spawnedCharacter)
                            end

                            spawnedCharacter.SetOriginalTeamAndChangeTeam(joinedFaction.teamID, true)
                            spawnedCharacter.GiveJobItems(false, spawnPoint)
                            spawnedCharacter.GiveIdCardTags(spawnPoint, true)
                            responder.SetClientCharacter(spawnedCharacter)
                            -- spawnedCharacter.SetOwnerClient(responder)
                            responder.TeamID = spawnedCharacter.TeamID
                            if joinedFaction.onJoined then joinedFaction.onJoined(spawnedCharacter) end
                            if job.onAssigned then job.onAssigned(spawnedCharacter) end

                            job:addParticipator(spawnedCharacter, joinedFaction)
                            job:modifyTickets(-1, joinedFaction)
                            self:refreshCacheJobs(joinedFaction)
                            self._assignedJob[responderAccountId] = job
                            self._remainingLives[responderAccountId] = remainingLives - job.liveConsumption
                            if not self._proxyCharacters[responderAccountId] then
                                self._proxyCharacters[responderAccountId] = {}
                            end
                            self._proxyCharacters[responderAccountId][spawnedCharacter] = true
                            self._characterFaction[spawnedCharacter] = joinedFaction
                            self._characterJob[spawnedCharacter] = job

                            if job.disallowChangeJob then
                                local unchangeableJobs = self._clientUnchangeableJobs[responderAccountId]
                                if unchangeableJobs == nil then
                                    unchangeableJobs = {}
                                    self._clientUnchangeableJobs[responderAccountId] = unchangeableJobs
                                end
                                unchangeableJobs[joinedFaction] = job
                            end

                            chat.send { l10n "PrivateAssignJobSuccess":format(
                                ("[%i] %s"):format(
                                    option,
                                    l10n { "JobDisplayName", job.identifier }.altvalue
                                )
                            ), responder, msgtypes = ChatMessageType.Private }

                            if job.notifyTeammates then
                                local accountIds = moses.clone(joinedFaction:tryGetParticipatorsByKeyEvenIfNil())
                                moses.remove(accountIds, responderAccountId)
                                local recipients = DFC.GetClientListByAccountIds(accountIds)
                                local boardcastJobMessage = l10n "BoardcastTeammateAssignJobSuccess":format(
                                    utils.ClientLogName(responder),
                                    ("[%i] %s"):format(
                                        option,
                                        l10n { "JobDisplayName", job.identifier }.altvalue
                                    )
                                )
                                Lub.Chat.boardcast {
                                    boardcastJobMessage,
                                    msgtypes = ChatMessageType.Private,
                                    filter = function(client)
                                        return moses.include(recipients, client) or client.SpectateOnly
                                    end
                                }
                            end
                        else
                            chat.send { l10n "PrivateAssignJobFailure":format(
                                l10n { "JobDisplayName", job.identifier }.altvalue
                            ), responder, msgtypes = ChatMessageType.Private }
                            self._promptedAssignJob[responderAccountId] = nil
                        end
                    end

                    local unchangeableJobs = self._clientUnchangeableJobs[clientAccountId]
                    if unchangeableJobs then
                        local job = unchangeableJobs[joinedFaction]
                        if job then
                            if joinedFaction.jobs[job.identifier]
                                and self._remainingLives[clientAccountId] >= job.liveConsumption
                                and job:participatory(joinedFaction.jobs, joinedFaction)
                            then
                                pretaskAssignJob()
                                for i = 1, #contextFactionJobs do
                                    if contextFactionJobs[i] == job then
                                        chooseCallback(i - 1, client)
                                        break
                                    end
                                end
                            end
                            return
                        end
                    end

                    if joinedFaction.jobCount == 1 and self.autoParticipateWhenNoChoices then
                        pretaskAssignJob()
                        if self._remainingLives[clientAccountId] >= contextFactionJobs[1].liveConsumption
                            and contextFactionJobs[1]:participatory(joinedFaction.jobs, joinedFaction)
                        then
                            chooseCallback(0, client)
                        end
                    elseif not self._promptedAssignJob[clientAccountId] then
                        pretaskAssignJob()
                        if moses.any(contextFactionJobs, function(job)
                                return self._remainingLives[clientAccountId] >= job.liveConsumption
                                    and job:participatory(joinedFaction.jobs, joinedFaction)
                            end)
                        then
                            dialog.prompt(l10n "PromptAssignJob":format(self._remainingLives[clientAccountId]), contextFactionJobOptions, client, chooseCallback, "JacovSubra1", true)
                            self._promptedAssignJob[clientAccountId] = true
                        end
                    end
                end
            else
                local characterJob = self._characterJob[clientCharacter]
                if characterJob and characterJob.existAnyGear and not self._chosenGear[clientCharacter] then
                    ---@type dfc.gear[]
                    local contextFactionJobGears
                    ---@type string[]
                    local contextFactionJobGearOptions
                    ---@type dfc.gear
                    local characterFaction
                    local function pretaskChooseGear()
                        characterFaction = self._characterFaction[clientCharacter]
                        contextFactionJobGears = moses.clone(self._cachedGears[characterFaction][characterJob], true)
                        contextFactionJobGearOptions = moses.clone(self._cachedGearOptions[characterFaction][characterJob], true)
                    end

                    local chooseCallback = function(option_index, responder)
                        local responderAccountId = responder.AccountId.StringRepresentation
                        if option_index == 255 then
                            self._promptedChooseGear[clientCharacter] = nil
                            return
                        end
                        local option = option_index + 1
                        local gear = contextFactionJobGears[option]
                        if characterJob.gears[gear.identifier]
                            and not self._chosenGear[clientCharacter]
                            and gear:participatory(characterJob.gears, characterFaction)
                        then
                            gear:addCharacterTagsFor(clientCharacter)
                            if gear.action then gear.action(clientCharacter) end

                            gear:addParticipator(clientCharacter, characterFaction)
                            gear:modifyTickets(-1, characterFaction)
                            self:refreshCacheGears(characterFaction, characterJob)
                            self._chosenGear[clientCharacter] = gear

                            chat.send { l10n "PrivateChooseGearSuccess":format(
                                ("[%i] %s"):format(
                                    option,
                                    l10n { "GearDisplayName", gear.identifier }.altvalue
                                )
                            ), responder, msgtypes = ChatMessageType.Private }

                            if gear.notifyTeammates then
                                local accountIds = moses.clone(characterFaction:tryGetParticipatorsByKeyEvenIfNil())
                                moses.remove(accountIds, responderAccountId)
                                local recipients = DFC.GetClientListByAccountIds(accountIds)
                                local boardcastGearMessage = l10n "BoardcastTeammateChooseGearSuccess":format(
                                    utils.ClientLogName(responder),
                                    ("[%i] %s"):format(
                                        option,
                                        l10n { "GearDisplayName", gear.identifier }.altvalue
                                    )
                                )
                                Lub.Chat.boardcast {
                                    boardcastGearMessage,
                                    msgtypes = ChatMessageType.Private,
                                    filter = function(client)
                                        return moses.include(recipients, client) or client.SpectateOnly
                                    end
                                }
                            end
                        else
                            chat.send { l10n "PrivateChooseGearFailure":format(
                                l10n { "GearDisplayName", gear.identifier }.altvalue
                            ), responder, msgtypes = ChatMessageType.Private }
                            self._promptedChooseGear[clientCharacter] = nil
                        end
                    end

                    if characterJob.gearCount == 1 and self.autoParticipateWhenNoChoices then
                        pretaskChooseGear()
                        if contextFactionJobGears[1]:participatory(characterJob.gears, characterFaction) then
                            chooseCallback(0, client)
                        end
                    elseif not self._promptedChooseGear[clientCharacter] then
                        pretaskChooseGear()
                        if moses.any(contextFactionJobGears, function(gear)
                                return gear:participatory(characterJob.gears, characterFaction)
                            end)
                        then
                            dialog.prompt(l10n "PromptChooseGear".value, contextFactionJobGearOptions, client, chooseCallback, "Stowaway1B", false)
                            self._promptedChooseGear[clientCharacter] = true
                        end
                    end
                end
            end
        end
    end)
end
