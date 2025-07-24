function eventFired()
    local door = getChannel(eventArgs.Guild, "door")
    local goodbyeMessage = eventArgs.Member.Mention .. " has left the server!"
    await(door.SendMessageAsync(goodbyeMessage));
end

keepAlive()