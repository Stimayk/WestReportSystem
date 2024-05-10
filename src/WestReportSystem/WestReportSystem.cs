using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using Newtonsoft.Json;
using Steamworks;
using WestReportSystemApiReborn;

namespace WestReportSystemReborn
{
    [MinimumApiVersion(221)]
    public class WestReportSystemReborn : BasePlugin
    {
        public override string ModuleName => "WestReportSystem";
        public override string ModuleAuthor => "E!N";
        public override string ModuleDescription => "Modular reporting system";
        public override string ModuleVersion => "v1.2";

        private static readonly Dictionary<CCSPlayerController, int> amountThisRound = [];
        public Dictionary<CCSPlayerController, List<(DateTime, string)>> PlayerChatHistory = [];
        private static readonly Dictionary<CCSPlayerController, int> playerReportCountPerMap = [];

        public Dictionary<CCSPlayerController, Action<string>> NextCommandAction { get; set; } = [];

        private static string? ChatPrefix;
        public WestReportSystemApi? _api;
        private static readonly Config cfg = Config.Load();
        private static int core;

        private readonly PluginCapability<IWestReportSystemApi> _pluginCapability = new("westreportsystem:core");

        public override void Load(bool hotReload)
        {
            _api = new WestReportSystemApi(this);
            Capabilities.RegisterPluginCapability(_pluginCapability, () => _api);

            AddCommandListener("say", OnSay);
            AddCommandListener("say_team", OnSay);

            _api.NextCommandAction = [];

            SetupEventHandlers();
            SetupPrefix();
            CreateReportMenu();

            _api.ModuleRegistered += OnModuleRegistered;
        }

        private void SetupEventHandlers()
        {
            RegisterEventHandler<EventRoundStart>(OnRoundStart);
        }

        private void SetupPrefix()
        {
            ChatPrefix = $" {ChatColors.Orange}{Localizer["wrs.Prefix"]} | ";
        }

        private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
        {
            amountThisRound.Clear();
            return HookResult.Continue;
        }

        private void CreateReportMenu()
        {
            var reportPlayerMenu = InitializeReportMenu();
            ConfigureReportCommand(reportPlayerMenu);
        }

        private CenterHtmlMenu InitializeReportMenu()
        {
            var reportPlayerMenu = new CenterHtmlMenu($"{Localizer["wrs.MenuTitle"]}", this);
            reportPlayerMenu.MenuOptions.Clear();
            return reportPlayerMenu;
        }

        private void ConfigureReportCommand(CenterHtmlMenu reportPlayerMenu)
        {
            foreach (var cmds in cfg.Commands)
            {
                var cmdsList = string.Join(", ", cmds);
                AddCommand($"css_{cmdsList}", "report command", (player, info) => HandleReportCommand(player, reportPlayerMenu));
            }
        }

        private void HandleReportCommand(CCSPlayerController? player, CenterHtmlMenu reportPlayerMenu)
        {
            if (core == 1)
            {
                if (player == null) return;

                if (CheckReportLimitReached(player))
                {
                    InformPlayerAboutReportLimit(player);
                    return;
                }

                CreateReportPlayerMenu(reportPlayerMenu, player);
                MenuManager.OpenCenterHtmlMenu(this, player, reportPlayerMenu);
            }
            else
            {
                player?.PrintToChat($"{ChatPrefix}{Localizer["wrs.ModuleNotActive"]}");
            }
        }

        private static bool CheckReportLimitReached(CCSPlayerController player)
        {
            return amountThisRound.TryGetValue(player, out int value) && value >= cfg.MaxReportsPerRound;
        }

        private void InformPlayerAboutReportLimit(CCSPlayerController player)
        {
            player.PrintToChat($" {ChatPrefix}{Localizer["wrs.MaxReportsPerRound"]} {cfg.MaxReportsPerRound}");
        }

        private void CreateReportPlayerMenu(CenterHtmlMenu reportPlayerMenu, CCSPlayerController controller)
        {
            reportPlayerMenu.MenuOptions.Clear();

            var eligiblePlayers = GetEligiblePlayersForReport(controller);
            if (!eligiblePlayers.Any())
            {
                controller.PrintToChat($" {ChatPrefix}{Localizer["wrs.NoSuitablePlayersOnTheServer"]}");
                return;
            }

            foreach (var player in eligiblePlayers)
            {
                AddPlayerToReportMenu(reportPlayerMenu, player);
            }
        }

        private static IEnumerable<CCSPlayerController> GetEligiblePlayersForReport(CCSPlayerController reportingPlayer)
        {
            return Utilities.GetPlayers().Where(player =>
            player.PlayerName == reportingPlayer.PlayerName && !player.IsHLTV && player.Pawn.IsValid && !player.IsBot);
        }

        private void AddPlayerToReportMenu(CenterHtmlMenu reportPlayerMenu, CCSPlayerController violator)
        {
            var playerOption = $"{violator.PlayerName}";

            reportPlayerMenu.AddMenuOption(playerOption, (sender, option) =>
            {
                CreateReportReasonsMenu(sender, violator);
            });
        }

        private void CreateReportReasonsMenu(CCSPlayerController sender, CCSPlayerController violator)
        {
            var reportReasonMenu = new CenterHtmlMenu($"{Localizer["wrs.SelectTheReasonForTheComplaint"]}", this);
            AddStandardReportReasons(reportReasonMenu, violator);
            AddCustomReportReasonOption(reportReasonMenu, violator);
            if (PlayerChatHistory.TryGetValue(sender, out var messages) && messages.Count > 0)
            {
                AddChatMessagesReportOption(reportReasonMenu, sender);
            }
            MenuManager.OpenCenterHtmlMenu(this, sender, reportReasonMenu);
        }

        private void AddStandardReportReasons(CenterHtmlMenu reportReasonMenu, CCSPlayerController violator)
        {
            foreach (var reason in cfg.ReportReasons)
            {
                reportReasonMenu.AddMenuOption(reason, (sender, option) =>
                {
                    CreateConfirmationMenu(sender, violator, reason);
                });
            }
        }

        private void AddChatMessagesReportOption(CenterHtmlMenu reportReasonMenu, CCSPlayerController violator)
        {
            reportReasonMenu.AddMenuOption(Localizer["wrs.ChatMessage"], (ctrl, option) =>
            {
                var chatHistoryMenu = new CenterHtmlMenu(Localizer["wrs.SelectChatMessage"], this);
                if (PlayerChatHistory.TryGetValue(violator, out var messages) && messages.Count > 0)
                {
                    foreach (var (time, message) in messages.TakeLast(10))
                    {
                        chatHistoryMenu.AddMenuOption($"{time:HH:mm:ss}: {message}", (sender, opt) =>
                        {
                            var timemessage = $"[{time:HH:mm:ss}]";
                            CreateConfirmationMenu(sender, violator, $"{Localizer["wrs.ChatMessage"]} {violator.PlayerName}: {message} {timemessage}");
                        });
                    }
                    MenuManager.OpenCenterHtmlMenu(this, ctrl, chatHistoryMenu);
                }
                else
                {
                    ctrl.PrintToChat($"{ChatPrefix}{Localizer["wrs.NoChatMessages"]}");
                    MenuManager.CloseActiveMenu(ctrl);
                }
            });
        }

        private void AddCustomReportReasonOption(CenterHtmlMenu reportReasonMenu, CCSPlayerController violator)
        {
            if (cfg.AllowCustomReason)
            {
                reportReasonMenu.AddMenuOption(Localizer["wrs.CustomReason"], (sender, option) =>
                {
                    sender.PrintToChat($"{ChatPrefix}{Localizer["wrs.EnterCustomReason"]}");
                    MenuManager.CloseActiveMenu(sender);

                    if (_api != null)
                    {
                        _api.NextCommandAction ??= [];

                        _api.NextCommandAction[sender] = input =>
                        {
                            if (!string.IsNullOrWhiteSpace(input))
                            {
                                CreateConfirmationMenu(sender, violator, input);
                            }
                            else
                            {
                                sender.PrintToChat($"{ChatPrefix}{Localizer["wrs.InvalidInput"]}");
                            }
                        };
                    }
                    else
                    {
                        sender.PrintToChat($"{ChatPrefix}Error: API is not available.");
                    }
                });
            }
        }

        private HookResult OnSay(CCSPlayerController? player, CommandInfo info)
        {
            if (player == null || !player.IsValid || player.IsBot) return HookResult.Continue;

            var message = info.GetArg(1);

            LogPlayerChatMessage(player, message);

            if (_api?.NextCommandAction != null)
            {
                if (_api.NextCommandAction.TryGetValue(player, out Action<string>? action))
                {
                    action.Invoke(message);
                    _api.NextCommandAction.Remove(player);
                    return HookResult.Stop;
                }
            }

            return HookResult.Continue;
        }

        private void LogPlayerChatMessage(CCSPlayerController player, string message)
        {
            if (!cfg.ChatRecord) return;

            if (!PlayerChatHistory.TryGetValue(player, out List<(DateTime, string)>? value))
            {
                value = [];
                PlayerChatHistory[player] = value;
            }

            value.Add((DateTime.Now, message));
        }

        private void CreateConfirmationMenu(CCSPlayerController sender, CCSPlayerController violator, string reason)
        {
            var confirmationMenu = new CenterHtmlMenu(Localizer["wrs.ConfirmationTitle"], this);
            string violatorPlayerName = GetPlayerNameBySteamID(violator.SteamID);
            confirmationMenu.Title = $"{Localizer["wrs.ConfirmationDetails"]} {Localizer["wrs.TargetPlayer"]} {violatorPlayerName} {Localizer["wrs.Reason"]} {reason}";

            confirmationMenu.AddMenuOption(Localizer["wrs.Confirm"], (sender, option) =>
            {
                _api?.WRS_SendReport(sender, violator, reason);
                _api?.NotifyPlayerAboutReport(sender, violator, reason);
                WestReportSystemApi.UpdateReportCountForController(sender);

                MenuManager.CloseActiveMenu(sender);
            });

            confirmationMenu.AddMenuOption(Localizer["wrs.Change"], (sender, option) =>
            {
                HandleReportCommand(sender, InitializeReportMenu());
            });
            MenuManager.OpenCenterHtmlMenu(this, sender, confirmationMenu);
        }

        private static string GetPlayerNameBySteamID(ulong steamID)
        {
            return Utilities.GetPlayers().FirstOrDefault(p => p.SteamID == steamID)?.PlayerName ?? "Unknown Player";
        }

        private void OnModuleRegistered()
        {
            if (_api != null)
            {
                core = _api.HasRegisteredReportingModules() ? 1 : 0;

                if (core == 0)
                {
                    Console.WriteLine($"{_api.GetTranslatedText("wrs.SystemName")} | module not found. Core is waiting for a module");
                }
                else
                {
                    Console.WriteLine($"{_api.GetTranslatedText("wrs.SystemName")} | Reporting module module successfully detected, core fully active.");
                }
            }
        }

        [GameEventHandler]
        public HookResult MapTransition(EventMapTransition @event, GameEventInfo info)
        {
            playerReportCountPerMap.Clear();

            return HookResult.Continue;
        }

        public class WestReportSystemApi(WestReportSystemReborn WestReportSystemCore) : IWestReportSystemApi
        {

            public string WestReportSystem { get; } = WestReportSystemCore.ModuleName;

            public event Action? ModuleRegistered;

            private event Action<CCSPlayerController, CCSPlayerController, string>? OnReportSend;

            private readonly List<Action<CCSPlayerController, CCSPlayerController, string>> _reportDelegates = [];
            public Dictionary<CCSPlayerController, Action<string>>? NextCommandAction { get; set; }

            event Action<CCSPlayerController, CCSPlayerController, string>? IWestReportSystemApi.OnReportSend
            {
                add
                {
                    OnReportSend += value;
                }

                remove
                {
                    OnReportSend -= value;
                }
            }

            public void RegisterReportingModule(Action<CCSPlayerController, CCSPlayerController, string> reportAction)
            {
                if (!_reportDelegates.Contains(reportAction))
                {
                    _reportDelegates.Add(reportAction);
                    ModuleRegistered?.Invoke();
                }
            }

            public bool HasRegisteredReportingModules()
            {
                return _reportDelegates.Count > 0;
            }

            public void WRS_SendReport(CCSPlayerController? sender, CCSPlayerController violator, string reason)
            {
                if (sender != null)
                {
                    foreach (var reportDelegate in _reportDelegates)
                    {
                        reportDelegate.Invoke(sender, violator, reason);
                    }
                    OnReportSend?.Invoke(sender, violator, reason);

                    if (playerReportCountPerMap.TryGetValue(violator, out int value))
                    {
                        playerReportCountPerMap[violator] = ++value;
                    }
                    else
                    {
                        playerReportCountPerMap[violator] = 1;
                    }
                }
            }

            // Update the report counter for the player
            public static void UpdateReportCountForController(CCSPlayerController? sender)
            {
                if (sender != null)
                {
                    if (!amountThisRound.TryGetValue(sender, out var amount))
                    {
                        amount = 0;
                    }
                    amountThisRound[sender] = amount + 1;
                }
            }

            // Notify the player of the report
            public void NotifyPlayerAboutReport(CCSPlayerController? sender, CCSPlayerController violator, string reason)
            {
                sender?.PrintToChat($"{ChatPrefix}{WestReportSystemCore.Localizer["wrs.NotifyPlayerAboutReport", violator.PlayerName, reason]}");
            }

            // Get value from core configuration
            public T? GetConfigValue<T>(string propertyName)
            {
                return TryGetConfigValue<T>(propertyName, out var value) ? value : default;
            }

            // Clearing the report counter
            public void WRS_ClearCooldown(CCSPlayerController? sender)
            {
                if (sender != null)
                {
                    amountThisRound[sender] = 0;
                }
            }

            // Get the report counter
            public Dictionary<CCSPlayerController, int> WRS_GetCooldown(CCSPlayerController? sender)
            {
                return amountThisRound;
            }

            // Trying to get the value from the configuration
            public bool TryGetConfigValue<T>(string propertyName, out T? value)
            {
                var propertyInfo = typeof(Config).GetProperty(propertyName);
                if (propertyInfo != null)
                {
                    var configValue = propertyInfo.GetValue(cfg);
                    if (configValue != null)
                    {
                        if (configValue is T castedValue)
                        {
                            value = castedValue;
                            return true;
                        }
                        else if (typeof(T).IsArray && configValue.GetType().IsArray)
                        {
                            value = (T)Convert.ChangeType(configValue, typeof(T));
                            return true;
                        }
                    }
                }

                value = default;
                return false;
            }

            // Get translation
            public string GetTranslatedText(string name, params object[] args) => WestReportSystemCore.Localizer[name, args];

            // Check for Prime status
            public bool HasPrimeStatus(ulong steamID)
            {
                CSteamID cSteamID = new(steamID);

                var primeStatusApp1 = SteamGameServer.UserHasLicenseForApp(cSteamID, new AppId_t(624820));
                var primeStatusApp2 = SteamGameServer.UserHasLicenseForApp(cSteamID, new AppId_t(54029));

                bool hasPrime = primeStatusApp1 == EUserHasLicenseForAppResult.k_EUserHasLicenseResultHasLicense ||
                                primeStatusApp2 == EUserHasLicenseForAppResult.k_EUserHasLicenseResultHasLicense;

                return hasPrime;
            }

            // Getting the number of reports per map
            public Dictionary<CCSPlayerController, int> WRS_GetReportCounterPerRound(CCSPlayerController? violator)
            {
                if (violator == null)
                {
                    return [];
                }

                return playerReportCountPerMap;
            }
        }
    }

    public class Config
    {
        public int MaxReportsPerRound { get; set; }
        public string[] ReportReasons { get; set; }
        public bool AllowCustomReason { get; set; }
        public bool ChatRecord { get; set; }
        public string? SiteLink { get; set; }
        public string[] Commands { get; set; }

        public Config()
        {
            MaxReportsPerRound = 3;
            ReportReasons = ["Читы", "Оскорбления", "Флуд", "Токсик", "Мониторинг"];
            AllowCustomReason = true;
            ChatRecord = true;
            Commands = ["report", "rt", "rep"];
            SiteLink = "";
        }

        public static Config Load()
        {
            string configFilePath = GetConfigFilePath();

            if (!File.Exists(configFilePath))
            {
                Config defaultConfig = new()
                {
                    MaxReportsPerRound = 3,
                    ReportReasons = ["Читы", "Оскорбления", "Флуд", "Токсик", "Мониторинг"],
                    AllowCustomReason = true,
                    Commands = ["report", "rt", "rep"]
                };
                WriteConfigFile(configFilePath, JsonConvert.SerializeObject(defaultConfig, Newtonsoft.Json.Formatting.Indented));
            }

            string json = ReadConfigFile(configFilePath);
            return JsonConvert.DeserializeObject<Config>(json) ?? new Config();
        }

        public static string GetConfigFilePath()
        {
            string path = GetCfgDirectory() + "/WestReportSystem";
            Directory.CreateDirectory(path);
            return $"{path}/core.json";
        }

        private static string ReadConfigFile(string filePath)
        {
            try
            {
                using StreamReader reader = new(filePath);
                return reader.ReadToEnd();
            }
            catch (IOException ex)
            {
                throw new Exception($"Error loading config file: {ex.Message}");
            }
        }

        public static void WriteConfigFile(string filePath, string content)
        {
            try
            {
                using StreamWriter writer = new(filePath);
                writer.Write(content);
            }
            catch (IOException ex)
            {
                throw new Exception($"Error writing config file: {ex.Message}");
            }
        }

        private static string GetCfgDirectory()
        {
            return Server.GameDirectory + "/csgo/addons/counterstrikesharp/configs/plugins/";
        }
    }
}
