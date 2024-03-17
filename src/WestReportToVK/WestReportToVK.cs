using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Cvars;
using WestReportSystemApi;

namespace WestReportToVK
{
    public class WestReportToVK : BasePlugin
    {

        public override string ModuleName => "WestReportToVK";
        public override string ModuleVersion => "1.0.1";
        public override string ModuleAuthor => "E!N";

        private IWestReportSystemApi? _api;
        private VK? _vk;

        private PluginCapability<IWestReportSystemApi> PluginCapability { get; } = new("westreportsystem:core");

        public override void OnAllPluginsLoaded(bool hotReload)
        {
            _api = PluginCapability.Get();
            if (_api == null) return;

            _api.OnCoreReady += () =>
            {
                InitializeVK();
                _api.SetReportDelegate(WRS_SendReport);
            };
        }

        private void InitializeVK()
        {
            try
            {
                var vkToken = _api?.GetConfigValue<string>("VkToken");
                var vkPeerId = _api?.GetConfigValue<string>("VkPeerId");
                var siteLink = _api?.GetConfigValue<string>("SiteLink");

                if (vkToken == null || vkPeerId == null || siteLink == null)
                    throw new InvalidOperationException("Necessary VK configuration parameters are missing");

                _vk = new VK(vkToken, vkPeerId, siteLink);
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
                Server.PrintToConsole($"WEST REPORT SYSTEM | Error sending report to VK: {ex.Message}");
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
            var vkAdmins = _api?.GetConfigValue<string[]>("VkAdmins");

            _vk?.Send(serverName, vkAdmins, controller.PlayerName, controller.SteamID, target.PlayerName, target.SteamID, reason, mapName, serverIp, serverPort);
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
