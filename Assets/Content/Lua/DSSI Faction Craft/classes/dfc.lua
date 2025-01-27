local log = require "utilbelt.logger" ("DFC")
local l10n = require "utilbelt.l10n"
local chat = require "utilbelt.chat"
local dialog = require "utilbelt.dialog"
local itbu = require "utilbelt.itbu"
local utils = require "utilbelt.csharpmodule.Shared.Utils"

---@class dfc.inner
---@field dfc dfc

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
---@field _firstPlayerAccountIds { [string]:boolean }
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
---@field _clientCharacterInfoRegistries { [string]: { [dfc.faction]: { [dfc.job]: Barotrauma.CharacterInfo } } }
---@field allowMidRoundJoin boolean
---@field allowRespawn boolean
---@field autoParticipateWhenNoChoices boolean
---@overload fun():self
local m = Class 'dfc'

function m:__init()
    self.itemBuilders = {}
    self.spawnPointSets = {}
    self.factions = {}
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
    self._firstPlayerAccountIds = {}
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
    self._clientCharacterInfoRegistries = {}
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
    self.factionCount = self.factionCount + 1
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
            Game.ServerSettings.RespawnMode = self.allowRespawn and 0 or 1
            if self.allowRespawn then
                DFC.OverrideRespawnManager = true
            end
        end

        chat.addcommand({
            { "!fix", "!fixprompt" },
            help = l10n { "ChatCommandFixPrompt" }.value,
            callback = function(client, args)
                local clientAccountId = client.AccountId.StringRepresentation
                if not self._joinedFaction[clientAccountId] then
                    self._promptedJoiningFaction[clientAccountId] = nil
                end
                if not self._assignedJob[clientAccountId] then
                    self._promptedAssigningJob[clientAccountId] = nil
                end
                local character = self._characterWaitChooseGearBy[clientAccountId]
                if character and not self._chosenGear[character] then
                    self._promptedChoosingGear[character] = nil
                end
            end
        })

        chat.addcommand({
            "!rejoin",
            callback = function(client, args)
                local clientAccountId = client.AccountId.StringRepresentation
                self._joinedFaction[clientAccountId] = nil
                self._promptedJoiningFaction[clientAccountId] = nil
                self._assignedJob[clientAccountId] = nil
                self._promptedAssigningJob[clientAccountId] = nil
                client.SpectateOnly = false
            end,
            hidden = true,
            permissions = ClientPermissions.All
        })

        moses.forEachi(Client.ClientList, function(client)
            table.insert(self._firstPlayerAccountIds, client.AccountId.StringRepresentation)
            self._originalSpectateOnly[client.AccountId.StringRepresentation] = client.SpectateOnly
            client.SpectateOnly = true
        end)
    end

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
                    if parameters.inhertCharacterInfo then job.inhertCharacterInfo = parameters.inhertCharacterInfo end
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
                    faction.allowRespawn = parameters.allowRespawn
                    faction.respawnIntervalMultiplier = parameters.respawnIntervalMultiplier
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

            DFC.OverrideRespawnManager = false
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

                local disallowSpectating = not Game.ServerSettings.AllowSpectating
                local clients = self.allowMidRoundJoin
                    and Client.ClientList
                    or DFC.GetClientListByAccountIds(self._firstPlayerAccountIds)
                moses.forEachi(clients, function(client)
                    if (disallowSpectating or not client.SpectateOnly)
                        and client.InGame
                        and not client.NeedsMidRoundSync
                    then
                        local clientCharacter = client.Character
                        local clientAccountId = client.AccountId.StringRepresentation
                        if clientCharacter == nil or clientCharacter.Removed or clientCharacter.IsDead then
                            local joinedFaction = self._joinedFaction[clientAccountId]
                            if not joinedFaction then
                                if not self._promptedJoiningFaction[clientAccountId] then
                                    self:trySortFactions()
                                    ---@type dfc.faction[]
                                    factions = factions or moses.clone(self.sortedFactions, true)
                                    factionOptions = factionOptions or moses.mapi(
                                        factions,
                                        function(faction, index)
                                            return ("[%i] %s"):format(
                                                index,
                                                faction:statistic(l10n { "FactionDisplayName", faction.identifier }.altvalue, self.factions)
                                            )
                                        end
                                    )

                                    ---@param option_index integer
                                    ---@param responder Barotrauma.Networking.Client
                                    local function chooseCallback(option_index, responder)
                                        local responderAccountId = responder.AccountId.StringRepresentation
                                        if option_index == 255 then
                                            self._promptedJoiningFaction[responderAccountId] = nil
                                            return
                                        end
                                        local option = option_index + 1
                                        ---@type dfc.faction
                                        local faction = factions[option]
                                        if faction:participatory(self.factions) then
                                            faction:addParticipator(responderAccountId)
                                            faction:modifyTickets(-1)
                                            self._remainingLives[responderAccountId] = faction.maxLives
                                            self._joinedFaction[responderAccountId] = faction

                                            chat.send { l10n "PrivateJoinFactionSuccess":format(
                                                l10n { "FactionDisplayName", faction.identifier }.altvalue
                                            ), responder, msgtypes = ChatMessageType.Private }

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
                                        else
                                            chat.send { l10n "PrivateJoinFactionFailure":format(
                                                l10n { "FactionDisplayName", faction.identifier }.altvalue
                                            ), responder, msgtypes = ChatMessageType.Private }
                                            self._promptedJoiningFaction[responderAccountId] = nil
                                        end
                                    end

                                    if self.autoParticipateWhenNoChoices and self.factionCount == 1 then
                                        chooseCallback(0, client)
                                    else
                                        dialog.prompt(l10n "PromptJoinFaction".value, factionOptions, client, chooseCallback, nil, true)
                                    end
                                    self._promptedJoiningFaction[clientAccountId] = true
                                end
                            elseif joinedFaction.allowRespawn
                                and joinedFaction.existAnyJob
                                and not self._assignedJob[clientAccountId]
                                and not self._waitRespawn[clientAccountId]
                                and not self._promptedAssigningJob[clientAccountId]
                                and self._remainingLives[clientAccountId] > 0
                                and moses.include(joinedFaction.jobs, function(job)
                                    return job.liveConsumption <= self._remainingLives[clientAccountId]
                                end) then
                                joinedFaction:trySortJobs()
                                ---@type dfc.job[]
                                local jobs = factionJobs[joinedFaction] or moses.clone(joinedFaction.sortedJobs, true)
                                local jobOptions = factionJobOptions[joinedFaction] or moses.mapi(
                                    jobs,
                                    function(job, index)
                                        return ("[%i] %s (%s)"):format(
                                            index,
                                            job:statistic(l10n { "JobDisplayName", job.identifier }.altvalue, joinedFaction.jobs, joinedFaction),
                                            l10n "JobLiveConsumption":format(job.liveConsumption)
                                        )
                                    end)
                                factionJobs[joinedFaction] = jobs
                                factionJobOptions[joinedFaction] = jobOptions

                                ---@param option_index integer
                                ---@param responder Barotrauma.Networking.Client
                                local chooseCallback = function(option_index, responder)
                                    ---@type string
                                    local responderAccountId = responder.AccountId.StringRepresentation
                                    if option_index == 255 then
                                        self._promptedAssigningJob[responderAccountId] = nil
                                        return
                                    end
                                    local option = option_index + 1
                                    local job = jobs[option]
                                    local remainingLives = self._remainingLives[responderAccountId]
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
                                                local variant = job.jobPrefab and math.random(0, job.jobPrefab.Variants - 1) or 0
                                                local characterInfo = CharacterInfo(CharacterPrefab.HumanSpeciesName, responder.Name, nil, job.jobPrefab, variant, RandSync.Unsynced, nil)
                                                characterInfo.TeamID = joinedFaction.teamID
                                                spawnedCharacter = Character.Create(characterInfo, spawnPosition, ToolBox.RandomSeed(8))

                                                if job.inhertCharacterInfo then
                                                    local jobMapCharacterInfo = characterInfosRegistry[joinedFaction]
                                                    if jobMapCharacterInfo == nil then
                                                        jobMapCharacterInfo = {}
                                                        characterInfosRegistry[joinedFaction] = jobMapCharacterInfo
                                                    end
                                                    jobMapCharacterInfo[job] = characterInfo
                                                end
                                            end
                                        else
                                            spawnedCharacter = Character.Create(job.characterPrefab, spawnPosition, ToolBox.RandomSeed(8))
                                        end
                                        if spawnedCharacter.Info then spawnedCharacter.Info.Name = responder.Name end

                                        joinedFaction:addCharacterTagsFor(spawnedCharacter)
                                        job:addCharacterTagsFor(spawnedCharacter)
                                        if spawnPointSet then
                                            spawnPointSet:addCharacterTagsFor(spawnedCharacter)
                                        end

                                        spawnedCharacter.GiveJobItems(false, spawnPoint)
                                        spawnedCharacter.GiveIdCardTags(spawnPoint, true)
                                        responder.SetClientCharacter(spawnedCharacter)
                                        -- spawnedCharacter.SetOwnerClient(responder)
                                        responder.TeamID = spawnedCharacter.TeamID
                                        if joinedFaction.onJoined then joinedFaction.onJoined(spawnedCharacter) end
                                        if job.onAssigned then job.onAssigned(spawnedCharacter) end

                                        job:addParticipator(spawnedCharacter, joinedFaction)
                                        job:modifyTickets(-1, joinedFaction)
                                        self._remainingLives[responderAccountId] = remainingLives - job.liveConsumption
                                        self._assignedJob[responderAccountId] = job
                                        self._characterWaitChooseGearBy[responderAccountId] = spawnedCharacter
                                        self._characterFaction[spawnedCharacter] = joinedFaction
                                        self._characterJob[spawnedCharacter] = job

                                        chat.send { l10n "PrivateAssignJobSuccess":format(
                                            ("[%i] %s"):format(
                                                option,
                                                l10n { "JobDisplayName", job.identifier }.altvalue
                                            )
                                        ), responder, msgtypes = ChatMessageType.Private }

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
                                    else
                                        chat.send { l10n "PrivateAssignJobFailure":format(
                                            l10n { "JobDisplayName", job.identifier }.altvalue
                                        ), responder, msgtypes = ChatMessageType.Private }
                                        self._promptedAssigningJob[responderAccountId] = nil
                                    end
                                end

                                if self.autoParticipateWhenNoChoices and joinedFaction.jobCount == 1 then
                                    chooseCallback(0, client)
                                else
                                    dialog.prompt(l10n "PromptAssignJob":format(self._remainingLives[clientAccountId]), jobOptions, client, chooseCallback, nil, true)
                                end

                                self._promptedAssigningJob[clientAccountId] = true
                            end
                        elseif self._characterWaitChooseGearBy[clientAccountId] == clientCharacter then
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
                                            gear:statistic(l10n { "GearDisplayName", gear.identifier }.altvalue, job.gears, faction)
                                        )
                                    end
                                )
                                jobGears[job] = gears
                                jobGearOptions[job] = gearOptions

                                local chooseCallback = function(option_index, responder)
                                    local responderAccountId = responder.AccountId.StringRepresentation
                                    if option_index == 255 then
                                        self._promptedChoosingGear[clientCharacter] = nil
                                        return
                                    end
                                    local option = option_index + 1
                                    local gear = gears[option]
                                    if job.existAnyGear
                                        and job.gears[gear.identifier]
                                        and gear:participatory(job.gears, faction)
                                    then
                                        gear:addCharacterTagsFor(clientCharacter)
                                        if gear.action then gear.action(clientCharacter) end

                                        gear:addParticipator(clientCharacter, faction)
                                        gear:modifyTickets(-1, faction)
                                        self._chosenGear[clientCharacter] = gear

                                        chat.send { l10n "PrivateChooseGearSuccess":format(
                                            ("[%i] %s"):format(
                                                option,
                                                l10n { "GearDisplayName", gear.identifier }.altvalue
                                            )
                                        ), responder, msgtypes = ChatMessageType.Private }
                                        local accountIds = moses.clone(faction:tryGetParticipatorsByKeyEvenIfNil())
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
                                    else
                                        chat.send { l10n "PrivateChooseGearFailure":format(
                                            l10n { "GearDisplayName", gear.identifier }.altvalue
                                        ), responder, msgtypes = ChatMessageType.Private }
                                        self._promptedChoosingGear[clientCharacter] = nil
                                    end
                                end

                                if self.autoParticipateWhenNoChoices and job.gearCount == 1 then
                                    chooseCallback(0, client)
                                else
                                    dialog.prompt(l10n "PromptChooseGear".value, gearOptions, client, chooseCallback, nil, false)
                                end

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
                                local clientAccountId = moses.invert(self._characterWaitChooseGearBy)[dead]
                                if clientAccountId then
                                    self._assignedJob[clientAccountId] = nil
                                    self._promptedAssigningJob[clientAccountId] = nil
                                    self._waitRespawn[clientAccountId] = true
                                    local client = DFC.GetClientByAccountId(clientAccountId)
                                    if client then
                                        if client.Character == dead then
                                            client.SetClientCharacter(nil)
                                        end
                                    end
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
                local clientAccountId = client.AccountId.StringRepresentation
                self._promptedJoiningFaction[clientAccountId] = nil
                self._promptedAssigningJob[clientAccountId] = nil
                local character = self._characterWaitChooseGearBy[clientAccountId]
                if character then
                    self._promptedChoosingGear[character] = nil
                end
            end
        )
    end
end
