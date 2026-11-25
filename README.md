# CleanPulse

**CleanPulse** — локальная, безопасная и лёгкая утилита очистки Windows (по типу CCleaner) без телеметрии, рекламы и фоновых служб. Написана на **C# / .NET 9 (WPF)** с поддержкой светлой/тёмной темы и переключением языка **Русский / English** прямо в интерфейсе.

> Local, lightweight, telemetry-free Windows system & temporary file cleaner built with **C# / .NET 9 (WPF)**, featuring live RU/EN localization and Dark/Light themes.

---

## Возможности / Features

- **Двухэтапная проверка (`Анализ` → `Очистка`) / Two-step `Analyze` → `Clean`:**
  Сначала безопасно оценивает объём мусора, показывает размеры по категориям и список крупнейших найденных файлов, и только затем удаляет выбранное.
- **Категории очистки / Cleanup Categories:**
  - *Система и временные файлы:* `%TEMP%`, `C:\Windows\Temp`, Корзина (`SHQueryRecycleBinW` / `SHEmptyRecycleBinW`), кэш эскизов и иконок (`thumbcache_*.db`, `iconcache_*.db`), недавние документы (опционально), кэш шейдеров DirectX (опционально).
  - *Кэш Windows и сеть:* кэш обновлений Windows (`SoftwareDistribution\Download`), кэш оптимизации доставки (`Delivery Optimization`), `Prefetch` (опционально), сброс DNS-кэша (`DnsFlushResolverCache`).
  - *Логи, отчёты и дампы:* отчёты об ошибках Windows (WER), дампы сбоев `.dmp` (опционально), журналы CBS / DISM / Panther / setupapi / WindowsUpdate.
- **Встроенные гарантии безопасности / Built-in Safety Guards:**
  - Жёсткая блокировка системных (`Windows`, `System32`, `WinSxS`, `Program Files`) и личных директорий (`Рабочий стол`, `Документы`, `Загрузки`, `Изображения` и т.д.).
  - Защита от перехода по символическим ссылкам и точкам соединения (`ReparsePoint`).
  - Фильтр минимального возраста временных файлов (по умолчанию 24 часа, настраивается: 0 ч / 1 ч / 24 ч / 7 дней).
  - Аккуратный пропуск занятых системой файлов без падений.
  - Гибкие пользовательские **Исключения** (пути, папки, маски вроде `*.dmp`) и кнопка **«Запустить как админ»** прямо из интерфейса.
- **Журнал операций / History & Export:**
  Локальный лог всех анализов и очисток, счётчик освобождённого места и экспорт отчёта в `.txt`.
- **100% локально / 100% Local:**
  Все настройки (`settings.json`) и история (`history.json`) хранятся локально в `%LOCALAPPDATA%\CleanPulse`.

---

## Структура проекта / Project Structure

```text
CleanPulse/
├── CleanPulse.sln
├── src/
│   ├── CleanPulse.Core/        # Движок сканирования, SafetyGuard, Win32 адаптеры, локализация
│   └── CleanPulse/             # WPF-приложение (MVVM, Dark/Light темы, иконка, UI)
├── tests/
│   └── CleanPulse.Tests/       # Автотесты безопасности, фильтрации, очистки и UI smoke-тест
├── scripts/
│   └── publish.ps1             # Скрипт сборки CleanPulse.exe в ./dist
└── .github/workflows/ci.yml    # GitHub Actions CI (build, test, artifact)
```

---

## Сборка и запуск / Build & Run

### Требования / Prerequisites
- Windows 10 / Windows 11 (x64)
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

### Запуск из исходников / Run from source
```powershell
dotnet build CleanPulse.sln -c Release
dotnet run --project src/CleanPulse/CleanPulse.csproj -c Release
```

### Запуск тестов / Run automated tests
```powershell
dotnet run --project tests/CleanPulse.Tests/CleanPulse.Tests.csproj -c Release
```

### Сборка локального `.exe` / Publish `.exe`
```powershell
.\scripts\publish.ps1
# Однофайловый exe (использует установленный .NET 9 Desktop Runtime):
.\scripts\publish.ps1 -SingleFile
# Или полностью автономный exe (без зависимости от установленного .NET Desktop Runtime):
.\scripts\publish.ps1 -SelfContained
```
Готовый файл появится в `dist\CleanPulse.exe`.

---

## Лицензия / License

[MIT](LICENSE)
