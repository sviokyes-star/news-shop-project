import { useState, useEffect, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import Icon from '@/components/ui/icon';
import func2url from '../../backend/func2url.json';

interface Notification {
  id: number;
  type: string;
  title: string;
  body: string;
  link: string;
  isRead: boolean;
  createdAt: string;
}

interface NotificationBellProps {
  steamId: string;
}

const TYPE_ICON: Record<string, string> = {
  friend_request: 'UserPlus',
  tournament_registered: 'Trophy',
  tournament_24h: 'Clock',
  tournament_1h: 'Zap',
};

const TYPE_COLOR: Record<string, string> = {
  friend_request: 'text-primary',
  tournament_registered: 'text-green-500',
  tournament_24h: 'text-yellow-500',
  tournament_1h: 'text-destructive',
};

export default function NotificationBell({ steamId }: NotificationBellProps) {
  const navigate = useNavigate();
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const [unread, setUnread] = useState(0);
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    loadNotifications();
    const interval = setInterval(loadNotifications, 60_000);
    return () => clearInterval(interval);
  }, [steamId]);

  useEffect(() => {
    const handleClick = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false);
    };
    document.addEventListener('mousedown', handleClick);
    return () => document.removeEventListener('mousedown', handleClick);
  }, []);

  const loadNotifications = async () => {
    try {
      const res = await fetch(`${func2url.notifications}?steam_id=${steamId}`);
      const data = await res.json();
      setNotifications(data.notifications || []);
      setUnread(data.unread || 0);
    } catch (e) {
      console.error('Failed to load notifications', e);
    }
  };

  const markAllRead = async () => {
    try {
      await fetch(func2url.notifications, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ steam_id: steamId }),
      });
      setUnread(0);
      setNotifications(prev => prev.map(n => ({ ...n, isRead: true })));
    } catch (e) {
      console.error('Failed to mark notifications', e);
    }
  };

  const handleOpen = () => {
    setOpen(v => !v);
    if (!open && unread > 0) markAllRead();
  };

  const handleClick = (n: Notification) => {
    setOpen(false);
    if (n.link) navigate(n.link);
  };

  const formatTime = (iso: string) => {
    const d = new Date(iso);
    const now = new Date();
    const diff = Math.floor((now.getTime() - d.getTime()) / 60000);
    if (diff < 1) return 'только что';
    if (diff < 60) return `${diff} мин. назад`;
    if (diff < 1440) return `${Math.floor(diff / 60)} ч. назад`;
    return d.toLocaleDateString('ru-RU', { day: '2-digit', month: '2-digit' });
  };

  return (
    <div ref={ref} className="relative">
      <button
        onClick={handleOpen}
        className="relative p-2 rounded-lg hover:bg-secondary transition-colors"
        aria-label="Уведомления"
      >
        <Icon name="Bell" size={20} className={unread > 0 ? 'text-primary' : 'text-muted-foreground'} />
        {unread > 0 && (
          <span className="absolute -top-0.5 -right-0.5 min-w-[18px] h-[18px] px-1 bg-destructive text-destructive-foreground text-[10px] font-bold rounded-full flex items-center justify-center">
            {unread > 99 ? '99+' : unread}
          </span>
        )}
      </button>

      {open && (
        <div className="absolute right-0 top-full mt-2 w-80 bg-card border border-border rounded-xl shadow-2xl shadow-black/30 z-50 overflow-hidden">
          <div className="flex items-center justify-between px-4 py-3 border-b border-border">
            <span className="font-semibold text-sm">Уведомления</span>
            {notifications.some(n => !n.isRead) && (
              <button onClick={markAllRead} className="text-xs text-primary hover:underline">
                Прочитать все
              </button>
            )}
          </div>

          <div className="max-h-80 overflow-y-auto">
            {notifications.length === 0 ? (
              <div className="py-10 text-center text-muted-foreground text-sm">
                <Icon name="BellOff" size={32} className="mx-auto mb-2 opacity-30" />
                Нет уведомлений
              </div>
            ) : (
              notifications.map(n => (
                <button
                  key={n.id}
                  onClick={() => handleClick(n)}
                  className={`w-full text-left flex items-start gap-3 px-4 py-3 hover:bg-secondary/50 transition-colors border-b border-border/50 last:border-0 ${!n.isRead ? 'bg-primary/5' : ''}`}
                >
                  <div className={`mt-0.5 flex-shrink-0 ${TYPE_COLOR[n.type] || 'text-muted-foreground'}`}>
                    <Icon name={TYPE_ICON[n.type] || 'Bell'} size={16} />
                  </div>
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center justify-between gap-2">
                      <p className="text-sm font-medium truncate">{n.title}</p>
                      {!n.isRead && <span className="w-2 h-2 rounded-full bg-primary flex-shrink-0" />}
                    </div>
                    {n.body && <p className="text-xs text-muted-foreground mt-0.5 line-clamp-2">{n.body}</p>}
                    <p className="text-xs text-muted-foreground/60 mt-1">{formatTime(n.createdAt)}</p>
                  </div>
                </button>
              ))
            )}
          </div>
        </div>
      )}
    </div>
  );
}