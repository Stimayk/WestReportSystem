using CounterStrikeSharp.API;
using Newtonsoft.Json;

public class Config
{
    // Определение свойств конфигурации
    public string? VkToken { get; set; }
    public string? VkPeerId { get; set; }
    public string[]? VkAdmins { get; set; }

    public string? DiscordWebhook { get; set; }
    public string[]? DiscordAdmins { get; set; }

    public int MaxReportsPerRound { get; set; } = 3;
    public string[] ReportReasons { get; set; } = { "Читы", "Оскорбления", "Флуд", "Токсик", "Мониторинг" };

    public bool AllowCustomReason { get; set; }

    public string? SiteLink { get; set; }
    public required string[] Commands { get; set; }

    // Загрузка конфигурации
    public static Config Load()
    {
        string configFilePath = GetConfigFilePath();
        string json = ReadConfigFile(configFilePath);

        Config cfg = JsonConvert.DeserializeObject<Config>(json) ?? throw new Exception("WEST REPORT | No settings found!");
        return cfg;
    }

    // Получение пути к файлу конфигурации
    private static string GetConfigFilePath()
    {
        string path = GetCfgDirectory() + "/WestReportSystem";
        Directory.CreateDirectory(path);
        return $"{path}/core.json";
    }

    // Чтение файла конфигурации
    private static string ReadConfigFile(string filePath)
    {
        try
        {
            using StreamReader reader = new(filePath);
            return reader.ReadToEnd();
        }
        catch (IOException ex)
        {
            throw new Exception($"WEST REPORT | Error loading config file: {ex.Message}");
        }
    }

    // Получение директории конфигурации
    private static string GetCfgDirectory()
    {
        return Server.GameDirectory + "/csgo/addons/counterstrikesharp/configs/plugins/";
    }
}