using DSharpPlus.Commands;
using Newtonsoft.Json;
using Serilog;

namespace APKonsult.Configuration;

internal class ConfigManager
{
    public static readonly ConfigManager Manager = new();

    private const string BOT_CONFIG_PATH = $"./configs/bot-config.json";

    private readonly JsonSerializer _serializer;

    /// <summary>
    /// Configurations specific to the functionality of the bot
    /// </summary>
    public BotConfigModel BotConfig { get; private set; } = null!;

    /// <summary>
    /// Initializes the <see cref="JsonSerializer"/> and loads all configurations.
    /// </summary>
    public ConfigManager()
    {
        _serializer = new();
        LoadBotConfig().Wait();
    }

    /// <summary>
    /// Loads <see cref="BotConfig"/> from file.
    /// </summary>
    /// <returns></returns>
    public async Task LoadBotConfig()
    {
        BotConfigModel config = await LoadConfig(BOT_CONFIG_PATH);

        if (Equals(config, null))
        {
            Environment.Exit(1);
        }

        BotConfig = config;
    }

    /// <summary>
    /// Saves <see cref="BotConfig"/> to file. (Defaults to <see cref="defaultBotConfigPath"/>)
    /// </summary>
    /// <param name="path"></param>
    /// <param name="ctx"></param>
    /// <returns></returns>
    public async Task SaveBotConfig(CommandContext? ctx = null)
    {
        if (BotConfig is null)
        {
            const string errorMessage = "Bot storage is null : Aborting save to prevent overwriting config";

            if (ctx is not null)
            {
                await ctx.RespondAsync(errorMessage);
            }

            Log.Information(errorMessage);
            return;
        }

        SaveConfig(BOT_CONFIG_PATH, BotConfig);
    }

    public void SaveConfig(string path, BotConfigModel config)
    {
        try
        {
            using StreamWriter sw = new(path);
            using JsonTextWriter writer = new(sw);
            _serializer.Serialize(writer, config);

            Log.Information("Config saved to '{Path}'.", path);
        }
        catch (Exception ex)
        {
            Log.Information(ex, "Error saving config to: {Path}", path);
        }
    }

    public async Task<BotConfigModel> LoadConfig(string path)
    {
        if (!File.Exists(path))
        {
            Log.Information("No config file found at '{Path}'. Creating one with default values.", path);

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            await File.WriteAllTextAsync(path, $"{{{Environment.NewLine}}}");
            return new BotConfigModel();
        }

        try
        {
            using StreamReader sr = new(path);
            using JsonTextReader reader = new(sr);
            BotConfigModel? config = _serializer.Deserialize<BotConfigModel>(reader);

            if (Equals(config, null))
            {
                Log.Information($"Failed to deserialize configuration file to type {nameof(BotConfigModel)} from: {{Path}}", path);
            }
            else
            {
                return config;
            }
        }
        catch (Exception ex)
        {
            Log.Information(ex, "Error loading config.");
        }

        return new();
    }
}