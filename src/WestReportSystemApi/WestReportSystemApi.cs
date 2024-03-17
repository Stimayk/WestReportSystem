using CounterStrikeSharp.API.Core;

namespace WestReportSystemApi
{
    public interface IWestReportSystemApi
    {
        /// <summary>
        /// Проверяет, загружено ли ядро WestReportSystem.
        /// </summary>
        public event Action? OnCoreReady;

        /// <summary>
        /// Регистрирует делегат для отправки репортов.
        /// </summary>
        void SetReportDelegate(Action<CCSPlayerController, int, string> sendReportDelegate);

        /// <summary>
        /// Вызывает зарегистрированные делегаты для отправки репорта.
        /// </summary>
        void WRS_SendReport(CCSPlayerController? controller, int targetIndex, string reason);

        /// <summary>
        /// Получает значение конфигурации по указанному свойству.
        /// </summary>
        T? GetConfigValue<T>(string propertyName);

        /// <summary>
        /// Уведомляет игрока об отправленном репорте.
        /// </summary>
        void NotifyPlayerAboutReport(CCSPlayerController? controller, string targetPlayerName, string reason);

        /// <summary>
        /// Обновляет счетчик репортов для игрока.
        /// </summary>
        void UpdateReportCountForController(CCSPlayerController? controller);

        /// <summary>
        /// Сбрасывает счетчик репортов для игрока.
        /// </summary>
        void WRS_ClearCooldown(CCSPlayerController? controller);

        /// <summary>
        /// Получает текущий счетчик репортов для игрока.
        /// </summary>
        Dictionary<CCSPlayerController, int> WRS_GetCooldown(CCSPlayerController? controller);
        bool TryGetConfigValue<T>(string propertyName, out T? value);

        string GetTranslatedText(string name, params object[] args);
    }
}
