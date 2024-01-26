using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using Modularity;
using WestReportSystemApi;

namespace WestReportToDiscord
{
    public class WestReportToDiscord : BasePlugin, IModulePlugin
    {
        private IApiProvider? _apiProvider;
        private Discord? _discord;

        public override string ModuleName => "WestReportToDiscord";
        public override string ModuleVersion => "1.0";
        public override string ModuleAuthor => "E!N";

        public void LoadModule(IApiProvider provider)
        {
            if (!provider.Get<IWestReportSystemApi>().IsCoreLoaded())
            {
                Server.PrintToConsole($"WEST REPORT | API для {ModuleName} не было инициализировано.");
                return;
            }

            _apiProvider = provider;
            InitializeDiscord(provider);
            provider.Get<IWestReportSystemApi>().SetReportDelegate(WRS_SendReport);
            Server.PrintToConsole($"WEST REPORT | API для {ModuleName} было инициализировано.");
        }

        private void InitializeDiscord(IApiProvider provider)
        {
            try
            {
                var discordWebhook = provider.Get<IWestReportSystemApi>().GetConfigValue<string>("DiscordWebhook");
                var discordAdmins = provider.Get<IWestReportSystemApi>().GetConfigValue<string[]>("DiscordAdmins");
                var siteLink = provider.Get<IWestReportSystemApi>().GetConfigValue<string>("SiteLink");

                if (discordWebhook == null || discordAdmins == null || siteLink == null)
                    throw new InvalidOperationException("Необходимые параметры конфигурации Discord отсутствуют");

                _discord = new Discord(discordWebhook, discordAdmins, siteLink);
            }
            catch (Exception ex)
            {
                Server.PrintToConsole($"WEST REPORT | Ошибка при инициализации {ModuleName}: {ex.Message}");
            }
        }

        public void WRS_SendReport(CCSPlayerController controller, int targetIndex, string reason)
        {
            if (_apiProvider == null) return;

            try
            {
                var target = FindTargetPlayer(targetIndex);
                if (target == null)
                {
                    controller.PrintToChat("Нарушитель не найден");
                    return;
                }

                SendReport(controller, target, reason);
                UpdateReportStatus(controller, target.PlayerName, reason);
            }
            catch (Exception ex)
            {
                Server.PrintToConsole($"WEST REPORT | Ошибка при отправке репорта в Discord: {ex.Message}");
            }
        }

        private static CCSPlayerController? FindTargetPlayer(int targetIndex)
        {
            return Utilities.GetPlayers().FirstOrDefault(player => player.Index == targetIndex);
        }

        private void SendReport(CCSPlayerController controller, CCSPlayerController target, string reason)
        {
            var serverName = GetServerName();
            var mapName = NativeAPI.GetMapName();
            var serverIp = ConVar.Find("ip")?.StringValue ?? "Unknown IP";
            var serverPort = ConVar.Find("hostport")?.GetPrimitiveValue<int>().ToString() ?? "Unknown Port";

            _discord?.Send(serverName, controller.PlayerName, controller.SteamID, target.PlayerName, target.SteamID, reason, mapName, serverIp, serverPort);
        }

        private static string GetServerName()
        {
            var fullServerName = ConVar.Find("hostname")?.StringValue ?? "Unknown Server";
            var serverNameParts = fullServerName.Split('|');
            return serverNameParts.Length > 1 ? $"{serverNameParts[0].Trim()} | {serverNameParts[1].Trim()}" : fullServerName;
        }

        private void UpdateReportStatus(CCSPlayerController controller, string targetPlayerName, string reason)
        {
            _apiProvider?.Get<IWestReportSystemApi>().UpdateReportCountForController(controller);
            _apiProvider?.Get<IWestReportSystemApi>().NotifyPlayerAboutReport(controller, targetPlayerName, reason);
        }
    }
}