using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Cvars;
using WestReportSystemApi;

namespace WestReportToDiscord
{
    public class WestReportToDiscord : BasePlugin
    {
        public override string ModuleName => "WestReportToDiscord";
        public override string ModuleVersion => "1.0.1";
        public override string ModuleAuthor => "E!N";

        private IWestReportSystemApi? _api;
        private Discord? _discord;

        private PluginCapability<IWestReportSystemApi> PluginCapability { get; } = new("westreportsystem:core");

        public override void OnAllPluginsLoaded(bool hotReload)
        {
            _api = PluginCapability.Get();
            if (_api == null) return;

            _api.OnCoreReady += () =>
            {
                InitializeDiscord();
                _api.SetReportDelegate(WRS_SendReport);
            };
        }

        private void InitializeDiscord()
        {
            try
            {
                var discordWebhook = _api?.GetConfigValue<string>("DiscordWebhook");
                var discordAdmins = _api?.GetConfigValue<string[]>("DiscordAdmins");
                var siteLink = _api?.GetConfigValue<string>("SiteLink");

                if (discordWebhook == null || discordAdmins == null || siteLink == null)
                    throw new InvalidOperationException("The required Discord configuration settings are missing");

                _discord = new Discord(discordWebhook, discordAdmins, siteLink);
            }
            catch (Exception ex)
            {
                Server.PrintToConsole($"WEST REPORT SYSTEM | Error during initialization of {ModuleName}: {ex.Message}");
            }
        }

        public void WRS_SendReport(CCSPlayerController controller, int targetIndex, string reason)
        {
            try
            {
                var target = FindTargetPlayer(targetIndex);
                if (target == null)
                {
                    controller.PrintToChat($"{_api?.GetTranslatedText("wrs.IntruderNotFound")}");
                    return;
                }

                SendReport(controller, target, reason);
                UpdateReportStatus(controller, target.PlayerName, reason);
            }
            catch (Exception ex)
            {
                Server.PrintToConsole($"WEST REPORT SYSTEM | Error sending report to Discord: {ex.Message}");
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
            _api?.UpdateReportCountForController(controller);
            _api?.NotifyPlayerAboutReport(controller, targetPlayerName, reason);
        }
    }
}
