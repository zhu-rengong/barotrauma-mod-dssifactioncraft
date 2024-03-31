---@class dfc.gear: dfc.inner
---@field _participators { [dfc.faction]:Barotrauma.Character[] }
---@field identifier string
---@field action fun(character:Barotrauma.Character)
---@field sort integer
---@field notifyTeammates boolean
---@overload fun(identifier: string, action?: fun(character:Barotrauma.Character)):self
local m = Class 'dfc.gear'

---@class dfc.gear : dfc.taggable, dfc.participatory
Extends('dfc.gear', 'dfc.taggable', 'dfc.participatory')

---@param identifier string
---@param action? fun(character:Barotrauma.Character)
function m:__init(identifier, action)
    Class 'dfc.taggable'.__init(self)
    Class 'dfc.participatory'.__init(self)
    self.identifier = identifier
    self.action = action
    self.sort = self.sort or 0
    self.notifyTeammates = self.notifyTeammates == nil and true or self.notifyTeammates
end
