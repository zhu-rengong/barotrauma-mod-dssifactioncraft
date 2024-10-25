if not (SERVER or Game.IsSingleplayer) then return end

local log = require "utilbelt.logger" ("DFC")
local l10n = require "utilbelt.l10n"

DFC.Path = ...

l10n.loadlangs(DFC.Path .. "/Lua/DSSI Faction Craft/localizations")

require "DSSI Faction Craft.classes.taggable"
require "DSSI Faction Craft.classes.participatory"
require "DSSI Faction Craft.classes.spawnpointset"
require "DSSI Faction Craft.classes.gear"
require "DSSI Faction Craft.classes.job"
require "DSSI Faction Craft.classes.faction"
require "DSSI Faction Craft.classes.dfc"

require "DSSI Faction Craft.utils"

Hook.Add("dssi.inject.after", "DFC",
    ---@param submarineInfo Barotrauma.SubmarineInfo
    function(submarineInfo)
        local dfc = DFC.Loaded
        if dfc == nil then
            local dfc_initializer = DFC.XMLExtensions.XPathSelectElements(
                submarineInfo.SubmarineElement, [[ //Item[@identifier="dfc_initializer"]/DfcInitializer ]])[1]
            if dfc_initializer then
                dfc = New 'dfc' ()
                for attribute in dfc_initializer.Attributes() do
                    local attributeName = attribute.Name.LocalName:lower()
                    local attributeValue = attribute.Value:lower()
                    if attributeName == "allowrespawn" then
                        dfc.allowRespawn = attributeValue == "true"
                    elseif attributeName == "allowmidroundjoin" then
                        dfc.allowMidRoundJoin = attributeValue == "true"
                    end
                end
                dfc:initialize()
            end
        end
    end
)

if SERVER then
    Hook.Add("item.readPropertyChange", "DFC",
        ---@param item Barotrauma.Item
        function(item)
            if item.HasTag("dfc_mapdevtool") then
                return true;
            end
        end)
end
