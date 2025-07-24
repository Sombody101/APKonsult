using APKonsult.Exceptions;
using DSharpPlus.Commands;
using Newtonsoft.Json;
using Serilog;

namespace APKonsult.Configuration;

internal class ConfigManager
{
    public static readonly ConfigManager Manager = new();

    private const string BOT_CONFIG_PATH = $"{ChannelIDs.FILE_ROOT}/configs/bot-config.json";
    private const string BOT_TOKENS_PATH = $"{ChannelIDs.FILE_ROOT}/configs/tokens.json";

    private readonly JsonSerializer _serializer;

    /// <summary>
    /// Configurations specific to the functionality of the bot
    /// </summary>
    public BotConfigModel BotConfig { get; private set; } = null!;

    /// <summary>
    /// Bot secrets.
    /// </summary>
    internal TokensModel Tokens { get; private set; } = null!;

    /// <summary>
    /// Initializes the <see cref="JsonSerializer"/> and loads all configurations.
    /// </summary>
    public ConfigManager()
    {
        _serializer = new();
        LoadBotConfig();
    }

    /// <summary>
    /// Loads <see cref="BotConfigModel"/> from file.
    /// </summary>
    /// <returns></returns>
    public void LoadBotConfig()
    {
        BotConfigModel? config = LoadConfig<BotConfigModel>(BOT_CONFIG_PATH);

        if (config is null)
        {
            Environment.Exit(1);
        }

        BotConfig = config;
    }

    /// <summary>
    /// Loads <see cref="TokensModel"/> from file.
    /// </summary>
    /// <exception cref="TokenLoadException"></exception>
    public void LoadBotTokens()
    {
        // The Discord webhook is also stored in this file, so the only way to know this file failed to load is 
        // to manually look at the logs :p
        Tokens = LoadConfig<TokensModel>(BOT_TOKENS_PATH) ?? throw new TokenLoadException(BOT_TOKENS_PATH);
    }

    /// <summary>
    /// Saves <see cref="BotConfig"/> to file. (Defaults to <see cref="defaultBotConfigPath"/>)
    /// </summary>
    /// <param name="path"></param>
    /// <param name="ctx"></param>
    /// <returns></returns>
    public async Task SaveBotConfigAsync(CommandContext? ctx = null)
    {
        if (BotConfig is not null)
        {
            SaveConfig(BOT_CONFIG_PATH, BotConfig);
            return;
        }

        const string errorMessage = "Bot storage is null : Aborting save to prevent overwriting config";

        if (ctx is not null)
        {
            await ctx.RespondAsync(errorMessage);
        }

        Log.Information(errorMessage);
    }

    public void SaveConfig(string path, BotConfigModel config)
    {
        try
        {
            using StreamWriter sw = new(path);
            using JsonTextWriter writer = new(sw)
            {
                Formatting = Formatting.Indented
            };

            _serializer.Serialize(writer, config);

            Log.Information("Config saved to '{Path}'.", path);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error saving config to: {Path}", path);
        }
    }

    public T LoadConfig<T>(string path) where T : class, new()
    {
        try
        {
            if (!File.Exists(path))
            {
                Log.Information("No config file found at '{Path}'. Proceeding with default values.", path);
                return new T();
            }

            using StreamReader sReader = new(path);
            using JsonTextReader jsonReader = new(sReader);
            T? config = _serializer.Deserialize<T>(jsonReader);

            if (config is null)
            {
                Log.Information("Failed to deserialize configuration file to type {Name} from: {Path}", typeof(T).Name, path);
            }
            else
            {
                return config;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error loading config.");
        }

        return new();
    }
}