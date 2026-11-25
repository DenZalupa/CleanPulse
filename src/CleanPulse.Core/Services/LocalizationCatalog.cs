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
        ["nav_analyzer"] = "Анализатор диска",
        ["nav_duplicates"] = "Дубликаты",
        ["nav_startup"] = "Автозагрузка",
        ["nav_rules"] = "Правила и исключения",
        ["nav_logs"] = "Журнал",
        ["nav_settings"] = "Настройки",
        ["privilege_admin"] = "Администратор",
        ["privilege_user"] = "Стандартный режим",
        ["privilege_elevate"] = "Запустить как админ",
        ["privilege_elevate_hint"] = "Для очистки системного Temp, кэша обновлений, DISM и HKLM автозагрузки нужны права администратора.",
        ["drive_free_of"] = "свободно из",
        ["drive_system_disk"] = "Системный диск",

        // Groups
        ["group_system_temp"] = "Система и временные файлы",
        ["group_windows_cache"] = "Кэш Windows и сеть",
        ["group_browsers"] = "Браузеры (только кэш)",
        ["group_dev_apps"] = "Инструменты разработки",
        ["group_logs_dumps"] = "Логи, отчёты и дампы",
        ["group_custom_rules"] = "Пользовательские правила",

        // Categories (Built-in)
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

        ["cat_gpu_cache_title"] = "Кэш видеодрайверов (NVIDIA / AMD / Intel)",
        ["cat_gpu_cache_desc"] = "Кэш DXCache / GLCache / ShaderCache видеокарты",
        ["cat_gpu_cache_safety"] = "Помогает после обновления драйверов GPU; при первом запуске игр кэш соберётся заново.",

        ["cat_windows_update_title"] = "Кэш обновлений Windows",
        ["cat_windows_update_desc"] = "Загруженные пакеты из SoftwareDistribution\\Download",
        ["cat_windows_update_safety"] = "Освобождает значительный объём места после установки обновлений. Требуются права администратора.",

        ["cat_delivery_opt_title"] = "Кэш оптимизации доставки",
        ["cat_delivery_opt_desc"] = "Временный кэш раздачи обновлений Windows (Delivery Optimization)",
        ["cat_delivery_opt_safety"] = "Полностью безопасно для системы. Требуются права администратора.",

        ["cat_windows_prefetch_title"] = "Данные Prefetch",
        ["cat_windows_prefetch_desc"] = "Файлы предварительной выборки запуска приложений (C:\\Windows\\Prefetch)",
        ["cat_windows_prefetch_safety"] = "Опционально: Windows пересоздаст файлы автоматически, первые запуски программ могут стать чуть медленнее.",

        ["cat_windows_old_title"] = "Старая версия Windows (Windows.old)",
        ["cat_windows_old_desc"] = "Резервная копия предыдущей сборки Windows после обновления",
        ["cat_windows_old_safety"] = "Внимание: после удаления вы не сможете откатиться к предыдущей версии Windows. Требуются права администратора.",

        ["cat_dns_cache_title"] = "Кэш DNS-резолвера",
        ["cat_dns_cache_desc"] = "Локальный кэш DNS-записей Windows (ipconfig /flushdns)",
        ["cat_dns_cache_safety"] = "Обновляет сетевые маршруты и помогает при смене DNS или проблемах с сайтами.",

        ["cat_browser_chrome_title"] = "Google Chrome — кэш",
        ["cat_browser_chrome_desc"] = "Cache, Code Cache, GPUCache и GrShaderCache всех профилей Chrome",
        ["cat_browser_edge_title"] = "Microsoft Edge — кэш",
        ["cat_browser_edge_desc"] = "Cache, Code Cache и GPUCache всех профилей Edge",
        ["cat_browser_yandex_title"] = "Яндекс Браузер — кэш",
        ["cat_browser_yandex_desc"] = "Cache, Code Cache и GPUCache всех профилей Яндекс Браузера",
        ["cat_browser_firefox_title"] = "Mozilla Firefox — кэш",
        ["cat_browser_firefox_desc"] = "Папки cache2 и startupCache всех профилей Firefox",
        ["cat_browser_opera_title"] = "Opera / Opera GX — кэш",
        ["cat_browser_opera_desc"] = "Кэш и GPUCache браузеров Opera и Opera GX",
        ["cat_browser_cache_safety"] = "Пароли, закладки, cookies, история и открытые вкладки НЕ затрагиваются. Для полной очистки закройте браузер.",

        ["cat_dev_nuget_title"] = "NuGet HTTP и плагины",
        ["cat_dev_nuget_desc"] = "Локальный HTTP-кэш NuGet (v3-cache, plugins-cache)",
        ["cat_dev_nuget_safety"] = "Установленные пакеты в .nuget\\packages не удаляются, очищается только сетевой кэш.",

        ["cat_dev_pip_npm_title"] = "Кэш пакетов pip и npm",
        ["cat_dev_pip_npm_desc"] = "Кэш загруженных архивов pip\\Cache и npm-cache",
        ["cat_dev_pip_npm_safety"] = "Безопасно: при следующей установке пакетов архивы скачаются повторно.",

        ["cat_dev_vscode_title"] = "VS Code — кэш и логи",
        ["cat_dev_vscode_desc"] = "Временные файлы Cache, CachedData, GPUCache и logs редактора",
        ["cat_dev_vscode_safety"] = "Настройки, расширения и открытые проекты остаются нетронутыми.",

        ["cat_wer_reports_title"] = "Отчёты об ошибках Windows",
        ["cat_wer_reports_desc"] = "Архивы и очереди сбоев Windows Error Reporting (WER)",
        ["cat_wer_reports_safety"] = "Старые отчёты о падениях программ, не влияющие на работу Windows.",

        ["cat_crash_dumps_title"] = "Дампы памяти и сбоев",
        ["cat_crash_dumps_desc"] = "Файлы отладки .dmp в CrashDumps и Minidump",
        ["cat_crash_dumps_safety"] = "Могут занимать сотни мегабайт. Отключайте очистку, если анализируете недавние сбои.",

        ["cat_windows_logs_title"] = "Системные журналы Windows",
        ["cat_windows_logs_desc"] = "Файлы журналов CBS, DISM, Panther, setupapi и WindowsUpdate",
        ["cat_windows_logs_safety"] = "Удаляются старые .log и .etl журналы установки и обслуживания. Требуются права администратора.",

        ["cat_custom_rule_title"] = "Пользовательское правило",
        ["cat_custom_rule_desc"] = "Пользовательская папка очистки",
        ["cat_custom_rule_safety_recycle"] = "Пользовательское правило: подходящие файлы перемещаются в Корзину.",
        ["cat_custom_rule_safety_perm"] = "Пользовательское правило: подходящие файлы удаляются безвозвратно.",

        // Badges & Status
        ["badge_admin"] = "Админ",
        ["badge_caution"] = "Опционально",
        ["badge_browser_open"] = "Запущен",
        ["badge_custom"] = "Своё",
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

        // DISM Maintenance
        ["dism_title"] = "Очистка компонентов Windows (WinSxS)",
        ["dism_desc"] = "Запускает встроенную утилиту DISM /StartComponentCleanup для безопасного удаления старых версий обновлений.",
        ["dism_run"] = "Очистить WinSxS (DISM)",
        ["dism_running"] = "Выполняется DISM...",
        ["dism_admin_needed"] = "Для запуска DISM требуются права администратора.",
        ["dism_done"] = "DISM успешно завершён за {0} сек.",
        ["dism_failed"] = "DISM завершился с кодом {0}: {1}",

        // Inspector
        ["inspector_title"] = "Детали категории",
        ["inspector_empty_selection"] = "Выберите категорию слева, чтобы увидеть пути и найденные файлы.",
        ["inspector_targets"] = "Целевые пути:",
        ["inspector_no_files"] = "После анализа здесь появятся найденные файлы. Запустите «Анализ» или выберите другую категорию.",
        ["inspector_search_hint"] = "Фильтр по имени или пути...",
        ["inspector_sort_size"] = "По размеру",
        ["inspector_sort_name"] = "По имени",
        ["inspector_sort_date"] = "По дате",
        ["inspector_add_exclusion"] = "В исключения",
        ["inspector_open_folder"] = "Открыть папку",
        ["inspector_recycle_item"] = "Переместить файл в Корзину",
        ["inspector_truncated_note"] = "Показано до 5 000 элементов (всего найдено: {0}). При очистке будут удалены все подходящие файлы.",

        // Confirm dialog
        ["confirm_title"] = "Подтверждение очистки",
        ["confirm_message"] = "Будут очищены выбранные категории ({0} шт., около {1}).\n\nЗанятые системой файлы и элементы из списка исключений будут пропущены. Продолжить?",
        ["confirm_delete_file"] = "Удалить файл?\n\n{0}",

        // Disk Analyzer
        ["analyzer_title"] = "Анализатор места на диске",
        ["analyzer_subtitle"] = "Находит папки и самые крупные файлы, занимающие больше всего пространства.",
        ["analyzer_browse"] = "Выбрать папку",
        ["analyzer_scan"] = "Сканировать",
        ["analyzer_scanning"] = "Анализ диска...",
        ["analyzer_summary"] = "Проверено {0} ({1} файлов) за {2} сек • Пропущено системных папок: {3}",
        ["analyzer_folders_title"] = "Крупнейшие папки",
        ["analyzer_files_title"] = "Самые большие файлы (Топ-150)",
        ["analyzer_filter_all"] = "Все",
        ["analyzer_filter_archives"] = "Архивы",
        ["analyzer_filter_images"] = "Образы (.iso)",
        ["analyzer_filter_installers"] = "Установщики",
        ["analyzer_filter_media"] = "Медиа",
        ["analyzer_filter_temp"] = "Временные/Дампы",
        ["analyzer_recycle"] = "В Корзину",
        ["analyzer_delete_perm"] = "Удалить",
        ["analyzer_empty"] = "Выберите диск или папку и нажмите «Сканировать».",
        ["analyzer_protected_warn"] = "Системные файлы Windows защищены от удаления.",

        // Duplicate Finder
        ["dup_title"] = "Поиск дубликатов",
        ["dup_subtitle"] = "Сравнивает файлы по размеру, заголовку и полному SHA-256, оставляя выбранный оригинал нетронутым.",
        ["dup_folder_label"] = "Папка для поиска:",
        ["dup_min_size"] = "Мин. размер:",
        ["dup_scan"] = "Найти дубликаты",
        ["dup_scanning"] = "Поиск дубликатов...",
        ["dup_summary"] = "Найдено групп: {0} • Можно освободить: {1} • Проверено файлов: {2} за {3} сек",
        ["dup_select_copies"] = "Выбрать копии (оставить старые)",
        ["dup_select_newest"] = "Выбрать копии (оставить новые)",
        ["dup_clear_sel"] = "Снять выбор",
        ["dup_recycle_selected"] = "Удалить выбранные в Корзину",
        ["dup_deleted_result"] = "Удалено копий: {0} ({1}), пропущено: {2}.",
        ["dup_empty"] = "Выберите папку (например, «Загрузки» или «Документы») и запустите поиск.",
        ["dup_none_found"] = "Дубликаты с заданным размером не найдены.",
        ["dup_group_header"] = "Размер файла: {0} • Копий: {1} • Можно освободить: {2}",

        // Startup Manager
        ["startup_title"] = "Менеджер автозагрузки",
        ["startup_subtitle"] = "Безопасное отключение программ через встроенный механизм Windows StartupApproved (без потери записей).",
        ["startup_refresh"] = "Обновить",
        ["startup_filter_hint"] = "Поиск по названию или команде...",
        ["startup_enabled"] = "Вкл",
        ["startup_disabled"] = "Выкл",
        ["startup_admin_warn"] = "Для изменения записей HKLM / общей автозагрузки требуются права администратора.",
        ["startup_empty"] = "Элементы автозагрузки не найдены.",

        // Rules & Exclusions
        ["rules_title"] = "Правила и исключения",
        ["rules_subtitle"] = "Создавайте свои папки очистки и настраивайте пути, которые CleanPulse никогда не затронет.",
        ["rules_custom_header"] = "Свои папки для очистки",
        ["rules_name_hint"] = "Название (напр. Логи проектов)",
        ["rules_path_hint"] = "Путь к папке",
        ["rules_pattern_hint"] = "Маска (напр. *.log или *)",
        ["rules_recursive"] = "Включая подпапки",
        ["rules_respect_age"] = "Учитывать мин. возраст",
        ["rules_to_recycle"] = "В Корзину (безопаснее)",
        ["rules_add_btn"] = "Добавить правило",
        ["rules_custom_empty"] = "Пользовательских правил пока нет.",
        ["rules_protected_error"] = "Нельзя добавить системную или личную папку (Windows, System32, Рабочий стол, Документы, Загрузки).",

        ["excl_title"] = "Исключения (никогда не удалять)",
        ["excl_subtitle"] = "Папки, файлы или шаблоны (например, *.dmp или D:\\Work\\Temp), которые CleanPulse всегда пропускает.",
        ["excl_placeholder"] = "Введите путь или шаблон (например, C:\\Temp\\keep или *.bak)",
        ["excl_add"] = "Добавить",
        ["excl_browse_folder"] = "Выбрать папку",
        ["excl_remove"] = "Удалить",
        ["excl_empty"] = "Список исключений пуст.",

        // Logs view
        ["logs_title"] = "Журнал операций",
        ["logs_subtitle"] = "Локальная история анализов и очисток • Всего освобождено за всё время: {0}",
        ["logs_clear"] = "Очистить журнал",
        ["logs_export"] = "Экспорт .txt",
        ["logs_empty"] = "История пока пуста. Выполните анализ или очистку.",
        ["logs_action_clean"] = "Очистка",
        ["logs_action_analyze"] = "Анализ",
        ["logs_export_done"] = "Журнал экспортирован: {0}",

        // Settings view
        ["settings_title"] = "Настройки",
        ["settings_subtitle"] = "Локальные параметры безопасности, расписания, интерфейса и CLI.",
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
        ["settings_auto_analyze"] = "Запускать автоматический анализ при открытии программы",
        ["settings_schedule"] = "Автоочистка по расписанию (Планировщик Windows)",
        ["settings_schedule_desc"] = "Создаёт локальную задачу Windows (CleanPulse.exe --clean-safe --silent) для безопасных категорий.",
        ["sched_disabled"] = "Отключено",
        ["sched_daily"] = "Ежедневно (10:00)",
        ["sched_weekly"] = "Еженедельно (Вс 10:00)",
        ["sched_monthly"] = "Ежемесячно (1 числа)",
        ["sched_error"] = "Не удалось обновить задачу Планировщика: {0}",
        ["settings_cli_title"] = "Запуск из терминала (CLI)",
        ["settings_cli_body"] = "CleanPulse.exe --analyze   |   CleanPulse.exe --clean-safe --silent   |   CleanPulse.exe --clean-selected",
        ["settings_security_title"] = "Встроенные гарантии безопасности",
        ["settings_security_body"] = "• CleanPulse работает 100% локально без телеметрии и фоновых служб.\n• Системные папки (Windows, System32, WinSxS, Program Files) и личные папки (Рабочий стол, Документы, Загрузки) защищены от автоматической очистки.\n• Символические ссылки и точки соединения (junctions) никогда не обходятся при сканировании.",
        ["settings_about_title"] = "О программе",
        ["settings_about_body"] = "CleanPulse v1.1 • Локальный очиститель и набор утилит на .NET 9 WPF",
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
        ["nav_analyzer"] = "Disk Analyzer",
        ["nav_duplicates"] = "Duplicates",
        ["nav_startup"] = "Startup",
        ["nav_rules"] = "Rules & Exclusions",
        ["nav_logs"] = "History",
        ["nav_settings"] = "Settings",
        ["privilege_admin"] = "Administrator",
        ["privilege_user"] = "Standard Mode",
        ["privilege_elevate"] = "Restart as Admin",
        ["privilege_elevate_hint"] = "Administrator rights are required for Windows Temp, Update Cache, DISM WinSxS cleanup, and HKLM startup entries.",
        ["drive_free_of"] = "free of",
        ["drive_system_disk"] = "System Drive",

        // Groups
        ["group_system_temp"] = "System & Temp Files",
        ["group_windows_cache"] = "Windows Cache & Network",
        ["group_browsers"] = "Browsers (Cache Only)",
        ["group_dev_apps"] = "Developer & App Caches",
        ["group_logs_dumps"] = "Logs, Reports & Dumps",
        ["group_custom_rules"] = "Custom Rules",

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

        ["cat_gpu_cache_title"] = "GPU Driver Cache (NVIDIA / AMD / Intel)",
        ["cat_gpu_cache_desc"] = "Graphics driver DXCache / GLCache / ShaderCache folders",
        ["cat_gpu_cache_safety"] = "Useful after GPU driver updates; shaders rebuild cleanly when games run.",

        ["cat_windows_update_title"] = "Windows Update Cache",
        ["cat_windows_update_desc"] = "Downloaded update packages in SoftwareDistribution\\Download",
        ["cat_windows_update_safety"] = "Reclaims substantial space after updates finish installing. Requires Administrator privileges.",

        ["cat_delivery_opt_title"] = "Delivery Optimization Cache",
        ["cat_delivery_opt_desc"] = "Peer update distribution cache (Delivery Optimization)",
        ["cat_delivery_opt_safety"] = "Completely safe to remove. Requires Administrator privileges.",

        ["cat_windows_prefetch_title"] = "Windows Prefetch Data",
        ["cat_windows_prefetch_desc"] = "App launch preloading hints (C:\\Windows\\Prefetch)",
        ["cat_windows_prefetch_safety"] = "Optional: Windows regenerates these automatically; initial app launches may be slightly slower.",

        ["cat_windows_old_title"] = "Previous Windows Install (Windows.old)",
        ["cat_windows_old_desc"] = "Backup of previous Windows build created during feature upgrade",
        ["cat_windows_old_safety"] = "Caution: removing Windows.old disables rolling back to the previous Windows version. Requires Admin.",

        ["cat_dns_cache_title"] = "DNS Resolver Cache",
        ["cat_dns_cache_desc"] = "Local Windows DNS lookup cache (ipconfig /flushdns)",
        ["cat_dns_cache_safety"] = "Refreshes network host entries and resolves stale DNS records.",

        ["cat_browser_chrome_title"] = "Google Chrome — Cache",
        ["cat_browser_chrome_desc"] = "Cache, Code Cache, GPUCache, and GrShaderCache across Chrome profiles",
        ["cat_browser_edge_title"] = "Microsoft Edge — Cache",
        ["cat_browser_edge_desc"] = "Cache, Code Cache, and GPUCache across Edge profiles",
        ["cat_browser_yandex_title"] = "Yandex Browser — Cache",
        ["cat_browser_yandex_desc"] = "Cache, Code Cache, and GPUCache across Yandex Browser profiles",
        ["cat_browser_firefox_title"] = "Mozilla Firefox — Cache",
        ["cat_browser_firefox_desc"] = "cache2 and startupCache folders across Firefox profiles",
        ["cat_browser_opera_title"] = "Opera / Opera GX — Cache",
        ["cat_browser_opera_desc"] = "Cache and GPUCache folders for Opera and Opera GX",
        ["cat_browser_cache_safety"] = "Passwords, cookies, bookmarks, history, and open tabs are NEVER touched. Close the browser for full cleanup.",

        ["cat_dev_nuget_title"] = "NuGet HTTP & Plugin Cache",
        ["cat_dev_nuget_desc"] = "Local NuGet network cache (v3-cache, plugins-cache)",
        ["cat_dev_nuget_safety"] = "Installed packages in .nuget\\packages are preserved; only HTTP metadata cache is cleared.",

        ["cat_dev_pip_npm_title"] = "pip & npm Package Cache",
        ["cat_dev_pip_npm_desc"] = "Downloaded archive caches in pip\\Cache and npm-cache",
        ["cat_dev_pip_npm_safety"] = "Safe to clean: package managers re-fetch archives on demand.",

        ["cat_dev_vscode_title"] = "VS Code — Cache & Logs",
        ["cat_dev_vscode_desc"] = "Editor Cache, CachedData, GPUCache, and logs",
        ["cat_dev_vscode_safety"] = "Settings, extensions, and workspaces remain untouched.",

        ["cat_wer_reports_title"] = "Windows Error Reports",
        ["cat_wer_reports_desc"] = "Archived crash and diagnostic reports (WER)",
        ["cat_wer_reports_safety"] = "Old diagnostic report folders that are safe to delete.",

        ["cat_crash_dumps_title"] = "Crash & Memory Dumps",
        ["cat_crash_dumps_desc"] = "Diagnostic .dmp files in CrashDumps and Minidump",
        ["cat_crash_dumps_safety"] = "Can consume hundreds of MB. Keep unchecked if troubleshooting a recent crash.",

        ["cat_windows_logs_title"] = "Windows Log Files",
        ["cat_windows_logs_desc"] = "CBS, DISM, Panther, setupapi, and WindowsUpdate logs",
        ["cat_windows_logs_safety"] = "Removes old servicing and setup .log/.etl files. Requires Administrator privileges.",

        ["cat_custom_rule_title"] = "Custom Cleanup Rule",
        ["cat_custom_rule_desc"] = "User-configured folder rule",
        ["cat_custom_rule_safety_recycle"] = "Custom rule: matching files are moved safely to the Recycle Bin.",
        ["cat_custom_rule_safety_perm"] = "Custom rule: matching files are permanently deleted.",

        // Badges & Status
        ["badge_admin"] = "Admin",
        ["badge_caution"] = "Optional",
        ["badge_browser_open"] = "Running",
        ["badge_custom"] = "Custom",
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

        // DISM Maintenance
        ["dism_title"] = "Windows Component Store Cleanup (WinSxS)",
        ["dism_desc"] = "Runs native DISM /StartComponentCleanup to safely purge superseded Windows Update backups.",
        ["dism_run"] = "Clean WinSxS (DISM)",
        ["dism_running"] = "Running DISM...",
        ["dism_admin_needed"] = "Administrator privileges are required to run DISM.",
        ["dism_done"] = "DISM completed successfully in {0} s.",
        ["dism_failed"] = "DISM exited with code {0}: {1}",

        // Inspector
        ["inspector_title"] = "Category Details",
        ["inspector_empty_selection"] = "Select a category on the left to inspect its target paths and discovered files.",
        ["inspector_targets"] = "Target Paths:",
        ["inspector_no_files"] = "Run Analyze or select another category to view discovered items.",
        ["inspector_search_hint"] = "Filter by name or path...",
        ["inspector_sort_size"] = "Sort: Size",
        ["inspector_sort_name"] = "Sort: Name",
        ["inspector_sort_date"] = "Sort: Date",
        ["inspector_add_exclusion"] = "Exclude",
        ["inspector_open_folder"] = "Open Folder",
        ["inspector_recycle_item"] = "Move file to Recycle Bin",
        ["inspector_truncated_note"] = "Showing up to 5,000 items (total found: {0}). Clean removes all matching items.",

        // Confirm dialog
        ["confirm_title"] = "Confirm Cleanup",
        ["confirm_message"] = "Clean {0} selected categories (approx. {1})?\n\nIn-use system files and excluded items will be skipped safely.",
        ["confirm_delete_file"] = "Delete file?\n\n{0}",

        // Disk Analyzer
        ["analyzer_title"] = "Disk Space Analyzer",
        ["analyzer_subtitle"] = "Identifies largest folders and heavy files consuming drive space.",
        ["analyzer_browse"] = "Browse Folder",
        ["analyzer_scan"] = "Scan",
        ["analyzer_scanning"] = "Scanning disk...",
        ["analyzer_summary"] = "Scanned {0} ({1} files) in {2} s • Skipped protected dirs: {3}",
        ["analyzer_folders_title"] = "Largest Folders",
        ["analyzer_files_title"] = "Largest Files (Top 150)",
        ["analyzer_filter_all"] = "All",
        ["analyzer_filter_archives"] = "Archives",
        ["analyzer_filter_images"] = "Disk Images",
        ["analyzer_filter_installers"] = "Installers",
        ["analyzer_filter_media"] = "Media",
        ["analyzer_filter_temp"] = "Temp / Dumps",
        ["analyzer_recycle"] = "Recycle",
        ["analyzer_delete_perm"] = "Delete",
        ["analyzer_empty"] = "Select a drive or folder and click Scan.",
        ["analyzer_protected_warn"] = "Core Windows system files are protected from deletion.",

        // Duplicate Finder
        ["dup_title"] = "Duplicate File Finder",
        ["dup_subtitle"] = "Matches files by size, header hash, and full SHA-256 while keeping one copy safe.",
        ["dup_folder_label"] = "Target Folder:",
        ["dup_min_size"] = "Min Size:",
        ["dup_scan"] = "Find Duplicates",
        ["dup_scanning"] = "Scanning for duplicates...",
        ["dup_summary"] = "Groups found: {0} • Reclaimable: {1} • Scanned files: {2} in {3} s",
        ["dup_select_copies"] = "Select Copies (Keep Oldest)",
        ["dup_select_newest"] = "Select Copies (Keep Newest)",
        ["dup_clear_sel"] = "Clear Selection",
        ["dup_recycle_selected"] = "Recycle Selected Copies",
        ["dup_deleted_result"] = "Removed {0} copies ({1}), skipped {2}.",
        ["dup_empty"] = "Choose a folder (such as Downloads or Documents) and start scanning.",
        ["dup_none_found"] = "No duplicates matching the minimum size were found.",
        ["dup_group_header"] = "File size: {0} • Copies: {1} • Reclaimable: {2}",

        // Startup Manager
        ["startup_title"] = "Startup Manager",
        ["startup_subtitle"] = "Safely toggle autostart apps using native Windows StartupApproved flags without deleting entries.",
        ["startup_refresh"] = "Refresh",
        ["startup_filter_hint"] = "Filter by app name or command...",
        ["startup_enabled"] = "Enabled",
        ["startup_disabled"] = "Disabled",
        ["startup_admin_warn"] = "Administrator rights are required to toggle HKLM / All Users startup items.",
        ["startup_empty"] = "No startup items found.",

        // Rules & Exclusions
        ["rules_title"] = "Custom Rules & Exclusions",
        ["rules_subtitle"] = "Add your own cleanup folders and configure paths CleanPulse must never touch.",
        ["rules_custom_header"] = "Custom Cleanup Folders",
        ["rules_name_hint"] = "Rule name (e.g. Project Logs)",
        ["rules_path_hint"] = "Folder path",
        ["rules_pattern_hint"] = "Pattern (e.g. *.log or *)",
        ["rules_recursive"] = "Include subfolders",
        ["rules_respect_age"] = "Respect min age",
        ["rules_to_recycle"] = "Send to Recycle Bin",
        ["rules_add_btn"] = "Add Rule",
        ["rules_custom_empty"] = "No custom rules added yet.",
        ["rules_protected_error"] = "Cannot target protected OS or personal library folders (Windows, System32, Desktop, Documents, Downloads).",

        ["excl_title"] = "Exclusions (Never Touch)",
        ["excl_subtitle"] = "Paths or wildcard patterns (e.g. *.dmp or D:\\Work\\Temp) that CleanPulse will always skip.",
        ["excl_placeholder"] = "Enter path or wildcard pattern (e.g. C:\\Temp\\keep or *.bak)",
        ["excl_add"] = "Add",
        ["excl_browse_folder"] = "Browse Folder",
        ["excl_remove"] = "Remove",
        ["excl_empty"] = "No exclusions configured.",

        // Logs view
        ["logs_title"] = "Operation History",
        ["logs_subtitle"] = "Local scan & cleanup history • Lifetime space freed: {0}",
        ["logs_clear"] = "Clear History",
        ["logs_export"] = "Export .txt",
        ["logs_empty"] = "No history entries yet. Run an analysis or cleanup first.",
        ["logs_action_clean"] = "Clean",
        ["logs_action_analyze"] = "Analyze",
        ["logs_export_done"] = "History exported to: {0}",

        // Settings view
        ["settings_title"] = "Settings",
        ["settings_subtitle"] = "Local safety rules, scheduled cleanup, appearance, and CLI usage.",
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
        ["settings_auto_analyze"] = "Automatically run Analyze on startup",
        ["settings_schedule"] = "Scheduled Auto-Cleanup (Windows Task Scheduler)",
        ["settings_schedule_desc"] = "Registers a local Windows scheduled task (CleanPulse.exe --clean-safe --silent) for safe categories.",
        ["sched_disabled"] = "Disabled",
        ["sched_daily"] = "Daily (10:00 AM)",
        ["sched_weekly"] = "Weekly (Sun 10:00 AM)",
        ["sched_monthly"] = "Monthly (1st day)",
        ["sched_error"] = "Failed to update scheduled task: {0}",
        ["settings_cli_title"] = "Command-Line Usage (CLI)",
        ["settings_cli_body"] = "CleanPulse.exe --analyze   |   CleanPulse.exe --clean-safe --silent   |   CleanPulse.exe --clean-selected",
        ["settings_security_title"] = "Built-in Safety Guarantees",
        ["settings_security_body"] = "• CleanPulse runs 100% locally with zero telemetry or background services.\n• Core OS paths (Windows, System32, WinSxS, Program Files) and user libraries (Desktop, Documents, Downloads) are hard-blocked in the automated cleaner.\n• Symbolic links and directory junctions are never traversed during scans.",
        ["settings_about_title"] = "About",
        ["settings_about_body"] = "CleanPulse v1.1 • Local .NET 9 WPF Cleaner & System Toolkit",
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
