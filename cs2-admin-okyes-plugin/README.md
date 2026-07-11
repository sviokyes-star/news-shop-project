# Admin [Okyes]

Плагин админ-панели для Counter-Strike 2 с удобным меню в чате.

## Возможности

### 📋 Меню (команда `!admin`)
- **Управление игроками** — Забанить, Кикнуть (выбор игрока из списка)
- **Управление сервером** — Сменить карту (выбор из списка популярных карт)

## Команды

| Команда | Права | Описание | Использование |
|---------|-------|----------|---------------|
| `!admin` или `css_admin` | @css/kick, @css/ban, @css/generic, @css/root | Открыть меню Admin [Okyes] | `!admin` |
| `css_okban` | @css/ban | Забанить игрока | `css_okban <имя/userid> [минуты] [причина]` |
| `css_okkick` | @css/kick | Кикнуть игрока | `css_okkick <имя/userid> [причина]` |
| `css_okmap` | @css/root | Сменить карту | `css_okmap <название карты>` |

## Установка

### Требования
- Counter-Strike 2 сервер
- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) (v200+)
- .NET 8.0 SDK (для компиляции)

### Шаги установки

1. **Компиляция плагина:**
   ```bash
   # Windows
   build.bat

   # Linux/Mac
   chmod +x build.sh
   ./build.sh
   ```

2. **Установка на сервер:**
   ```
   Скопируйте AdminOkyesPlugin.dll из bin/Release/net8.0/ в:
   csgo/addons/counterstrikesharp/plugins/AdminOkyesPlugin/AdminOkyesPlugin.dll
   ```

3. **Настройка администраторов:**

   Отредактируйте файл `csgo/addons/counterstrikesharp/configs/admins.json`:

   ```json
   {
     "admins": [
       {
         "identity": "76561198012345678",
         "flags": ["@css/kick", "@css/ban", "@css/generic"]
       }
     ]
   }
   ```

4. **Перезагрузка плагина:**
   ```
   В консоли сервера: css_plugins reload AdminOkyesPlugin
   ```

## Список карт в меню смены карты

de_dust2, de_mirage, de_inferno, de_nuke, de_ancient, de_anubis, de_vertigo, de_train

Список можно изменить в файле `AdminOkyesPlugin.cs` (массив `Maps`).

## Разработка

Версия: 1.0.0
Автор: Okyes
Лицензия: MIT
