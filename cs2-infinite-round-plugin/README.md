# Infinite Round

Плагин для Counter-Strike 2, убирающий время раунда — раунд становится бесконечным (не завершается по таймеру).

## Возможности

- Отключает лимит времени раунда (`mp_roundtime` и связанные cvar выставляются в максимум)
- Раунд не завершается по игровым условиям (взрыв бомбы, обезвреживание, полное уничтожение команды) благодаря `mp_ignore_round_win_conditions 1`
- Автоматически применяется при старте каждого раунда
- Команда `css_okinfinite`-переключатель, чтобы включать/выключать функцию на лету

## Команды

| Команда | Права | Описание |
|---------|-------|----------|
| `css_infinite` | @css/root | Включить/выключить бесконечный раунд |

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
   Скопируйте InfiniteRoundPlugin.dll из bin/Release/net8.0/ в:
   csgo/addons/counterstrikesharp/plugins/InfiniteRoundPlugin/InfiniteRoundPlugin.dll
   ```

3. **Перезагрузка плагина:**
   ```
   В консоли сервера: css_plugins reload InfiniteRoundPlugin
   ```

## Примечание

Раунд физически всё равно можно завершить командой `mp_restartgame` или сменой карты — плагин лишь убирает автоматическое завершение по таймеру и по условиям победы.

## Разработка

Версия: 1.0.0
Автор: Okyes
Лицензия: MIT
