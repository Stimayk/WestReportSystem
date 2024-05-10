# WestReportSystem
Modular report system for your CS:2 server - WestReportSystem

Reporting system core

Capabilities:
Customizing the command to send a report
Customization of sending your reason
Report on a message from chat
Limit the number of reports per round.
Displaying if the sender and offender have Prime Status
Display the number of reports per card
Support for translations into different languages

Configuration file:
```
{
  "MaxReportsPerRound": 3, // Number of reports that can be sent in 1 round
  "ReportReasons": [
    "Читы",
    "Оскорбления",
    "Флуд",
    "Токсик",
    "Мониторинг"
  ], // Reasons for the reports
  "AllowCustomReason": true, // Allow you to give your own reason for the report
  "ChatRecord": true, // Report on a chat message
  "SiteLink": "test.com", // Link to the site where redirect.php is located
  "Commands": [
    "report",
    "rt",
    "rep"
  ], // Commands that can be used to call the report sending menu
}
```

**Currently there are 8 modules available for its operation 3 of which allow you to send reports to Discord, VK and Telegram**

[WestReportToDiscord](https://github.com/Stimayk/WestReportToDiscord) - (Sending reports to Discord)

[WestReportToVK](https://github.com/Stimayk/WestReportToVK) - (Send reports to VK)

[WestReportToTelegram](https://github.com/Stimayk/WestReportToTelegram) - (Send reports to Telegram)

[WestReportAdminNotify](https://github.com/Stimayk/WestReportAdminNotify) - (Notify admins about a report in chat/hud)

[WestReportChatNotify](https://github.com/Stimayk/WestReportChatNotify) - (Promotional reminder to send reports to chat/hud)

[WestReportHUDNotify](https://github.com/Stimayk/WestReportHUDNotify) - (Notifies the player in the hud that a report has been sent, displays nickname and avatar)

[WestReportNoColdown](https://github.com/Stimayk/WestReportNoColdown) - (Disables report sending restriction for a certain admin flag)

[WestReportNoColdownVIP](https://github.com/Stimayk/WestReportNoColdownVIP) - (Disables the restriction to send reports for a certain group of VIP players)
