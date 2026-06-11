import { useRef, useEffect } from 'react';
import { Card } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import Icon from '@/components/ui/icon';
import { Message, SteamUser, TournamentAdmin } from './types';

interface LobbyChatProps {
  messages: Message[];
  user: SteamUser | null;
  message: string;
  isSending: boolean;
  onMessageChange: (v: string) => void;
  onSend: () => void;
  tournamentAdmins?: TournamentAdmin[];
}

export default function LobbyChat({
  messages, user, message, isSending, onMessageChange, onSend, tournamentAdmins = [],
}: LobbyChatProps) {
  const adminIds = new Set(tournamentAdmins.map(a => a.steam_id));
  const chatEndRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    chatEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages.length]);

  return (
    <Card className="border-border bg-card/60 flex flex-col" style={{ height: 400 }}>
      <div className="px-4 py-3 border-b border-border flex items-center gap-2">
        <Icon name="MessageSquare" size={16} className="text-primary" />
        <span className="font-semibold text-sm">Чат матча</span>
      </div>

      <div className="flex-1 overflow-y-auto px-4 py-3 space-y-3">
        {messages.length === 0 ? (
          <p className="text-center text-muted-foreground text-sm mt-8">Сообщений пока нет</p>
        ) : messages.map(msg => {
          const isSystem = msg.steam_id === 'system';
          const isMine = msg.steam_id === user?.steamId;
          if (isSystem) return (
            <div key={msg.id} className="flex justify-center">
              <span className="text-xs text-muted-foreground bg-muted/40 px-3 py-1 rounded-full">{msg.message}</span>
            </div>
          );
          return (
            <div key={msg.id} className={`flex gap-2 ${isMine ? 'flex-row-reverse' : ''}`}>
              {msg.avatar_url
                ? <img src={msg.avatar_url} className="w-7 h-7 rounded-full flex-shrink-0 mt-0.5" />
                : <div className="w-7 h-7 rounded-full bg-primary/20 flex-shrink-0 mt-0.5" />
              }
              <div className={`max-w-[75%] ${isMine ? 'items-end' : 'items-start'} flex flex-col gap-0.5`}>
                <div className={`flex items-center gap-1 px-1 ${isMine ? 'flex-row-reverse' : ''}`}>
                  <span className="text-xs text-muted-foreground">{msg.persona_name}</span>
                  {adminIds.has(msg.steam_id) && (
                    <span className="flex items-center gap-0.5 text-xs font-semibold text-orange-400 bg-orange-400/10 border border-orange-400/30 px-1.5 py-0.5 rounded">
                      <Icon name="Shield" size={10} />
                      Судья
                    </span>
                  )}
                </div>
                {msg.image_url ? (
                  <div className={`rounded-xl overflow-hidden border ${isMine ? 'border-primary/40' : 'border-border'}`}>
                    <a href={msg.image_url} target="_blank" rel="noopener noreferrer">
                      <img src={msg.image_url} className="max-w-[240px] max-h-48 object-cover block" />
                    </a>
                    <div className={`px-3 py-1.5 text-xs ${isMine ? 'bg-primary text-primary-foreground' : 'bg-muted/60'}`}>
                      {msg.message}
                    </div>
                  </div>
                ) : (
                  <div className={`px-3 py-2 rounded-xl text-sm ${
                    isMine ? 'bg-primary text-primary-foreground rounded-tr-none' : 'bg-muted/60 rounded-tl-none'
                  }`}>
                    {msg.message}
                  </div>
                )}
                {msg.created_at && (
                  <span className={`text-[10px] text-muted-foreground px-1 ${isMine ? 'text-right' : ''}`}>
                    {new Date(msg.created_at).toLocaleString('ru-RU', { day: '2-digit', month: '2-digit', year: '2-digit', hour: '2-digit', minute: '2-digit' })}
                  </span>
                )}
              </div>
            </div>
          );
        })}
        <div ref={chatEndRef} />
      </div>

      <div className="px-3 py-3 border-t border-border">
        {user ? (
          <div className="flex gap-2">
            <Input
              value={message}
              onChange={e => onMessageChange(e.target.value)}
              placeholder="Написать сообщение..."
              className="flex-1 h-9 text-sm"
              onKeyDown={e => { if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); onSend(); } }}
              disabled={isSending}
            />
            <Button size="sm" className="h-9 px-3" onClick={onSend} disabled={isSending || !message.trim()}>
              <Icon name="Send" size={15} />
            </Button>
          </div>
        ) : (
          <p className="text-center text-xs text-muted-foreground">Войдите через Steam чтобы писать в чат</p>
        )}
      </div>
    </Card>
  );
}