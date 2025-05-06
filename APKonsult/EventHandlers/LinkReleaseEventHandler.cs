using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace APKonsult.EventHandlers;

public sealed partial class LinkReleaseEventHandler : IEventHandler<MessageCreatedEventArgs>
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S1075:URIs should not be hardcoded", Justification = "I don't care.")]
    private const string RELEASES_URL = "https://api.github.com/repos/Sombody101/APKognito/releases/tags";

    [GeneratedRegex(@"&((v|pd|d)(?<version>\d+.\d+.\d+(.\d+)?))")]
    private static partial Regex ReleaseRegex();

    private readonly HttpClient _httpClient;
    private readonly Dictionary<Version, string> _releaseCache = [];

    public LinkReleaseEventHandler(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task HandleEventAsync(DiscordClient sender, MessageCreatedEventArgs eventArgs)
    {
        if (eventArgs.Author.IsBot)
        {
            return;
        }

        List<string> releaseBlocks = [];
        foreach (GroupCollection groups in ReleaseRegex().Matches(eventArgs.Message.Content).Select(m => m.Groups))
        {
            if (!Version.TryParse(groups["version"].ValueSpan, out Version? releaseVersion))
            {
                continue;
            }
            else if (_releaseCache.TryGetValue(releaseVersion, out string? cachedLink))
            {
                releaseBlocks.Add(cachedLink);
                continue;
            }

            string issueUrl = $"{RELEASES_URL}/{groups[2].ValueSpan}{releaseVersion}";
            HttpResponseMessage responseMessage = await _httpClient.GetAsync(issueUrl);
            if (!responseMessage.IsSuccessStatusCode)
            {
                continue;
            }

            JsonDocument? json = await responseMessage.Content.ReadFromJsonAsync<JsonDocument>();
            if (json is null
                || !json.RootElement.TryGetProperty("html_url", out JsonElement htmlUrl)
                || !json.RootElement.TryGetProperty("name", out JsonElement title)
                || !json.RootElement.TryGetProperty("created_at", out JsonElement publishTime)
                || !json.RootElement.TryGetProperty("tag_name", out JsonElement tagName)
                || !json.RootElement.TryGetProperty("assets", out JsonElement assets))
            {
                continue;
            }

            if (assets.ValueKind is not JsonValueKind.Array || assets.GetArrayLength() is 0)
            {
                continue;
            }

            JsonElement firstAsset = assets[0];

            if (!firstAsset.TryGetProperty("browser_download_url", out JsonElement downloadUrl)
                || !firstAsset.TryGetProperty("name", out JsonElement downloadFileName)
                || !firstAsset.TryGetProperty("size", out JsonElement rawDownloadSize))
            {
                continue;
            }

            if (!DateTimeOffset.TryParse(publishTime.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTimeOffset utcDateTimeOffset))
            {
                continue;
            }

            StringBuilder buffer = new();

            buffer.Append("## ").AppendLine(title.GetString())
                .Append("GitHub release page: [").Append(tagName.GetString()).Append("](").Append(htmlUrl).AppendLine(")")
                .Append("Direct download: [").Append(downloadFileName).Append(" (").Append($"{(rawDownloadSize.GetUInt64() / 1024f / 1024f):0.00} MB").Append(")](").Append(downloadUrl).AppendLine(")")
                .AppendLine($"<t:{utcDateTimeOffset.ToUnixTimeSeconds()}:f>");

            string output = buffer.ToString();
            releaseBlocks.Add(output);
            _releaseCache[releaseVersion] = output;
        }

        releaseBlocks = releaseBlocks.Distinct().ToList();

        switch (releaseBlocks.Count)
        {
            case 0:
                return;

            case 1:
                {
                    string message = releaseBlocks[0];

                    var embed = new DiscordEmbedBuilder()
                        .WithDescription(message)
                        .WithColor();

                    _ = await eventArgs.Message.RespondAsync(embed);
                }
                break;

            default:
                {
                    StringBuilder builder = new();
                    foreach (string releaseBlock in releaseBlocks)
                    {
                        if ((builder.Length + releaseBlock.Length + 3) > 2000)
                        {
                            break;
                        }

                        _ = builder.AppendLine(releaseBlock);
                    }

                    var embed = new DiscordEmbedBuilder()
                        .WithDescription(builder.ToString())
                        .WithColor();

                    _ = await eventArgs.Message.RespondAsync(embed);
                }
                break;
        }
    }
}
