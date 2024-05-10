using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;

namespace WestReportSystemApiReborn
{
    public interface IWestReportSystemApi
    {
        static PluginCapability<IWestReportSystemApi> Capability { get; } = new("westreportsystem:core");

        void RegisterReportingModule(Action<CCSPlayerController, CCSPlayerController, string> reportAction);

        /// <summary>
        /// Вызывает зарегистрированные делегаты для отправки репорта.
        /// </summary>
        void WRS_SendReport(CCSPlayerController? sender, CCSPlayerController violator, string reason);

        /// <summary>
        /// Получает значение конфигурации по указанному свойству.
        /// </summary>
        T? GetConfigValue<T>(string propertyName);

        /// <summary>
        /// Сбрасывает счетчик репортов для игрока.
        /// </summary>
        void WRS_ClearCooldown(CCSPlayerController? controller);

        /// <summary>
        /// Получает текущий счетчик репортов для игрока.
        /// </summary>
        Dictionary<CCSPlayerController, int> WRS_GetCooldown(CCSPlayerController? controller);
        Dictionary<CCSPlayerController, int> WRS_GetReportCounterPerRound(CCSPlayerController? violator);
        bool TryGetConfigValue<T>(string propertyName, out T? value);

        string GetTranslatedText(string name, params object[] args);

        Dictionary<CCSPlayerController, Action<string>>? NextCommandAction { get; set; }

        event Action<CCSPlayerController, CCSPlayerController, string>? OnReportSend;
        public bool HasPrimeStatus(ulong steamID);
    }
}