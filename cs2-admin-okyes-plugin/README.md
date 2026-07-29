# Admin [Okyes]

Плагин админ-панели для Counter-Strike 2 с удобным меню в чате.

## Возможности

### 📋 Меню (команда `!admin`)
- **Управление игроками** — Забанить, Кикнуть (выбор игрока из списка)
- **Управление сервером** — Сменить карту (выбор из списка популярных карт)
- **Управление Таймером** — Добавить старт, Добавить финиш (для плагина Okyes - Map Timer, зона ставится в текущей позиции игрока)

## Команды

| Команда | Права | Описание | Использование |
|---------|-------|----------|---------------|
| `!admin` или `css_admin` | @css/kick, @css/ban, @css/generic, @css/root | Открыть меню Admin [Okyes] | `!admin` |
| `css_okban` | @css/ban | Забанить игрока | `css_okban <имя/userid> [минуты] [причина]` |
| `css_okkick` | @css/kick | Кикнуть игрока | `css_okkick <имя/userid> [причина]` |
| `css_okmap` | @css/root | Сменить карту | `css_okmap <название карты>` |

> Пункт «Управление Таймером» требует прав `@css/root` и установленный плагин **cs2-timer-plugin** (Okyes - Map Timer) — команды старта/финиша выполняются от его имени.

## Установка

### Требования
- Counter-Strike 2 сервер
- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) (v200+)
- **[CS2MenuManager](https://github.com/schwarper/CS2MenuManager)** — обязательная библиотека, без неё меню не откроется
- .NET 8.0 SDK (для компиляции)

### Шаги установки

1. **Установите CS2MenuManager на сервер (обязательно, до запуска плагина):**
   ```
   Скачайте архив последнего релиза:
   https://github.com/schwarper/CS2MenuManager/releases

   Распакуйте его содержимое в папку:
   csgo/addons/counterstrikesharp/

   После распаковки должна появиться папка:
   csgo/addons/counterstrikesharp/shared/CS2MenuManager/

   Перезапустите сервер.
   ```

2. **Компиляция плагина:**
   ```bash
   # Windows
   build.bat

   # Linux/Mac
   chmod +x build.sh
   ./build.sh
   ```

3. **Установка на сервер:**
   ```
   Скопируйте AdminOkyesPlugin.dll из bin/Release/net8.0/ в:
   csgo/addons/counterstrikesharp/plugins/AdminOkyesPlugin/AdminOkyesPlugin.dll
   ```

4. **Настройка администраторов:**

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

5. **Перезагрузка плагина:**
   ```
   В консоли сервера: css_plugins reload AdminOkyesPlugin
   ```

## Управление меню

После установки CS2MenuManager меню `!admin` открывается по центру экрана и управляется:
- **W / S** — перемещение по пунктам вверх/вниз
- **E** — выбрать пункт
- **A** — назад (в предыдущее меню)
- **R** — закрыть меню

## Список карт в меню смены карты

de_dust2, de_mirage, de_inferno, de_nuke, de_ancient, de_anubis, de_vertigo, de_train

Список можно изменить в файле `AdminOkyesPlugin.cs` (массив `Maps`).

## Разработка

Версия: 1.0.0
Автор: Okyes
Лицензия: MIT