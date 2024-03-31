---@class dfc.taggable
---@field public characterTags? string[]
---@overload fun():self
local m = Class 'dfc.taggable'

function m:__init()
    self.characterTags = {}
end

---@param character Barotrauma.Character
function m:addCharacterTagsFor(character)
    DFC.AddCharacterTags(character, self.characterTags)
end
