---@meta

DFC = {}

---@type dfc
DFC.Loaded = nil

---@param character Barotrauma.Character
---@param tags string[]
function DFC.AddCharacterTags(character, tags) end

---@param character Barotrauma.Character
---@return string[]
function DFC.GetCharacterTags(character) end

DFC.XMLExtensions = {}

---@param submarineElement System.Xml.Linq.XElement
---@param xpath string
---@return System.Xml.Linq.XElement[]
function DFC.XMLExtensions.XPathSelectElements(submarineElement, xpath) end



