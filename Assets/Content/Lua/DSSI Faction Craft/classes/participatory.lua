local l10n = require "utilbelt.l10n"

---@class dfc.participatory
---@field _participators { [unknown]:unknown[] }
---@field _tickets { [unknown]:integer }
---@overload fun():self
local m = Class 'dfc.participatory'

m.participantTickets = -1
m.participantNumberLimit = -1
m.participantWeight = 0

function m:__init()
    self:resetParticipatoryDatas()
end

function m:resetParticipatoryDatas()
    self._participators = {}
    self._tickets = {}
end

---@param key? unknown
---@return unknown[]
function m:tryGetParticipatorsByKeyEvenIfNil(key)
    if self._participators[key] == nil then
        self._participators[key] = {}
    end
    return self._participators[key]
end


---@param key? unknown
---@param participator unknown
function m:addParticipator(key, participator)
    local participators = self:tryGetParticipatorsByKeyEvenIfNil(key)
    table.insert(participators, participator)
end

---@param participatoryGroup { [unknown]:dfc.participatory }
---@return number
function m:calculateGroupWeights(participatoryGroup)
    return moses.reduce(participatoryGroup, function(state, current)
        return state + current.participantWeight
    end, 0)
end

---@param key? unknown
---@return number
function m:countParticipators(key)
    local participators = key and self._participators[key]
    return participators and #participators or 0
end

---@param key? unknown
---@return number
function m:countTickets(key)
    if key == nil then return 0 end
    if self._tickets[key] == nil then
        self._tickets[key] = self.participantTickets
    end
    return self._tickets[key]
end

---@param key? unknown
---@param number unknown
function m:modifyTickets(key, number)
    if key == nil then return end
    local tickets = self:countTickets(key)
    self._tickets[key] = tickets + number
end

---@param key? unknown
---@param participatoryGroup { [unknown]:dfc.participatory }
---@return number
function m:countGroupParticipators(key, participatoryGroup)
    return moses.reduce(participatoryGroup, function(state, current)
        return state + current:countParticipators(key)
    end, 0)
end

---@param key? unknown
---@param participatoryGroup { [unknown]:dfc.participatory }
---@vararg string # keys for finding localized text
---@return string
function m:statistic(participatoryGroup, key, ...)
    local number = self:countParticipators(key)
    local tickets = self:countTickets(key)
    local total = self:countGroupParticipators(key, participatoryGroup)
    local weights = self:calculateGroupWeights(participatoryGroup)
    local ratio = self.participantWeight / weights
    local proportion = number / total
    return l10n { "ParticipatoryStatistic" }:format(
        l10n { ..., self.identifier }.altvalue,
        number,
        self.participantNumberLimit >= 0 and self.participantNumberLimit or '∞',
        tickets >= 0 and tickets or '∞',
        total > 0 and ("%.0f"):format(proportion * 100) or '0',
        (total == 0 or proportion <= ratio) and "≤" or ">",
        self.participantWeight, weights
    )
end

---@param participatoryGroup { [unknown]:dfc.participatory }
---@param key? unknown # default: '_'
---@return boolean
function m:participatory(participatoryGroup, key)
    key = key or '_'
    if self:countTickets(key) == 0 then return false end
    local number = self:countParticipators(key)
    if self.participantNumberLimit >= 0 and number >= self.participantNumberLimit then return false end
    local weights = self:calculateGroupWeights(participatoryGroup)
    if weights > 0 then
        local ratio = self.participantWeight / weights
        local total = self:countGroupParticipators(key, participatoryGroup)
        if ratio > 0 and total > 0 then
            local proportion = number / total
            if proportion > ratio then return false end
        end
    end
    return true
end
