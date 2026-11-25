namespace CleanPulse.Core.Services;

/// <summary>
/// Built-in bilingual (Russian / English) string dictionary for CleanPulse.
/// </summary>
public static class LocalizationCatalog
{
    private static readonly Dictionary<string, string> Ru = new(StringComparer.Ordinal)
    {
        // App & Nav
        ["app_name"] = "CleanPulse",
        ["app_tagline"] = "Локальная очистка Windows",
        ["nav_cleaner"] = "Очистка",
        ["nav_logs"] = "Журнал",
        ["nav_exclusions"] = "Исключения",
        ["nav_settings"] = "Настройки",
        ["privilege_admin"] = "Администратор",
        ["privilege_user"] = "Стандартный режим",
        ["privilege_elevate"] = "Запустить как админ",
        ["privilege_elevate_hint"] = "Для очистки системного Temp, кэша обновлений Windows и журналов требуются права администратора.",
        ["drive_free_of"] = "свободно из",
        ["drive_system_disk"] = "Системный диск",

        // Groups
        ["group_system_temp"] = "Система и временные файлы",
        ["group_windows_cache"] = "Кэш Windows и сеть",
        ["group_logs_dumps"] = "Логи, отчёты и дампы",

        // Categories
        ["cat_user_temp_title"] = "Временные файлы пользователя",
        ["cat_user_temp_desc"] = "Временные файлы программ и установщиков в %TEMP%",
        ["cat_user_temp_safety"] = "Удаляются только файлы старше выбранного порога возраста. Символические ссылки пропускаются.",

        ["cat_windows_temp_title"] = "Системные временные файлы",
        ["cat_windows_temp_desc"] = "Служебные временные файлы Windows в C:\\Windows\\Temp",
        ["cat_windows_temp_safety"] = "Занятые системой файлы безопасно пропускаются. Требуются права администратора.",

        ["cat_recycle_bin_title"] = "Корзина",
        ["cat_recycle_bin_desc"] = "Все удалённые файлы, находящиеся в Корзине на локальных дисках",
        ["cat_recycle_bin_safety"] = "После очистки файлы из Корзины невозможно восстановить стандартными средствами.",

        ["cat_thumbnail_cache_title"] = "Кэш эскизов и иконок",
        ["cat_thumbnail_cache_desc"] = "Базы миниатюр Проводника (thumbcache_*.db, iconcache_*.db)",
        ["cat_thumbnail_cache_safety"] = "Windows автоматически пересоздаст миниатюры при открытии папок. Активные базы пропускаются.",

        ["cat_recent_items_title"] = "Недавние документы",
        ["cat_recent_items_desc"] = "Ярлыки недавно открытых файлов из меню быстрого доступа",
        ["cat_recent_items_safety"] = "Удаляются только ярлыки истории (.lnk), сами документы остаются нетронутыми.",

        ["cat_directx_shader_title"] = "Кэш шейдеров DirectX",
        ["cat_directx_shader_desc"] = "Скомпилированные шейдеры игр и графических приложений (D3DSCache)",
        ["cat_directx_shader_safety"] = "Безопасно, но игры могут заново компилировать шейдеры при первом запуске.",

        ["cat_windows_update_title"] = "Кэш обновлений Windows",
        ["cat_windows_update_desc"] = "Загруженные пакеты из SoftwareDistribution\\Download",
        ["cat_windows_update_safety"] = "Освобождает значительный объём места после установки обновлений. Требуются права администратора.",

        ["cat_delivery_opt_title"] = "Кэш оптимизации доставки",
        ["cat_delivery_opt_desc"] = "Временный кэш раздачи обновлений Windows (Delivery Optimization)",
        ["cat_delivery_opt_safety"] = "Полностью безопасно для системы. Требуются права администратора.",

        ["cat_windows_prefetch_title"] = "Данные Prefetch",
        ["cat_windows_prefetch_desc"] = "Файлы предварительной выборки запуска приложений (C:\\Windows\\Prefetch)",
        ["cat_windows_prefetch_safety"] = "Опционально: Windows пересоздаст файлы автоматически, первые запуски программ могут стать чуть медленнее.",

        ["cat_dns_cache_title"] = "Кэш DNS-резолвера",
        ["cat_dns_cache_desc"] = "Локальный кэш DNS-записей Windows (ipconfig /flushdns)",
        ["cat_dns_cache_safety"] = "Обновляет сетевые маршруты и помогает при смене DNS или проблемах с сайтами.",

        ["cat_wer_reports_title"] = "Отчёты об ошибках Windows",
        ["cat_wer_reports_desc"] = "Архивы и очереди сбоев Windows Error Reporting (WER)",
        ["cat_wer_reports_safety"] = "Старые отчёты о падениях программ, не влияющие на работу Windows.",

        ["cat_crash_dumps_title"] = "Дампы памяти и сбоев",
        ["cat_crash_dumps_desc"] = "Файлы отладки .dmp в CrashDumps и Minidump",
        ["cat_crash_dumps_safety"] = "Могут занимать сотни мегабайт. Отключайте очистку, если анализируете недавние сбои.",

        ["cat_windows_logs_title"] = "Системные журналы Windows",
        ["cat_windows_logs_desc"] = "Файлы журналов CBS, DISM, Panther, setupapi и WindowsUpdate",
        ["cat_windows_logs_safety"] = "Удаляются старые .log и .etl журналы установки и обслуживания. Требуются права администратора.",

        // Badges & Status
        ["badge_admin"] = "Админ",
        ["badge_caution"] = "Опционально",
        ["status_idle"] = "Ожидание",
        ["status_scanning"] = "Сканирование...",
        ["status_analyzed"] = "Проверено",
        ["status_cleaning"] = "Очистка...",
        ["status_cleaned"] = "Очищено",
        ["status_access_denied"] = "Нужны права админа",
        ["status_not_found"] = "Не найдено",
        ["status_cancelled"] = "Отменено",
        ["status_error"] = "Ошибка",

        // Cleaner Hero & Actions
        ["hero_ready_title"] = "Готов к проверке системы",
        ["hero_ready_subtitle"] = "Нажмите «Анализ», чтобы безопасно оценить объём мусора без удаления файлов.",
        ["hero_scanning_title"] = "Идёт анализ системы...",
        ["hero_cleaning_title"] = "Выполняется очистка...",
        ["hero_analyzed_title"] = "Найдено {0} для очистки",
        ["hero_analyzed_subtitle"] = "Файлов: {0} • Время проверки: {1} сек • Ничего ещё не удалено",
        ["hero_cleaned_title"] = "Освобождено {0}",
        ["hero_cleaned_subtitle"] = "Удалено файлов: {0} • Пропущено (занято/защищено): {1} • За {2} сек",
        ["hero_cancelled_title"] = "Операция остановлена",
        ["hero_cancelled_subtitle"] = "Частичные результаты сохранены в таблице ниже.",
        ["btn_analyze"] = "Анализ",
        ["btn_clean"] = "Очистить",
        ["btn_cancel"] = "Отмена",
        ["btn_select_recommended"] = "Безопасные",
        ["btn_select_all"] = "Выбрать все",
        ["btn_select_none"] = "Снять все",

        // Inspector
        ["inspector_title"] = "Детали категории",
        ["inspector_empty_selection"] = "Выберите категорию слева, чтобы увидеть пути и найденные файлы.",
        ["inspector_targets"] = "Целевые пути:",
        ["inspector_no_files"] = "После анализа здесь появятся найденные файлы. Запустите «Анализ» или выберите другую категорию.",
        ["inspector_col_name"] = "Файл / элемент",
        ["inspector_col_size"] = "Размер",
        ["inspector_col_modified"] = "Изменён",
        ["inspector_add_exclusion"] = "В исключения",
        ["inspector_open_folder"] = "Открыть папку",
        ["inspector_truncated_note"] = "Показаны крупнейшие элементы (всего найдено: {0}). При очистке будут удалены все подходящие файлы.",

        // Confirm dialog
        ["confirm_title"] = "Подтверждение очистки",
        ["confirm_message"] = "Будут очищены выбранные категории ({0} шт., около {1}).\n\nЗанятые системой файлы и элементы из списка исключений будут пропущены. Продолжить?",

        // Logs view
        ["logs_title"] = "Журнал операций",
        ["logs_subtitle"] = "Локальная история анализов и очисток • Всего освобождено за всё время: {0}",
        ["logs_clear"] = "Очистить журнал",
        ["logs_export"] = "Экспорт .txt",
        ["logs_empty"] = "История пока пуста. Выполните анализ или очистку.",
        ["logs_action_clean"] = "Очистка",
        ["logs_action_analyze"] = "Анализ",
        ["logs_export_done"] = "Журнал экспортирован: {0}",

        // Exclusions view
        ["excl_title"] = "Исключения",
        ["excl_subtitle"] = "Папки, файлы или шаблоны (например, *.dmp или D:\\Work\\Temp), которые CleanPulse никогда не затронет.",
        ["excl_placeholder"] = "Введите путь или шаблон (например, C:\\Temp\\keep или *.bak)",
        ["excl_add"] = "Добавить",
        ["excl_browse_folder"] = "Выбрать папку",
        ["excl_remove"] = "Удалить",
        ["excl_empty"] = "Список исключений пуст.",

        // Settings view
        ["settings_title"] = "Настройки",
        ["settings_subtitle"] = "Локальные параметры безопасности, интерфейса и хранения конфигурации.",
        ["settings_language"] = "Язык интерфейса",
        ["settings_theme"] = "Тема оформления",
        ["settings_theme_dark"] = "Тёмная",
        ["settings_theme_light"] = "Светлая",
        ["settings_min_age"] = "Минимальный возраст временных файлов",
        ["settings_min_age_desc"] = "Защищает свежие файлы работающих программ и установщиков от преждевременного удаления.",
        ["settings_age_0"] = "0 ч (удалять всё, кроме занятых)",
        ["settings_age_1"] = "1 час",
        ["settings_age_24"] = "24 часа (рекомендуется)",
        ["settings_age_168"] = "7 дней",
        ["settings_confirm_clean"] = "Спрашивать подтверждение перед очисткой",
        ["settings_security_title"] = "Встроенные гарантии безопасности",
        ["settings_security_body"] = "• CleanPulse работает полностью локально без телеметрии и фоновых служб.\n• Системные папки (Windows, System32, WinSxS, Program Files) и личные папки (Рабочий стол, Документы, Загрузки) жёстко заблокированы.\n• Символические ссылки и точки соединения (junctions) никогда не обходятся при очистке.",
        ["settings_about_title"] = "О программе",
        ["settings_about_body"] = "CleanPulse v1.0 • Локальный очиститель системы на .NET 9 WPF",
        ["common_files_word"] = "файлов",
        ["common_yes"] = "Да",
        ["common_no"] = "Нет"
    };

    private static readonly Dictionary<string, string> En = new(StringComparer.Ordinal)
    {
        // App & Nav
        ["app_name"] = "CleanPulse",
        ["app_tagline"] = "Local Windows Cleaner",
        ["nav_cleaner"] = "Cleaner",
        ["nav_logs"] = "History",
        ["nav_exclusions"] = "Exclusions",
        ["nav_settings"] = "Settings",
        ["privilege_admin"] = "Administrator",
        ["privilege_user"] = "Standard Mode",
        ["privilege_elevate"] = "Restart as Admin",
        ["privilege_elevate_hint"] = "Administrator rights are required to clean Windows Temp, Windows Update Cache, and system logs.",
        ["drive_free_of"] = "free of",
        ["drive_system_disk"] = "System Drive",

        // Groups
        ["group_system_temp"] = "System & Temp Files",
        ["group_windows_cache"] = "Windows Cache & Network",
        ["group_logs_dumps"] = "Logs, Reports & Dumps",

        // Categories
        ["cat_user_temp_title"] = "User Temporary Files",
        ["cat_user_temp_desc"] = "Temporary app and installer files inside %TEMP%",
        ["cat_user_temp_safety"] = "Only deletes files older than the configured age threshold. Symbolic links are skipped.",

        ["cat_windows_temp_title"] = "Windows System Temp",
        ["cat_windows_temp_desc"] = "System-wide temporary files in C:\\Windows\\Temp",
        ["cat_windows_temp_safety"] = "Locked files in use by Windows are safely skipped. Requires Administrator privileges.",

        ["cat_recycle_bin_title"] = "Recycle Bin",
        ["cat_recycle_bin_desc"] = "All deleted items currently held in the Recycle Bin across local drives",
        ["cat_recycle_bin_safety"] = "Emptied files cannot be restored through the Recycle Bin.",

        ["cat_thumbnail_cache_title"] = "Thumbnail & Icon Cache",
        ["cat_thumbnail_cache_desc"] = "Explorer thumbnail databases (thumbcache_*.db, iconcache_*.db)",
        ["cat_thumbnail_cache_safety"] = "Windows automatically rebuilds thumbnails as needed. Active locked files are skipped.",

        ["cat_recent_items_title"] = "Recent Documents",
        ["cat_recent_items_desc"] = "Quick Access shortcuts to recently opened files",
        ["cat_recent_items_safety"] = "Only removes shortcut links (.lnk); your actual documents remain untouched.",

        ["cat_directx_shader_title"] = "DirectX Shader Cache",
        ["cat_directx_shader_desc"] = "Precompiled DirectX shader caches in D3DSCache",
        ["cat_directx_shader_safety"] = "Safe to clean, though games may recompile shaders on next launch.",

        ["cat_windows_update_title"] = "Windows Update Cache",
        ["cat_windows_update_desc"] = "Downloaded update packages in SoftwareDistribution\\Download",
        ["cat_windows_update_safety"] = "Reclaims substantial space after updates finish installing. Requires Administrator privileges.",

        ["cat_delivery_opt_title"] = "Delivery Optimization Cache",
        ["cat_delivery_opt_desc"] = "Peer update distribution cache (Delivery Optimization)",
        ["cat_delivery_opt_safety"] = "Completely safe to remove. Requires Administrator privileges.",

        ["cat_windows_prefetch_title"] = "Windows Prefetch Data",
        ["cat_windows_prefetch_desc"] = "App launch preloading hints (C:\\Windows\\Prefetch)",
        ["cat_windows_prefetch_safety"] = "Optional: Windows regenerates these automatically; initial app launches may be slightly slower.",

        ["cat_dns_cache_title"] = "DNS Resolver Cache",
        ["cat_dns_cache_desc"] = "Local Windows DNS lookup cache (ipconfig /flushdns)",
        ["cat_dns_cache_safety"] = "Refreshes network host entries and resolves stale DNS records.",

        ["cat_wer_reports_title"] = "Windows Error Reports",
        ["cat_wer_reports_desc"] = "Archived crash and diagnostic reports (WER)",
        ["cat_wer_reports_safety"] = "Old diagnostic report folders that are safe to delete.",

        ["cat_crash_dumps_title"] = "Crash & Memory Dumps",
        ["cat_crash_dumps_desc"] = "Diagnostic .dmp files in CrashDumps and Minidump",
        ["cat_crash_dumps_safety"] = "Can consume hundreds of MB. Keep unchecked if troubleshooting a recent crash.",

        ["cat_windows_logs_title"] = "Windows Log Files",
        ["cat_windows_logs_desc"] = "CBS, DISM, Panther, setupapi, and WindowsUpdate logs",
        ["cat_windows_logs_safety"] = "Removes old servicing and setup .log/.etl files. Requires Administrator privileges.",

        // Badges & Status
        ["badge_admin"] = "Admin",
        ["badge_caution"] = "Optional",
        ["status_idle"] = "Ready",
        ["status_scanning"] = "Scanning...",
        ["status_analyzed"] = "Analyzed",
        ["status_cleaning"] = "Cleaning...",
        ["status_cleaned"] = "Cleaned",
        ["status_access_denied"] = "Admin Required",
        ["status_not_found"] = "Not Found",
        ["status_cancelled"] = "Cancelled",
        ["status_error"] = "Error",

        // Cleaner Hero & Actions
        ["hero_ready_title"] = "Ready to Scan System",
        ["hero_ready_subtitle"] = "Click Analyze to safely inspect reclaimable space before deleting anything.",
        ["hero_scanning_title"] = "Analyzing system...",
        ["hero_cleaning_title"] = "Cleaning selected items...",
        ["hero_analyzed_title"] = "{0} ready to clean",
        ["hero_analyzed_subtitle"] = "Files: {0} • Scan duration: {1} s • Nothing deleted yet",
        ["hero_cleaned_title"] = "Freed {0}",
        ["hero_cleaned_subtitle"] = "Files removed: {0} • Skipped (locked/protected): {1} • In {2} s",
        ["hero_cancelled_title"] = "Operation Cancelled",
        ["hero_cancelled_subtitle"] = "Partial results are displayed below.",
        ["btn_analyze"] = "Analyze",
        ["btn_clean"] = "Clean",
        ["btn_cancel"] = "Cancel",
        ["btn_select_recommended"] = "Recommended",
        ["btn_select_all"] = "Select All",
        ["btn_select_none"] = "Clear All",

        // Inspector
        ["inspector_title"] = "Category Details",
        ["inspector_empty_selection"] = "Select a category on the left to inspect its target paths and discovered files.",
        ["inspector_targets"] = "Target Paths:",
        ["inspector_no_files"] = "Run Analyze or select another category to view discovered items.",
        ["inspector_col_name"] = "File / Item",
        ["inspector_col_size"] = "Size",
        ["inspector_col_modified"] = "Modified",
        ["inspector_add_exclusion"] = "Exclude",
        ["inspector_open_folder"] = "Open Folder",
        ["inspector_truncated_note"] = "Showing largest items (total found: {0}). Clean removes all matching items.",

        // Confirm dialog
        ["confirm_title"] = "Confirm Cleanup",
        ["confirm_message"] = "Clean {0} selected categories (approx. {1})?\n\nIn-use system files and excluded items will be skipped safely.",

        // Logs view
        ["logs_title"] = "Operation History",
        ["logs_subtitle"] = "Local scan & cleanup history • Lifetime space freed: {0}",
        ["logs_clear"] = "Clear History",
        ["logs_export"] = "Export .txt",
        ["logs_empty"] = "No history entries yet. Run an analysis or cleanup first.",
        ["logs_action_clean"] = "Clean",
        ["logs_action_analyze"] = "Analyze",
        ["logs_export_done"] = "History exported to: {0}",

        // Exclusions view
        ["excl_title"] = "Exclusions",
        ["excl_subtitle"] = "Paths or wildcard patterns (e.g. *.dmp or D:\\Work\\Temp) that CleanPulse will never touch.",
        ["excl_placeholder"] = "Enter path or wildcard pattern (e.g. C:\\Temp\\keep or *.bak)",
        ["excl_add"] = "Add",
        ["excl_browse_folder"] = "Browse Folder",
        ["excl_remove"] = "Remove",
        ["excl_empty"] = "No exclusions configured.",

        // Settings view
        ["settings_title"] = "Settings",
        ["settings_subtitle"] = "Local safety rules, appearance, and configuration.",
        ["settings_language"] = "Interface Language",
        ["settings_theme"] = "Appearance Theme",
        ["settings_theme_dark"] = "Dark",
        ["settings_theme_light"] = "Light",
        ["settings_min_age"] = "Minimum Temporary File Age",
        ["settings_min_age_desc"] = "Protects recently created files used by active apps and installers.",
        ["settings_age_0"] = "0 hours (everything unlocked)",
        ["settings_age_1"] = "1 hour",
        ["settings_age_24"] = "24 hours (recommended)",
        ["settings_age_168"] = "7 days",
        ["settings_confirm_clean"] = "Ask for confirmation before cleaning",
        ["settings_security_title"] = "Built-in Safety Guarantees",
        ["settings_security_body"] = "• CleanPulse runs 100% locally with zero telemetry or background services.\n• Core OS paths (Windows, System32, WinSxS, Program Files) and user libraries (Desktop, Documents, Downloads) are hard-blocked.\n• Symbolic links and directory junctions are never traversed during cleanup.",
        ["settings_about_title"] = "About",
        ["settings_about_body"] = "CleanPulse v1.0 • Local .NET 9 WPF System Cleaner",
        ["common_files_word"] = "files",
        ["common_yes"] = "Yes",
        ["common_no"] = "No"
    };

    public static IReadOnlyDictionary<string, string> GetDictionary(string language) =>
        string.Equals(language, "en", StringComparison.OrdinalIgnoreCase) ? En : Ru;

    public static string Get(string key, string language = "ru")
    {
        var dict = GetDictionary(language);
        return dict.TryGetValue(key, out string? value) ? value : key;
    }

    public static string Format(string key, string language, params object[] args)
    {
        string template = Get(key, language);
        return string.Format(template, args);
    }
}
