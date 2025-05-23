---@class dfc.faction: dfc.inner
---@field _participators { [string]:string[] } # Client.AccountId
---@field identifier Barotrauma.CharacterTeamType
---@field teamID Barotrauma.CharacterTeamType
---@field maxLives integer
---@field onJoined? fun(character:Barotrauma.Character)
---@field sort integer
---@field shouldSortJobs boolean
---@field notifyTeammates boolean
---@field jobs { [string]:dfc.job }
---@field sortedJobs dfc.job[]
---@field existAnyJob boolean
---@field jobCount integer
---@field allowRespawn boolean
---@field respawnIntervalMultiplier number
---@field getRespawnLimitPerTime fun(identifier: string, dead: integer, total: integer):integer
---@overload fun(identifier: string, teamID: Barotrauma.CharacterTeamType, maxLives?: integer, onJoined?: fun(character: Barotrauma.Character)):self
local m = Class 'dfc.faction'

---@class dfc.faction : dfc.taggable, dfc.participatory
Extends('dfc.faction', 'dfc.taggable', 'dfc.participatory')

---@param identifier string
---@param teamID Barotrauma.CharacterTeamType
---@param maxLives? integer
---@param onJoined? fun(character:Barotrauma.Character)
function m:__init(identifier, teamID, maxLives, onJoined)
    Class 'dfc.taggable'.__init(self)
    Class 'dfc.participatory'.__init(self)
    self.identifier = identifier
    self.teamID = teamID
    self.maxLives = maxLives or 100
    self.onJoined = onJoined
    self.sort = self.sort or 0
    self.notifyTeammates = true
    self.jobs = {}
    self.sortedJobs = {}
    self.existAnyJob = false
    self.jobCount = 0
    self.allowRespawn = true
    self.respawnIntervalMultiplier = 1.0
    function self.getRespawnLimitPerTime() return -1 end
end

---@param identifier string
---@param refreshCache? boolean
function m:addJob(identifier, refreshCache)
    local job = self.dfc.jobs[identifier]
    if job ~= nil then
        self.jobs[identifier] = job
        self.existAnyJob = true
        self.jobCount = moses.count(self.jobs)
        self.shouldSortJobs = true
        if self.dfc and (refreshCache == nil and true or refreshCache) then
            self.dfc:refreshCacheJobs(self)
        end
    end
    return self
end

---@param list string[]
function m:addJobs(list)
    for _, identifier in ipairs(list) do
        self:addJob(identifier)
    end
    return self
end

---@param identifier string
---@param refreshCache? boolean
function m:removeJob(identifier, refreshCache)
    self.jobs[identifier] = nil
    self.existAnyJob = next(self.jobs) ~= nil
    self.jobCount = moses.count(self.jobs)
    self.shouldSortJobs = true
    if self.dfc and (refreshCache == nil and true or refreshCache) then
        self.dfc:refreshCacheJobs(self)
    end
    return self
end

---@param list string[]
function m:removeJobs(list)
    for _, identifier in ipairs(list) do
        self:removeJob(identifier)
    end
    return self
end

function m:trySortJobs()
    if self.shouldSortJobs then
        local jobs = moses.values(self.jobs)
        self.sortedJobs = moses.sort(jobs, function(j1, j2)
            if j1.sort == j2.sort then
                return j1.identifier < j2.identifier
            else
                return j1.sort < j2.sort
            end
        end)
        self.shouldSortJobs = false
    end
end
