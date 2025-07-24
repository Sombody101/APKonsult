using DSharpPlus.Commands;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.ComponentModel;

namespace APKonsult.Commands;

#pragma warning disable S1075 // URIs should not be hardcoded

public sealed class GitsAndShiggles(HttpClient _httpClient, ILogger<GitsAndShiggles> _logger)
{
    [Command("chucknorris"), Description("Get a random Chuck Norris fact")]
    public async Task GetChuckNorrisFactAsync(CommandContext ctx)
    {
        const string CHUCK_NORRIS_API_URL = "https://api.chucknorris.io/jokes/random";
        const string NO_CHUCK_FACT_MESSAGE = "Failed to get your Chuck Norris fact!";

        try
        {
            var fact = await GetAndParseAsync<ChuckNorrisFactModel>(CHUCK_NORRIS_API_URL);

            if (fact is null)
            {
                await ctx.RespondAsync(NO_CHUCK_FACT_MESSAGE);
                return;
            }

            var embed = new DiscordEmbedBuilder()
                .WithDescription(fact.Value)
                .WithImageUrl(fact.IconUrl)
                .WithFooter($"ID: {fact.Id}")
                .WithDefaultColor();

            await ctx.RespondAsync(embed);
        }
        catch (Exception ex)
        {
            await ctx.RespondAsync(NO_CHUCK_FACT_MESSAGE);
            _logger.LogError(ex, "Chuck norris fact failed.");
        }
    }

    [Command("dadjoke"), Description("Get a random dad joke")]
    public async Task GetDataJokeAsync(CommandContext ctx)
    {
        const string DAD_JOKE_URL = "https://icanhazdadjoke.com/";
        const string NO_DAD_JOKE_MESSAGE = "Failed to get your dad joke!";

        try
        {
            var joke = await GetAndParseAsync<DadJokeModel>(DAD_JOKE_URL);

            if (joke.Equals(default))
            {
                await ctx.RespondAsync(NO_DAD_JOKE_MESSAGE);
                return;
            }

            var embed = new DiscordEmbedBuilder()
                .WithDescription(joke.Joke)
                .WithFooter(joke.Id)
                .WithDefaultColor();

            await ctx.RespondAsync(embed);
        }
        catch (Exception ex)
        {
            await ctx.RespondAsync(NO_DAD_JOKE_MESSAGE);
            _logger.LogError(ex, "Dad joke failed.");
        }
    }

    [Command("comic"), Description("Gets the daily comic")]
    public async Task GetDailyComicAsync(CommandContext ctx)
    {
        const string COMIC_URL = "https://xkcd.com/info.0.json";
        const string NO_COMIC_MESSAGE = "Failed to get your daily comic!";

        try
        {
            var comic = await GetAndParseAsync<DailyComicModel>(COMIC_URL);

            if (comic is null)
            {
                await ctx.RespondAsync(NO_COMIC_MESSAGE);
                return;
            }

            var embed = new DiscordEmbedBuilder()
                .WithTitle(comic.Title)
                .WithImageUrl(comic.ImageUrl)
                .WithDefaultColor()
                .WithFooter($"#{comic.Number}, {comic.Day}/{comic.Month}/{comic.Year}");

            await ctx.RespondAsync(embed);
        }
        catch (Exception ex)
        {
            await ctx.RespondAsync(NO_COMIC_MESSAGE);
            _logger.LogError(ex, "Daily comic failed.");
        }
    }

    private async Task<T?> GetAndParseAsync<T>(string url)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Accept", "application/json");

        _logger.LogDebug("Headers: {Headers}", request.Headers);

        using var returned = await _httpClient.SendAsync(request);

        if (returned is null || !returned.IsSuccessStatusCode)
        {
            _logger.LogWarning("Failed to get payload for {Name}; Code:{StatusCode}", typeof(T).Name, returned?.StatusCode);
            return default;
        }

        T? fact = JsonConvert.DeserializeObject<T>(await returned.Content.ReadAsStringAsync());

        if (fact is null)
        {
            _logger.LogWarning("Failed to parse payload for {Name}; Content:{Content}", typeof(T).Name, returned.Content);
            return default;
        }

        return fact;
    }

    private sealed record ChuckNorrisFactModel
    {
        [JsonProperty("created_at")]
        public string CreatedAt { get; init; } = string.Empty;

        [JsonProperty("icon_url")]
        public string IconUrl { get; init; } = string.Empty;

        [JsonProperty("id")]
        public string Id { get; init; } = string.Empty;

        [JsonProperty("url")]
        public string Url { get; init; } = string.Empty;

        [JsonProperty("value")]
        public string Value { get; init; } = string.Empty;
    }

    private readonly record struct DadJokeModel
    {
        [JsonProperty("id")]
        public string Id { get; init; }

        [JsonProperty("joke")]
        public string Joke { get; init; }
    }

    private sealed record DailyComicModel
    {
        [JsonProperty("month")]
        public string Month { get; init; } = string.Empty;

        [JsonProperty("num")]
        public int Number { get; init; }

        [JsonProperty("link")]
        public string Link { get; init; } = string.Empty;

        [JsonProperty("year")]
        public string Year { get; init; } = string.Empty;

        [JsonProperty("news")]
        public string News { get; init; } = string.Empty;

        [JsonProperty("safe_title")]
        public string SafeTitle { get; init; } = string.Empty;

        [JsonProperty("transcript")]
        public string Transcript { get; init; } = string.Empty;

        // Likely will never be used
        [JsonProperty("alt")]
        public string Alt { get; init; } = string.Empty;

        [JsonProperty("img")]
        public string ImageUrl { get; init; } = string.Empty;

        [JsonProperty("title")]
        public string Title { get; init; } = string.Empty;

        [JsonProperty("day")]
        public string Day { get; init; } = string.Empty;
    }
}
