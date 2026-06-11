import { useState, useEffect, useRef } from 'react';
import { Card } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import Icon from '@/components/ui/icon';
import PlayerLink from '@/components/ui/player-link';
import { formatChatDateTime } from '@/utils/dateFormat';
import func2url from '../../backend/func2url.json';
import BanDialog, { BanType } from '@/components/ui/ban-dialog';

interface ReplyTo {
  id: number;
  personaName: string;
  message: string;
}

interface ChatMessage {
  id: number;
  steamId: string;
  personaName: string;
  avatarUrl?: string;
  message: string;
  createdAt: string;
  replyTo?: ReplyTo | null;
  isAdmin?: boolean;
  isModerator?: boolean;
  isOnline?: boolean;
}

interface SteamUser {
  steamId: string;
  personaName: string;
  avatarUrl: string;
  profileUrl: string;
  nickname?: string;
}

interface GlobalChatProps {
  user: SteamUser | null;
  onLoginClick: () => void;
}

export default function GlobalChat({ user, onLoginClick }: GlobalChatProps) {
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [newMessage, setNewMessage] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [isSending, setIsSending] = useState(false);
  const [isAdmin, setIsAdmin] = useState(false);
  const [isFrozen, setIsFrozen] = useState(false);
  const [replyingTo, setReplyingTo] = useState<ChatMessage | null>(null);
  const [userNickname, setUserNickname] = useState<string | null>(null);
  const [banDialog, setBanDialog] = useState<{ open: boolean; messageId: number | null }>({ open: false, messageId: null });
  const [isBanning, setIsBanning] = useState(false);
  const messagesEndRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    loadMessages();
  }, []);

  useEffect(() => {
    if (user) {
      checkAdmin();
      loadUserNickname();
    }
  }, [user]);

  useEffect(() => {
    scrollToBottom();
  }, [messages]);

  const loadMessages = async () => {
    try {
      const response = await fetch(`${func2url.chat}?limit=50`);
      const data = await response.json();
      setMessages(data.messages || []);
      setIsFrozen(data.isFrozen || false);
    } catch (error) {
      console.error('Failed to load messages:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  };

  const loadUserNickname = async () => {
    if (!user?.steamId) return;
    try {
      const response = await fetch(
        `https://functions.poehali.dev/88f7bd27-aac7-4eab-b045-2d423b092ebb?steam_id=${user.steamId}`
      );
      const data = await response.json();
      setUserNickname(data.user.nickname || user.personaName);
    } catch (error) {
      setUserNickname(user.personaName);
    }
  };

  const handleSendMessage = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!user) {
      onLoginClick();
      return;
    }

    if (!newMessage.trim() || newMessage.length > 500) {
      return;
    }

    setIsSending(true);

    try {
      const response = await fetch(func2url.chat, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          steam_id: user.steamId,
          persona_name: userNickname || user.personaName,
          avatar_url: user.avatarUrl,
          message: newMessage.trim(),
          reply_to_message_id: replyingTo?.id || null
        })
      });

      if (response.ok) {
        setNewMessage('');
        setReplyingTo(null);
        await loadMessages();
      }
    } catch (error) {
      console.error('Failed to send message:', error);
    } finally {
      setIsSending(false);
    }
  };

  const checkAdmin = async () => {
    if (!user) return;
    try {
      const response = await fetch(`${func2url.users}?action=check-admin&steam_id=${user.steamId}`);
      const data = await response.json();
      setIsAdmin(data.isAdmin || false);
    } catch (error) {
      console.error('Failed to check admin status:', error);
    }
  };

  const handleSelfDelete = async (messageId: number) => {
    if (!user) return;
    try {
      await fetch(func2url.chat, {
        method: 'DELETE',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ message_id: messageId, ban_type: 'delete_only', steam_id: user.steamId }),
      });
      await loadMessages();
    } catch (error) {
      console.error('Failed to delete message:', error);
    }
  };

  const handleDeleteMessage = (messageId: number) => {
    setBanDialog({ open: true, messageId });
  };

  const handleBanConfirm = async (banType: BanType, reason: string) => {
    if (!user || !banDialog.messageId) return;
    setIsBanning(true);
    try {
      await fetch(func2url.chat, {
        method: 'DELETE',
        headers: { 'Content-Type': 'application/json', 'X-Admin-Steam-Id': user.steamId },
        body: JSON.stringify({ message_id: banDialog.messageId, ban_type: banType, reason, banned_by_name: userNickname || user.personaName }),
      });
      setBanDialog({ open: false, messageId: null });
      await loadMessages();
    } catch (error) {
      console.error('Failed to delete message:', error);
    } finally {
      setIsBanning(false);
    }
  };



  return (
    <>
    <Card className="w-full flex flex-col bg-card/95 backdrop-blur">
      {/* Header */}
      <div className="flex items-center gap-2 p-4 border-b border-border">
        <Icon name="MessageCircle" size={20} className="text-primary" />
        <h3 className="font-semibold">Общий чат</h3>
        <span className="text-xs text-muted-foreground">
          ({messages.length})
        </span>
        {isFrozen && (
          <span className="ml-auto flex items-center gap-1 px-2 py-1 bg-muted text-xs text-muted-foreground rounded">
            <Icon name="Lock" size={12} />
            Заморожен
          </span>
        )}
      </div>

      {/* Messages */}
      <div className="h-[400px] overflow-y-auto p-4 space-y-3 [&::-webkit-scrollbar]:w-2 [&::-webkit-scrollbar-track]:bg-transparent [&::-webkit-scrollbar-thumb]:bg-muted [&::-webkit-scrollbar-thumb]:rounded-full [&::-webkit-scrollbar-thumb]:hover:bg-muted-foreground/50">
        {isLoading ? (
          <div className="flex items-center justify-center h-full">
            <Icon name="Loader2" size={24} className="animate-spin text-muted-foreground" />
          </div>
        ) : messages.length === 0 ? (
          <div className="flex flex-col items-center justify-center h-full text-muted-foreground">
            <Icon name="MessageSquare" size={48} className="mb-2 opacity-50" />
            <p className="text-sm">Сообщений пока нет</p>
            <p className="text-xs">Будьте первым!</p>
          </div>
        ) : (
          messages.map((msg) => (
            <div key={msg.id} className="flex gap-2 animate-in fade-in slide-in-from-bottom-2 group">
              <PlayerLink steamId={msg.steamId} name={msg.personaName} avatarOnly avatarUrl={msg.avatarUrl} avatarSize={8} isOnline={msg.isOnline} className="flex-shrink-0" />
              <div className="flex-1 min-w-0">
                <div className="flex items-baseline gap-2 flex-wrap">
                  <PlayerLink steamId={msg.steamId} name={msg.personaName} className="text-sm" />
                  {msg.isAdmin && (
                    <span className="px-1.5 py-0.5 bg-destructive/20 text-destructive text-[10px] font-semibold rounded">
                      Администратор
                    </span>
                  )}
                  {msg.isModerator && (
                    <span className="px-1.5 py-0.5 bg-primary/20 text-primary text-[10px] font-semibold rounded">
                      Модератор
                    </span>
                  )}
                  <span className="text-xs text-muted-foreground">
                    {formatChatDateTime(msg.createdAt)}
                  </span>
                </div>
                {msg.replyTo && (
                  <div className="mb-1 pl-3 border-l-2 border-primary/50 text-xs text-muted-foreground">
                    <span className="font-semibold">{msg.replyTo.personaName}:</span> {msg.replyTo.message.substring(0, 50)}{msg.replyTo.message.length > 50 ? '...' : ''}
                  </div>
                )}
                <p className="text-sm break-words">{msg.message}</p>
              </div>
              <div className="flex gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                {user && (
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => setReplyingTo(msg)}
                    className="h-6 w-6 p-0 text-muted-foreground hover:text-primary"
                  >
                    <Icon name="Reply" size={14} />
                  </Button>
                )}
                {user && msg.steamId === user.steamId && (
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => handleSelfDelete(msg.id)}
                    className="h-6 w-6 p-0 text-muted-foreground hover:text-destructive"
                  >
                    <Icon name="Trash2" size={14} />
                  </Button>
                )}
                {isAdmin && msg.steamId !== user?.steamId && (
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => handleDeleteMessage(msg.id)}
                    className="h-6 w-6 p-0 text-destructive hover:text-destructive"
                  >
                    <Icon name="Trash2" size={14} />
                  </Button>
                )}
              </div>
            </div>
          ))
        )}
        <div ref={messagesEndRef} />
      </div>

      {/* Input */}
      <form onSubmit={handleSendMessage} className="p-4 border-t border-border">
        {replyingTo && (
          <div className="mb-2 p-2 bg-secondary/50 rounded-lg flex items-start justify-between gap-2">
            <div className="flex-1 min-w-0">
              <div className="text-xs text-muted-foreground mb-1">
                Ответ на сообщение <span className="font-semibold">{replyingTo.personaName}</span>
              </div>
              <p className="text-xs truncate">{replyingTo.message}</p>
            </div>
            <Button
              type="button"
              variant="ghost"
              size="sm"
              onClick={() => setReplyingTo(null)}
              className="h-6 w-6 p-0 flex-shrink-0"
            >
              <Icon name="X" size={14} />
            </Button>
          </div>
        )}
        {!user ? (
          <Button
            type="button"
            onClick={onLoginClick}
            className="w-full"
            variant="outline"
          >
            <Icon name="User" size={16} className="mr-2" />
            Войти для отправки
          </Button>
        ) : isFrozen && !isAdmin ? (
          <div className="w-full p-3 bg-muted/50 rounded-lg border border-border flex items-center gap-2">
            <Icon name="Lock" size={16} className="text-muted-foreground" />
            <p className="text-sm text-muted-foreground">
              Чат временно заморожен администратором
            </p>
          </div>
        ) : (
          <div className="flex gap-2">
            <Input
              value={newMessage}
              onChange={(e) => setNewMessage(e.target.value)}
              placeholder="Введите сообщение..."
              maxLength={500}
              disabled={isSending}
              className="flex-1"
            />
            <Button
              type="submit"
              disabled={isSending || !newMessage.trim()}
              size="icon"
            >
              {isSending ? (
                <Icon name="Loader2" size={18} className="animate-spin" />
              ) : (
                <Icon name="Send" size={18} />
              )}
            </Button>
          </div>
        )}
        {user && (
          <p className="text-xs text-muted-foreground mt-2">
            {newMessage.length}/500
          </p>
        )}
      </form>
    </Card>

    <BanDialog
      open={banDialog.open}
      onClose={() => setBanDialog({ open: false, messageId: null })}
      onConfirm={handleBanConfirm}
      isLoading={isBanning}
    />
    </>
  );
}