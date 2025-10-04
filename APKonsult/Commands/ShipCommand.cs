using APKonsult.CommandChecks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands.Metadata;
using DSharpPlus.Entities;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace APKonsult.Commands;

public static class ShipCommand
{
    [Command("ship"),
        UserGuildInstallable,
        InteractionAllowedContexts(DiscordInteractionContextType.Guild, DiscordInteractionContextType.BotDM, DiscordInteractionContextType.PrivateChannel)]
    public static async ValueTask ShipAsync(CommandContext ctx, DiscordUser user1, DiscordUser user2, bool showHashes = false)
    {
        await ctx.DeferResponseAsync();

        Stopwatch sw = Stopwatch.StartNew();

        string asparagus1Hash = user1.Username.GetHashString();
        string asparagus2Hash = user2.Username.GetHashString();

        string avatarHash = XorHashes(user1.AvatarHash, user2.AvatarHash).GetHashString();
        string bannerHash = XorHashes(user1.BannerHash ?? "$NULL", user2.BannerHash ?? "$NULL").GetHashString();

        string combinedHash = XorHashes(avatarHash, bannerHash).GetHashString();
        string nameHash = XorHashes(asparagus1Hash, asparagus2Hash).GetHashString();

        string finalHash = XorHashes(combinedHash, nameHash).GetHashString();
        double percent = finalHash.HashToRange();

        string message = percent switch
        {
            > 95 => "Get to fuckin already",
            > 80 => "That's AMAZING!",
            > 70 => "That's great!",
            > 60 => "That could be better.",
            > 50 => "That's okay.",
            > 40 => "It might not be meant to be.",
            > 30 => "Good luck with that one!",
            > 15 => "Just call it off.",
            _ => "lol",
        };

        sw.Stop();

        DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
            .WithTitle($"Compatibility between {user1.Username} and {user2.Username}")
            .WithColor(Shared.DefaultEmbedColor)
            .AddField($"{percent:n0}% compatibility!", message)
            .WithFooter($"Calculation Took {sw.ElapsedMilliseconds}ms\nFinal comparison hash: {finalHash}");

        if (showHashes)
        {
            _ = embed.AddField(nameof(asparagus1Hash), $"{asparagus1Hash} ({asparagus1Hash.HashToRange()}%)")
                        .AddField(nameof(asparagus2Hash), $"{asparagus2Hash} ({asparagus2Hash.HashToRange()}%)")
                        .AddField(nameof(avatarHash), $"{avatarHash} ({avatarHash.HashToRange()}%)")
                        .AddField(nameof(bannerHash), $"{bannerHash} ({bannerHash.HashToRange()}%)")
                        .AddField(nameof(combinedHash), $"{combinedHash} ({combinedHash.HashToRange()}%)")
                        .AddField(nameof(nameHash), $"{nameHash} ({nameHash.HashToRange()}%)")
                        .AddField(nameof(finalHash), $"{finalHash} ({finalHash.HashToRange()}%)");
        }

        await ctx.RespondAsync(embed: embed);
    }

    private const double scalingFactor = 10000.0;

    private static double HashToRange(this string sha256Hash)
    {
        UInt128 l = UInt128.Parse(sha256Hash, NumberStyles.HexNumber);
        // Square root operation spreads the values more
        return +(Math.Sqrt((double)l) * scalingFactor % 101);
    }

    private static byte[] GetHash(string inputString)
    {
        return SHA256.HashData(Encoding.UTF8.GetBytes(inputString)).Skip(16).ToArray();
    }

    private static string GetHashString(this string inputString)
    {
        StringBuilder sb = new();
        foreach (byte b in GetHash(inputString))
        {
            _ = sb.Append(b.ToString("X2"));
        }

        return sb.ToString();
    }

    private static string XorHashes(string hash1, string hash2)
    {
        int length = hash1.Length;
        StringBuilder result = new(length);

        for (int i = 0; i < length; i++)
        {
            byte b1 = Convert.ToByte(hash1[i]);
            byte b2 = Convert.ToByte(hash2[i]);
            byte xorResult = (byte)(b1 ^ b2);

            _ = result.Append(xorResult.ToString("X2"));
        }

        return result.ToString();
    }
}