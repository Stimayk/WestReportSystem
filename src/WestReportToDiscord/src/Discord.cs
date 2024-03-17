using System.Text;
using System.Text.Json;

public class Discord
{
    private readonly string? _webhookUrl;
    private readonly string[] _admins;
    private readonly string? _link;

    public Discord(string webhookUrl, string[] admins, string sitelink)
    {
        _webhookUrl = webhookUrl;
        _admins = admins;
        _link = sitelink;
    }

    public async Task<bool> Send(string serverName, string controllerName, ulong controllerSteamID, string targetName, ulong targetSteamID, string reason, string mapName, string serverIp, string serverPort)
    {
        try
        {
            var message = CreateMessage(serverName, controllerName, controllerSteamID, targetName, targetSteamID, reason, mapName, serverIp, serverPort);
            var jsonPayload = JsonSerializer.Serialize(new { content = message });
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            using var httpClient = new HttpClient();
            var response = await httpClient.PostAsync(_webhookUrl, content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WEST REPORT SYSTEM | Error sending message to Discord: {ex.Message}");
            return false;
        }
    }

    private string CreateMessage(string serverName, string controllerName, ulong controllerSteamID, string targetName, ulong targetSteamID, string reason, string mapName, string serverIp, string serverPort)
    {
        var adminList = string.Join(", ", _admins);
        return $"🚨 Пришла новая жалоба!\n\n🖥️ Сервер: {serverName}\n👮🏿 Администраторы: {adminList}\n\n👨 Отправитель: {controllerName} [{controllerSteamID}]\nСсылка на Steam: https://steamcommunity.com/profiles/{controllerSteamID}/ \n🧌 Нарушитель: {targetName} [{targetSteamID}]\nСсылка на Steam: https://steamcommunity.com/profiles/{targetSteamID}/\n🖹 Причина жалобы: {reason}\n🗺️ Карта: {mapName}\n\n📡 Подключиться к серверу: {_link}/redirect.php?ip={serverIp}:{serverPort}";
    }
}
