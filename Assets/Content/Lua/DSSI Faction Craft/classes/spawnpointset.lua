---@class dfc.spawnpointset: dfc.inner
---@field public identifier string
---@field private caches Barotrauma.WayPoint[]
---@field private teamID? Barotrauma.CharacterTeamType
---@field private spawnType? Barotrauma.SpawnType
---@field private assignedJob? Barotrauma.JobPrefab
---@field private tag? Barotrauma.Identifier
---@field private existAny boolean
---@field private count integer
---@overload fun(identifier: string, tag?: string, spawnType?: Barotrauma.SpawnType, assignedJob?: string, teamID?: Barotrauma.CharacterTeamType):self
local m = Class 'dfc.spawnpointset'

---@class dfc.spawnpointset : dfc.taggable
Extends('dfc.spawnpointset', 'dfc.taggable')

---@param identifier string
---@param tag? string
---@param spawnType? Barotrauma.SpawnType
---@param assignedJob? string
---@param teamID? Barotrauma.CharacterTeamType
function m:__init(identifier, tag, spawnType, assignedJob, teamID)
    Class 'dfc.taggable'.__init(self)
    self.identifier = identifier
    self.tag = tag and Identifier(tag)
    self.spawnType = spawnType
    self.assignedJob = assignedJob and JobPrefab.Prefabs[assignedJob]
    self.teamID = teamID
end

---@return Barotrauma.WayPoint[]
function m:getTargets()
    local targets = self.caches
    if targets then return targets end
    targets = moses.filter(WayPoint.WayPointList, function(wayPoint)
        if self.teamID and (wayPoint.Submarine == nil or wayPoint.Submarine.TeamID ~= self.teamID) then return false end
        if self.spawnType and wayPoint.SpawnType ~= self.spawnType then return false end
        if self.assignedJob and wayPoint.AssignedJob ~= self.assignedJob then return false end
        if self.tag and not moses.include(moses.tabulate(wayPoint.Tags), self.tag) then return false end
        return true
    end)
    self.count = #targets
    self.existAny = self.count > 0
    self.caches = targets
    return targets
end

---@return Barotrauma.WayPoint?
function m:getRandom()
    local targets = self:getTargets()
    if self.existAny then
        return targets[math.random(1, self.count)]
    end
end

function m:uncache()
    self.caches = nil
end
