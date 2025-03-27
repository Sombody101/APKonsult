-- guild = await(client.GetGuildAsync("id:1348833304811540490"))
-- door = await(client.GetChannelAsync("id:1348833305943867445"))
-- botTesterRole = await(guild.GetRoleAsync("id:1348833304811540494"))
-- releaseChannelMention = await(client.GetChannelAsync("id:1348833305943867451")).Mention
-- debugChannelMention = await(client.GetChannelAsync("id:1348833305943867450")).Mention
function eventFired()
    local door = getChannel(eventArgs.Guild, "door")

    local botTesterRole = getRole(eventArgs.Guild, "bot tester")

    goodbyeMessage = "Hey there, " .. eventArgs.Member.Mention ..
                         "! Welcome to the Bot Tester server! 👋\n\n" ..
                         "We've got two APKonsult bot instances for you to try:\n" ..
                         "**Testing Ground:** <@:1348833305943867450> - This bot has the latest features but might be a little buggy, but is only active when the developer is debugging a feature.\n" ..
                         "**Stable Bot:** <@:1348833305943867451> - This bot is more stable but might have fewer features and not as up-to-date.\n" ..
                         "I've gone ahead and given you the `Bot Tester` role which allows you to send commands in these channels, as well as extra information when you use the `help` command!";

    await(eventArgs.Member.GrantRoleAsync(botTesterRole))
    await(door.SendMessageAsync(goodbyeMessage));
end

keepAlive()
