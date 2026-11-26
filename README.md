# CleanPulse

**CleanPulse v1.2** — локальный, безопасный и лёгкий комбайн для очистки и обслуживания Windows (по типу CCleaner) без телеметрии, рекламы и фоновых служб. Построен на **C# / .NET 9 (WPF)** в стиле **Liquid Glass (Жидкое стекло)** без неонового свечения с темами **Светлое стекло / Дымчатое стекло** и мгновенным переключением **Русский / English**.

> Local, telemetry-free Windows cleaner and system maintenance toolkit built with **C# / .NET 9 (WPF)**, featuring an Apple-inspired silver-white **Liquid Glass** UI (zero neon), live RU/EN localization, and CLI automation.

---

## Возможности / Features

### 1. Очистка системы, браузеров и кэшей / System, Browser & Dev Cleaner
- Двухэтапный режим **Анализ → Очистка** с фильтрацией, сортировкой и отправкой отдельных файлов в Корзину прямо из инспектора.
- **Система и временные файлы:** `%TEMP%`, `C:\Windows\Temp`, Корзина (`SHQueryRecycleBinW` / `SHEmptyRecycleBinW`), кэш эскизов и иконок (`thumbcache_*.db`, `iconcache_*.db`), недавние документы, DirectX `D3DSCache`, кэш видеодрайверов **NVIDIA / AMD / Intel**.
- **Кэш Windows и сеть:** `SoftwareDistribution\Download`, Delivery Optimization, `Prefetch`, обнаружение `C:\Windows.old`, сброс DNS-кэша (`DnsFlushResolverCache`), а также кнопка запуска встроенной очистки компонентов **WinSxS через DISM** (`/StartComponentCleanup`).
- **Браузеры (только кэш):** Chrome, Edge, Яндекс Браузер, Firefox, Opera / Opera GX — удаляется только `Cache / Code Cache / GPUCache / cache2` всех профилей с определением запущенного процесса. Пароли, cookies, история и вкладки **не затрагиваются**.
- **Инструменты разработки:** HTTP-кэш NuGet (`v3-cache`, `plugins-cache`), `pip\Cache`, `npm-cache`, кэш и логи VS Code.
- **Логи и дампы:** отчёты об ошибках WER, дампы `.dmp` (`CrashDumps`, `Minidump`), системные журналы CBS / DISM / Panther / setupapi / WindowsUpdate.

### 2. Анализатор места на диске / Disk Space Analyzer
- Однопроходное сканирование выбранного диска или папки с защитой от зацикливания по junctions/symlinks.
- Рейтинг крупнейших папок (с возможностью «провалиться» глубже) и **Топ-150 самых больших файлов** с фильтрами (`Архивы`, `Образы .iso`, `Установщики`, `Медиа`, `Временные/Дампы`) и быстрым перемещением в Корзину.

### 3. Поиск дубликатов / Duplicate File Finder
- Трёхэтапное точное сравнение (**размер → 4 КБ заголовок → полный SHA-256**) в выбранной папке.
- Быстрое выделение копий (*оставить старые* или *оставить новые*) с безопасным удалением в Корзину.

### 4. Менеджер автозагрузки / Startup Manager
- Просмотр программ из `HKCU\...\Run`, `HKLM\...\Run` и папок «Автозагрузка».
- Безопасное включение/отключение через официальный механизм Windows **`StartupApproved`** (совместимо с Диспетчером задач Windows — без удаления записей).

### 5. Пользовательские правила, исключения и автоматизация / Custom Rules, CLI & Scheduler
- **Свои папки для очистки** с маской файлов и выбором «В Корзину» / «Навсегда» (с жёсткой проверкой `SafetyGuard`).
- **Исключения** по путям, папкам или маскам (`*.dmp`, `D:\Work\Temp`).
- **CLI-режим и Планировщик Windows:**
  ```powershell
  CleanPulse.exe --analyze
  CleanPulse.exe --clean-safe --silent
  CleanPulse.exe --clean-selected
  ```
  В настройках можно в один клик создать или удалить задачу в Планировщике Windows (`CleanPulseAutoClean`).

---

## Встроенные гарантии безопасности / Safety Guarantees

- Системные пути (`Windows`, `System32`, `WinSxS`, `Program Files`) защищены на уровне кода `SafetyGuard`.
- Личные библиотеки (`Рабочий стол`, `Документы`, `Загрузки`, `Изображения`, `Музыка`, `Видео`) заблокированы для автоматической очистки и пользовательских правил.
- Символические ссылки и точки соединения (`ReparsePoint`) никогда не обходятся.
- Временные файлы очищаются только если они старше выбранного порога возраста (по умолчанию 24 часа).
- Все настройки и история хранятся 100% локально в `%LOCALAPPDATA%\CleanPulse`.

---

## Сборка, тесты и релиз / Build, Test & Publish

### Требования / Prerequisites
- Windows 10 / Windows 11 (x64)
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

### Сборка и запуск тестов
```powershell
dotnet build CleanPulse.sln -c Release
dotnet run --project tests/CleanPulse.Tests/CleanPulse.Tests.csproj -c Release --no-build
```

### Сборка локального `.exe`
```powershell
.\scripts\publish.ps1
# Однофайловый exe (использует установленный .NET 9 Desktop Runtime):
.\scripts\publish.ps1 -SingleFile
# Или полностью автономный exe (без зависимости от .NET Desktop Runtime):
.\scripts\publish.ps1 -SelfContained
```

### GitHub Actions & Releases
- [ci.yml](.github/workflows/ci.yml) автоматически собирает и тестирует каждый push в `main`.
- [release.yml](.github/workflows/release.yml) при пуше тега `v*` (например `v1.1.0`) автоматически собирает `CleanPulse-win-x64.exe` и `CleanPulse-win-x64-standalone.exe` и создаёт GitHub Release.

---

## Лицензия / License

[MIT](LICENSE)
