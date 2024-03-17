using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using WestReportSystemApi;

namespace WestReportSystem;

[MinimumApiVersion(184)]
public class WestReportSystem : BasePlugin
{
    public override string ModuleName => "WestReportSystem";
    public override string ModuleAuthor => "E!N";
    public override string ModuleDescription => "Modular reporting system";
    public override string ModuleVersion => "v1.1";

    private static readonly Dictionary<CCSPlayerController, int> amountThisRound = new();
    private static string? ChatPrefix;
    public WestReportSystemApi? _api;

    static readonly Config cfg = Config.Load();

    public static bool CoreLoaded { get; private set; }

    private readonly PluginCapability<WestReportSystemApi> _pluginCapability = new("westreportsystem:core");

    public override void Load(bool hotReload)
    {
        _api = new WestReportSystemApi(this);
        Capabilities.RegisterPluginCapability(_pluginCapability, () => _api);

        SetupEventHandlers();
        SetupPrefix();
        CreateReportMenu();
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
        var reportPlayerMenu = new CenterHtmlMenu($"{Localizer["wrs.MenuTitle"]}");
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
        if (player == null) return;

        if (CheckReportLimitReached(player))
        {
            InformPlayerAboutReportLimit(player);
            return;
        }

        CreateReportPlayerMenu(reportPlayerMenu, player);
        MenuManager.OpenCenterHtmlMenu(this, player, reportPlayerMenu);
    }

    private bool CheckReportLimitReached(CCSPlayerController player)
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

    private IEnumerable<CCSPlayerController> GetEligiblePlayersForReport(CCSPlayerController reportingPlayer)
    {
        return Utilities.GetPlayers().Where(player =>
            player.PlayerName != reportingPlayer.PlayerName && !player.IsHLTV && player.Pawn.IsValid);
    }

    private void AddPlayerToReportMenu(CenterHtmlMenu reportPlayerMenu, CCSPlayerController player)
    {
        var playerOption = $"{player.PlayerName}";
        reportPlayerMenu.AddMenuOption(playerOption, (controller, option) =>
        {
            var targetIndex = int.Parse(playerOption.Split('[', ']')[1]);
            CreateReportReasonsMenu(controller, targetIndex);
        });
    }

    private void CreateReportReasonsMenu(CCSPlayerController controller, int targetIndex)
    {
        var reportReasonMenu = new CenterHtmlMenu($"{Localizer["wrs.SelectTheReasonForTheComplaint"]}");
        AddStandardReportReasons(reportReasonMenu, controller, targetIndex);
        //AddCustomReportReasonOption(reportReasonMenu, controller);
        MenuManager.OpenCenterHtmlMenu(this, controller, reportReasonMenu);
    }

    private void AddStandardReportReasons(CenterHtmlMenu reportReasonMenu, CCSPlayerController? controller, int targetIndex)
    {
        foreach (var reason in cfg.ReportReasons)
        {
            reportReasonMenu.AddMenuOption(reason, (ctrl, option) => _api?.WRS_SendReport(ctrl, targetIndex, reason));
        }
    }

    //private void AddCustomReportReasonOption(CenterHtmlMenu reportReasonMenu, CCSPlayerController? controller)
    //{
    //    if (cfg.AllowCustomReason)
    //    {
    //        reportReasonMenu.AddMenuOption("Другая причина", (ctrl, option) =>
    //        {
    //            // TO DO;
    //        });
    //    }
    //}

    public class WestReportSystemApi : IWestReportSystemApi
    {
        private readonly WestReportSystem _WestReportSystemCore;

        public string WestReportSystem { get; }

        public WestReportSystemApi(WestReportSystem WestReportSystemCore)
        {
            _WestReportSystemCore = WestReportSystemCore;

            WestReportSystem = WestReportSystemCore.ModuleName;
        }

        private readonly List<Action<CCSPlayerController, int, string>> _sendReportDelegates = new();

        public event Action? OnCoreReady;

        // Регистрация делегата для отправки репортов
        public void SetReportDelegate(Action<CCSPlayerController, int, string> sendReportDelegate)
        {
            _sendReportDelegates.Add(sendReportDelegate);
        }

        // Отправка репорта через все зарегистрированные делегаты
        public void WRS_SendReport(CCSPlayerController? controller, int targetIndex, string reason)
        {
            if (controller != null)
            {
                foreach (var reportDelegate in _sendReportDelegates)
                {
                    reportDelegate?.Invoke(controller, targetIndex, reason);
                }
            }
        }

        // Обновление счетчика репортов для игрока
        public void UpdateReportCountForController(CCSPlayerController? controller)
        {
            if (controller != null)
            {
                if (!amountThisRound.TryGetValue(controller, out var amount))
                {
                    amount = 0;
                }
                amountThisRound[controller] = amount + 1;
            }
        }

        // Уведомление игрока о репорте
        public void NotifyPlayerAboutReport(CCSPlayerController? controller, string targetPlayerName, string reason)
        {
            controller?.PrintToChat($" {_WestReportSystemCore.Localizer["wrs.NotifyPlayerAboutReport", targetPlayerName, reason]}");
        }

        // Получение значения из конфигурации
        public T? GetConfigValue<T>(string propertyName)
        {
            return TryGetConfigValue<T>(propertyName, out var value) ? value : default;
        }

        // Очистка счетчика репортов
        public void WRS_ClearCooldown(CCSPlayerController? controller)
        {
            if (controller != null)
            {
                amountThisRound[controller] = 0;
            }
        }

        // Получение счетчика репортов
        public Dictionary<CCSPlayerController, int> WRS_GetCooldown(CCSPlayerController? controller)
        {
            return amountThisRound;
        }

        // Попытка получить значение из конфигурации
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

        public string GetTranslatedText(string name, params object[] args) => _WestReportSystemCore.Localizer[name, args];
    }
}
