local t_insert, t_remove = table.insert, table.remove

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

---@deprecated
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

---@param accountId string
---@return Barotrauma.Networking.Client?
function DFC.GetClientByAccountId(accountId)
    for _, client in ipairs(Client.ClientList) do
        if client.AccountId.StringRepresentation == accountId then
            return client;
        end
    end
    return nil
end

---@deprecated
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

---@param accountIds string[]
---@return Barotrauma.Networking.Client[]
function DFC.GetClientListByAccountIds(accountIds)
    local clientList = {}
    local clientsToInclude = Client.ClientList
    local count = #clientsToInclude
    for _, accountId in ipairs(accountIds) do
        for i = count, 1, -1 do
            if clientsToInclude[i].AccountId.StringRepresentation == accountId then
                t_insert(clientList, t_remove(clientsToInclude, i))
                count = count - 1
                break
            end
        end
    end
    return clientList
end
