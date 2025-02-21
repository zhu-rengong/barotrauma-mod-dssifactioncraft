local l10n = require "utilbelt.l10n"

---@class dfc.participatory
---@field _participators { [unknown]:unknown[] }
---@field _tickets { [unknown]:integer }
---@overload fun():self
local m = Class 'dfc.participatory'

m.participantTickets = -1
m.participantNumberLimit = -1
m.participantWeight = 0

local DefaultParticipateKey = {}

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
    key = key == nil and DefaultParticipateKey or key
    if self._participators[key] == nil then
        self._participators[key] = {}
    end
    return self._participators[key]
end


---@param participator unknown
---@param key? unknown
function m:addParticipator(participator, key)
    local participators = self:tryGetParticipatorsByKeyEvenIfNil(key)
    table.insert(participators, participator)
end

---@param participator unknown
---@param key? unknown
function m:tryRemoveParticipatorByKey(participator, key)
    local participators = self:tryGetParticipatorsByKeyEvenIfNil(key)
    for i = #participators, 1, -1 do
        if participators[i] == participator then
            table.remove(participators, i)
            return true
        end
    end
    return false
end

---@param participator unknown
function m:tryRemoveParticipator(participator)
    for _, participators in pairs(self._participators) do
        for i = #participators, 1, -1 do
            if participators[i] == participator then
                table.remove(participators, i)
                return true
            end
        end
    end
    return false
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
    key = key or DefaultParticipateKey
    local participators = self:tryGetParticipatorsByKeyEvenIfNil(key)
    return #participators
end

---@param key? unknown
---@return number
function m:countTickets(key)
    key = key or DefaultParticipateKey
    if self._tickets[key] == nil then
        self._tickets[key] = self.participantTickets
    end
    return self._tickets[key]
end

---@param number integer
---@param key? unknown
function m:modifyTickets(number, key)
    key = key or DefaultParticipateKey
    local tickets = self:countTickets(key)
    self._tickets[key] = tickets + number
end

---@param participatoryGroup { [unknown]:dfc.participatory }
---@param key? unknown
---@return number
function m:countGroupParticipators(participatoryGroup, key)
    return moses.reduce(participatoryGroup, function(state, current)
        return state + current:countParticipators(key)
    end, 0)
end

---@param statisticalName? string
---@param participatoryGroup { [unknown]:dfc.participatory }
---@param key? unknown
---@vararg string # keys for finding localized text
---@return string
function m:statistic(statisticalName, participatoryGroup, key)
    local number = self:countParticipators(key)
    local tickets = self:countTickets(key)
    local total = self:countGroupParticipators(participatoryGroup, key)
    local weights = self:calculateGroupWeights(participatoryGroup)
    local ratio = self.participantWeight / weights
    local proportion = number / total
    return l10n { "ParticipatoryStatistic" }:format(
        statisticalName or l10n { "Unknown" }.value,
        number,
        self.participantNumberLimit >= 0 and self.participantNumberLimit or '∞',
        tickets >= 0 and tickets or '∞',
        total > 0 and ("%.0f"):format(proportion * 100) or '0',
        (total == 0 or proportion <= ratio) and "≤" or ">",
        self.participantWeight, weights
    )
end

---@param participatoryGroup { [unknown]:dfc.participatory }
---@param key? unknown
---@return boolean
function m:participatory(participatoryGroup, key)
    key = key or DefaultParticipateKey
    if self:countTickets(key) == 0 then return false end
    local number = self:countParticipators(key)
    if self.participantNumberLimit >= 0 and number >= self.participantNumberLimit then return false end
    local weights = self:calculateGroupWeights(participatoryGroup)
    if weights > 0 then
        local ratio = self.participantWeight / weights
        local total = self:countGroupParticipators(participatoryGroup, key)
        if ratio > 0 and total > 0 then
            local proportion = number / total
            if proportion > ratio then return false end
        end
    end
    return true
end
