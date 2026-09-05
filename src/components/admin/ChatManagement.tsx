import { useState, useEffect } from 'react';
import { Card } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Switch } from '@/components/ui/switch';
import Icon from '@/components/ui/icon';
import func2url from '@/lib/api';

interface SteamUser {
  steamId: string;
  personaName: string;
  avatarUrl: string;
}

interface ChatManagementProps {
  user: SteamUser;
}

export default function ChatManagement({ user }: ChatManagementProps) {
  const [isFrozen, setIsFrozen] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);

  useEffect(() => {
    loadChatStatus();
  }, []);

  const loadChatStatus = async () => {
    setIsLoading(true);
    try {
      const response = await fetch(func2url.chat);
      const data = await response.json();
      setIsFrozen(data.isFrozen || false);
    } catch (error) {
      console.error('Failed to load chat status:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const handleToggleFreeze = async (checked: boolean) => {
    setIsSaving(true);
    try {
      const response = await fetch(func2url.chat, {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
          'X-Admin-Steam-Id': user.steamId
        },
        body: JSON.stringify({ is_frozen: checked })
      });

      if (response.ok) {
        setIsFrozen(checked);
      }
    } catch (error) {
      console.error('Failed to toggle chat freeze:', error);
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <div className="space-y-6">
      <Card className="p-6">
        <div className="flex items-center justify-between">
          <div className="space-y-1">
            <div className="flex items-center gap-2">
              <Icon name="MessageCircle" size={20} />
              <h3 className="text-lg font-semibold">Управление чатом</h3>
            </div>
            <p className="text-sm text-muted-foreground">
              Заморозить чат для всех пользователей (кроме администраторов)
            </p>
          </div>
          <div className="flex items-center gap-3">
            {isLoading ? (
              <Icon name="Loader2" size={20} className="animate-spin" />
            ) : (
              <>
                <span className="text-sm font-medium">
                  {isFrozen ? 'Заморожен' : 'Активен'}
                </span>
                <Switch
                  checked={isFrozen}
                  onCheckedChange={handleToggleFreeze}
                  disabled={isSaving}
                />
              </>
            )}
          </div>
        </div>
      </Card>

      <Card className="p-6 bg-muted/50">
        <div className="flex gap-3">
          <Icon name="Info" size={20} className="text-primary flex-shrink-0 mt-0.5" />
          <div className="space-y-2">
            <h4 className="font-semibold">Как работает заморозка чата:</h4>
            <ul className="text-sm text-muted-foreground space-y-1">
              <li>• Обычные пользователи не смогут отправлять сообщения</li>
              <li>• Администраторы могут писать в чат даже при заморозке</li>
              <li>• Автоматическое обновление чата отключено для экономии вызовов функций</li>
              <li>• Все существующие сообщения остаются видимыми</li>
              <li>• После разморозки чат работает в обычном режиме</li>
            </ul>
          </div>
        </div>
      </Card>
    </div>
  );
}