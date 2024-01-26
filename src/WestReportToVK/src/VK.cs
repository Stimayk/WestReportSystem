using System.Net.Http.Headers;

public class VK
{
    private readonly string? _peerId;
    private readonly string? _link;
    private readonly HttpClient _client;
    private readonly Random _random;

    public VK(string vkToken, string vkPeerId, string sitelink)
    {
        _random = new Random();
        _client = new HttpClient
        {
            BaseAddress = new Uri("https://api.vk.com"),
        };
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", vkToken);

        _peerId = vkPeerId;
        _link = sitelink;
    }

    public async Task<bool> Send(string serverName, string[]? admins, string controllerName, ulong controllerSteamID, string targetName, ulong targetSteamID, string reason, string mapName, string serverIp, string serverPort, bool debug = false)
    {
        try
        {
            var message = CreateMessage(serverName, admins, controllerName, controllerSteamID, targetName, targetSteamID, reason, mapName, serverIp, serverPort);
            return await PostMessage(message, debug);
        }
        catch (Exception e)
        {
            Console.WriteLine($"WEST REPORT | Error sending message to VK: {e.Message}");
            return false;
        }
    }

    private string CreateMessage(string serverName, string[]? admins, string controllerName, ulong controllerSteamID, string targetName, ulong targetSteamID, string reason, string mapName, string serverIp, string serverPort)
    {
        var adminsList = admins != null ? string.Join(", ", admins) : "Не указаны";
        return $"🚨 Пришла новая жалоба!\n\n🖥️ Сервер: {serverName}\n👮🏿 Администраторы: {adminsList}\n\n👨 Отправитель: {controllerName} [{controllerSteamID}]\n🔗 Ссылка на Steam: https://steamcommunity.com/profiles/{controllerSteamID}/ \n🧌 Нарушитель: {targetName} [{targetSteamID}]\n🔗 Ссылка на Steam: https://steamcommunity.com/profiles/{targetSteamID}/\n\n📃 Причина жалобы: {reason}\n🗺️ Карта: {mapName}\n\n📡 Подключиться к серверу: {_link}/redirect.php?ip={serverIp}:{serverPort}";
    }

    private async Task<bool> PostMessage(string message, bool debug)
    {
        var parameters = new Dictionary<string, string?>
        {
            {"random_id", _random.Next(int.MaxValue).ToString()},
            {"peer_id", _peerId},
            {"message", message},
            {"v", "5.199"},
            {"dont_parse_links", "1"}
        };

        var response = await _client.PostAsync("/method/messages.send", new FormUrlEncodedContent(parameters));
        var jsonResponse = await response.Content.ReadAsStringAsync();
        if (debug) Console.WriteLine($"WEST REPORT | {response.StatusCode} | {jsonResponse}");

        return response.IsSuccessStatusCode && !jsonResponse.Contains("error");
    }
}
