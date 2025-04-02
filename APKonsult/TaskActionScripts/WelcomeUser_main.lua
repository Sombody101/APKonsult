function eventFired()
    local door = getChannel(eventArgs.Guild, "door")

    welcomeMessage = "Hey there, " .. eventArgs.Member.Mention ..
                         "! Welcome to the APKognito support server! 👋\n\n" ..
                         "If you have an issue or bug you'd like to report, please go to <#1350194997701640325> and create an issue. " ..
                         "If you have a small question about APKognito, then you can ask that in <#1352549048422240327>!\n" ..
                         "Note: The Quick Support thread should be used for *quick* answers, hence its name."

    await(door.SendMessageAsync(welcomeMessage));
end

keepAlive()
