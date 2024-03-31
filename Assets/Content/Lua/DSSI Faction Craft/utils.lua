---@param character Barotrauma.Character
---@return Barotrauma.Networking.Client?
function DFC.FindOwnerClientByCharacter(character)
    if character then
        for _, client in ipairs(Client.ClientList) do
            if character.IsClientOwner(client) then
                return client
            end
        end
    end
end

---@param steamID string
---@return Barotrauma.Networking.Client?
function DFC.GetConnectedClientBySteamID(steamID)
    for _, client in ipairs(Client.ClientList) do
        if client.SteamID == steamID then
            return client;
        end
    end
    return nil
end

---@param steamIDs string[]
---@return Barotrauma.Networking.Client[]
function DFC.GetConnectedClientListBySteamIDs(steamIDs)
    local connectedClients = {}
    for _, client in ipairs(Client.ClientList) do
        for _, steamID in ipairs(steamIDs) do
            if client.SteamID == steamID then
                table.insert(connectedClients, client)
                break
            end
        end
    end
    return connectedClients
end
