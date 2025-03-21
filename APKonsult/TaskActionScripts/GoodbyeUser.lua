local guild = await(client.GetGuildAsync("id:1348833304811540490"))
door = await(client.GetChannelAsync("id:1348833305943867445"))

function eventFired()
    goodbyeMessage = eventArgs.Member.Mention .. " has left the server!"
    await(door.SendMessageAsync(goodbyeMessage));
end

keepAlive()