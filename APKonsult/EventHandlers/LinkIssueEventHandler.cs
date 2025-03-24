using APKonsult.Configuration;
using DSharpPlus;
using DSharpPlus.EventArgs;
using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace APKonsult.EventHandlers;

public sealed partial class LinkIssueEventHandler : IEventHandler<MessageCreatedEventArgs>
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S1075:URIs should not be hardcoded", Justification = "I don't care.")]
    private const string ISSUES_URL = "https://api.github.com/repos/Sombody101/APKognito/issues";

    [GeneratedRegex("##(\\d+)")]
    private static partial Regex IssueRegex();

    private readonly HttpClient _httpClient;
    private readonly Dictionary<uint, string> _issueCache = [];

    public LinkIssueEventHandler(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task HandleEventAsync(DiscordClient sender, MessageCreatedEventArgs eventArgs)
    {
        if (eventArgs.Author.IsBot)
        {
            return;
        }

        List<string> issueLinks = [];
        foreach (Match match in IssueRegex().Matches(eventArgs.Message.Content))
        {
            if (!uint.TryParse(match.Groups[1].ValueSpan, out uint issueNumber) || issueNumber == 0)
            {
                continue;
            }
            else if (_issueCache.TryGetValue(issueNumber, out string? cachedLink))
            {
                issueLinks.Add(cachedLink);
                continue;
            }

            string issueUrl = $"{ISSUES_URL}/{issueNumber.ToString(CultureInfo.InvariantCulture)}";
            HttpResponseMessage responseMessage = await _httpClient.GetAsync(issueUrl);
            if (!responseMessage.IsSuccessStatusCode)
            {
                continue;
            }

            JsonDocument? json = await responseMessage.Content.ReadFromJsonAsync<JsonDocument>();
            if (json is null 
                || !json.RootElement.TryGetProperty("html_url", out JsonElement url) 
                || !json.RootElement.TryGetProperty("title", out JsonElement title) 
                || !json.RootElement.TryGetProperty("user", out JsonElement user) 
                || !user.TryGetProperty("login", out JsonElement login))
            {
                continue;
            }

            string linkText = json.RootElement.TryGetProperty("pull_request", out _)
                ? $"Pull Request #{issueNumber}: [{title.GetString()}](<{url.GetString()}>) - {login.GetString()}"
                : $"Issue #{issueNumber}: [{title.GetString()}](<{url.GetString()}>) - {login.GetString()}";

            issueLinks.Add(linkText);
            _issueCache[issueNumber] = linkText;
        }

        issueLinks = issueLinks.Distinct().ToList();
        if (issueLinks.Count == 0)
        {
            return;
        }
        else if (issueLinks.Count == 1)
        {
            await eventArgs.Message.RespondAsync(issueLinks[0]);
        }
        else
        {
            StringBuilder builder = new();
            while (issueLinks.Count != 0)
            {
                int issuesListed = 0;
                foreach (string issueLink in issueLinks)
                {
                    if ((builder.Length + issueLink.Length + 3) > 2000)
                    {
                        break;
                    }

                    builder.AppendLine($"\\- {issueLink}");
                    issuesListed++;
                }

                await eventArgs.Message.RespondAsync(builder.ToString());
                builder.Clear();
                issueLinks.RemoveRange(0, issuesListed);
            }
        }
    }
}
