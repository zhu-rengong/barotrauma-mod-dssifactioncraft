---@class dfc.job: dfc.inner
---@field _participators { [dfc.faction]:Barotrauma.Character[] }
---@field identifier string
---@field name string
---@field onAssigned? fun(character:Barotrauma.Character)
---@field liveConsumption integer
---@field sort integer
---@field shouldSortGears boolean
---@field notifyTeammates boolean
---@field inhertCharacterInfo boolean
---@field gears { [string]:dfc.gear }
---@field sortedGears dfc.gear[]
---@field existAnyGear boolean
---@field gearCount integer
---@field jobPrefab? Barotrauma.JobPrefab
---@field characterPrefab? Barotrauma.CharacterPrefab
---@field human boolean
---@field existAnySpawnPointSet boolean
---@field spawnPointSets dfc.spawnpointset[]
---@field spawnPointSetWeights number[]
---@overload fun(identifier: string, name: string, onAssigned?: fun(character:Barotrauma.Character), liveConsumption?: integer):self
local m = Class 'dfc.job'

---@class dfc.job : dfc.taggable, dfc.participatory
Extends('dfc.job', 'dfc.taggable', 'dfc.participatory')

---@param identifier string
---@param name string
---@param onAssigned? fun(character:Barotrauma.Character)
---@param liveConsumption? integer
function m:__init(identifier, name, onAssigned, liveConsumption)
    Class 'dfc.taggable'.__init(self)
    Class 'dfc.participatory'.__init(self)
    self.identifier = identifier
    self.onAssigned = onAssigned
    self.liveConsumption = liveConsumption or 1
    self.sort = self.sort or 0
    self.notifyTeammates = self.notifyTeammates == nil and true or self.notifyTeammates
    self.inhertCharacterInfo = self.inhertCharacterInfo == nil and false or self.inhertCharacterInfo
    self.gears = {}
    self.existAnyGear = false
    self.gearCount = 0
    if JobPrefab.Prefabs.ContainsKey(name) then
        self.jobPrefab = JobPrefab.Prefabs[name]
    elseif CharacterPrefab.Prefabs.ContainsKey(name) then
        self.characterPrefab = CharacterPrefab.Prefabs[name]
    end
    self.human = self.jobPrefab or Identifier(name) == CharacterPrefab.HumanSpeciesName
    self.spawnPointSets = {}
    self.spawnPointSetWeights = {}
end

---@param identifier string
function m:addGear(identifier)
    local gear = self.dfc.gears[identifier]
    if gear ~= nil then
        self.gears[identifier] = gear
        self.existAnyGear = true
        self.gearCount = moses.count(self.gears)
        self.shouldSortGears = true
    end
    return self
end

---@param list string[]
function m:addGears(list)
    for _, identifier in ipairs(list) do
        self:addGear(identifier)
    end
    return self
end

---@param identifier string
function m:removeGear(identifier)
    self.gears[identifier] = nil
    self.existAnyGear = next(self.gears) ~= nil
    self.gearCount = moses.count(self.gears)
    self.shouldSortGears = true
    return self
end

---@param list string[]
function m:removeGears(list)
    for _, identifier in ipairs(list) do
        self:removeGear(identifier)
    end
    return self
end

function m:trySortGears()
    if self.shouldSortGears then
        local gears = moses.values(self.gears)
        self.sortedGears = moses.sort(gears, function(g1, g2)
            if g1.sort == g2.sort then
                return g1.identifier < g2.identifier
            else
                return g1.sort < g2.sort
            end
        end)
        self.shouldSortGears = false
    end
end

---@param identifier string
---@param weight? number # default by 1.0
function m:addSpawnPointSet(identifier, weight)
    local spawnPointSet = self.dfc.spawnPointSets[identifier]
    if spawnPointSet ~= nil then
        local length = #self.spawnPointSets
        self.spawnPointSets[length + 1] = spawnPointSet
        self.spawnPointSetWeights[length + 1] = weight or 1.0
        self.existAnySpawnPointSet = true
    end
    return self
end
