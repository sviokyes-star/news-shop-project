// Часовой пояс, в котором показываем время пользователю.
const TZ = 'Europe/Moscow';

// Бэкенд отдаёт время в UTC, но часто без суффикса "Z" (naive ISO,
// например "2026-07-26T13:24:00"). Такую строку браузер ошибочно
// считает локальной. Здесь добавляем "Z", чтобы она читалась как UTC.
export const parseDate = (timestamp: string): Date => {
  if (typeof timestamp === 'string') {
    const hasTz = /[zZ]|[+-]\d{2}:?\d{2}$/.test(timestamp.trim());
    if (!hasTz) return new Date(timestamp.trim() + 'Z');
  }
  return new Date(timestamp);
};

export const formatTime = (timestamp: string): string => {
  const date = parseDate(timestamp);
  return date.toLocaleTimeString('ru-RU', { 
    hour: '2-digit', 
    minute: '2-digit',
    timeZone: TZ
  });
};

export const formatDate = (timestamp: string): string => {
  const date = parseDate(timestamp);
  return date.toLocaleDateString('ru-RU', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
    timeZone: TZ
  });
};

export const formatDateTime = (timestamp: string): string => {
  const date = parseDate(timestamp);
  return date.toLocaleString('ru-RU', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    timeZone: TZ
  });
};

export const formatShortDate = (timestamp: string): string => {
  const date = parseDate(timestamp);
  return date.toLocaleDateString('ru-RU', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    timeZone: TZ
  });
};

export const formatShortDateTime = (timestamp: string): string => {
  const date = parseDate(timestamp);
  return date.toLocaleString('ru-RU', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    timeZone: TZ,
  });
};

export const formatRelativeTime = (timestamp: string): string => {
  const date = parseDate(timestamp);
  const now = new Date();
  const diffMs = now.getTime() - date.getTime();
  const diffMins = Math.floor(diffMs / 60000);
  const diffHours = Math.floor(diffMs / 3600000);
  const diffDays = Math.floor(diffMs / 86400000);

  if (diffMins < 1) return 'только что';
  if (diffMins < 60) return `${diffMins} мин. назад`;
  if (diffHours < 24) return `${diffHours} ч. назад`;
  if (diffDays < 7) return `${diffDays} дн. назад`;
  
  return formatShortDate(timestamp);
};

export const formatChatDateTime = (timestamp: string): string => {
  const date = parseDate(timestamp);

  const time = date.toLocaleTimeString('ru-RU', {
    hour: '2-digit',
    minute: '2-digit',
    timeZone: TZ,
  });

  // Календарный день в московской зоне (через en-CA получаем YYYY-MM-DD).
  const dayKey = (d: Date) =>
    d.toLocaleDateString('en-CA', { timeZone: TZ });

  const now = new Date();
  const yesterday = new Date(now.getTime() - 86400000);

  const messageDay = dayKey(date);
  if (messageDay === dayKey(now)) return time;
  if (messageDay === dayKey(yesterday)) return `вчера ${time}`;

  return date.toLocaleDateString('ru-RU', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    timeZone: TZ,
  }) + ` ${time}`;
};

export const toLocalDateTimeInput = (timestamp: string): string => {
  const date = parseDate(timestamp);
  // Раскладываем на компоненты в московской зоне для input datetime-local.
  const parts = date.toLocaleString('en-CA', {
    timeZone: TZ,
    year: 'numeric', month: '2-digit', day: '2-digit',
    hour: '2-digit', minute: '2-digit', hour12: false,
  });
  const [datePart, timePart] = parts.split(', ');
  return `${datePart}T${timePart}`;
};

export const toUTCISOString = (localDateTimeInput: string): string => {
  // Значение из <input datetime-local> трактуем как московское время (UTC+3,
  // фиксированный сдвиг без перехода на летнее время), переводим в UTC.
  const m = localDateTimeInput.match(
    /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})/
  );
  if (m) {
    const [, y, mo, d, h, mi] = m;
    const utcMs = Date.UTC(+y, +mo - 1, +d, +h - 3, +mi);
    return new Date(utcMs).toISOString();
  }
  return new Date(localDateTimeInput).toISOString();
};