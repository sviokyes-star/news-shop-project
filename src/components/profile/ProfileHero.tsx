import { useState } from 'react';
import { Card } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import Icon from '@/components/ui/icon';
import { formatRelativeTime } from '@/utils/dateFormat';
import func2url from '../../../backend/func2url.json';
import { toast } from '@/hooks/use-toast';

interface SteamUser {
  steamId: string;
  personaName: string;
  avatarUrl: string;
  profileUrl: string;
  nickname?: string;
}

interface ProfileUserData {
  balance: number;
  isBlocked: boolean;
  blockReason?: string;
  nickname?: string;
  isOnline?: boolean;
  lastOnline?: string;
}

interface ProfileStatistics {
  tournaments_count: number;
  purchases_count: number;
  total_spent: number;
}

interface ProfileHeroProps {
  user: SteamUser;
  profileUser: ProfileUserData;
  statistics: ProfileStatistics;
  onUserUpdate: (user: SteamUser) => void;
  onProfileReload: () => void;
}

export default function ProfileHero({ user, profileUser, statistics, onUserUpdate, onProfileReload }: ProfileHeroProps) {
  const [isEditingNickname, setIsEditingNickname] = useState(false);
  const [newNickname, setNewNickname] = useState(profileUser.nickname || user.personaName);
  const [nicknameError, setNicknameError] = useState('');
  const [isUploadingAvatar, setIsUploadingAvatar] = useState(false);

  const handleUpdateNickname = async () => {
    if (!user?.steamId) return;

    if (newNickname.trim().length < 3 || newNickname.trim().length > 30) {
      setNicknameError('Никнейм должен быть от 3 до 30 символов');
      return;
    }

    try {
      const response = await fetch(func2url['update-profile'], {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ action: 'nickname', steam_id: user.steamId, nickname: newNickname.trim() }),
      });
      const data = await response.json();
      if (data.success) {
        setIsEditingNickname(false);
        setNicknameError('');
        onProfileReload();
      } else {
        setNicknameError(data.error || 'Ошибка при обновлении никнейма');
      }
    } catch {
      setNicknameError('Ошибка при обновлении никнейма');
    }
  };

  const handleAvatarUpload = async (event: React.ChangeEvent<HTMLInputElement>) => {
    if (!user?.steamId) return;
    const file = event.target.files?.[0];
    if (!file) return;

    if (file.size > 5 * 1024 * 1024) {
      toast({ title: 'Ошибка', description: 'Размер файла не должен превышать 5MB', variant: 'destructive' });
      return;
    }
    if (!file.type.startsWith('image/')) {
      toast({ title: 'Ошибка', description: 'Можно загружать только изображения', variant: 'destructive' });
      return;
    }

    setIsUploadingAvatar(true);
    try {
      const reader = new FileReader();
      reader.onloadend = async () => {
        const base64String = reader.result as string;
        const response = await fetch(func2url['update-profile'], {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ action: 'avatar', steam_id: user.steamId, image: base64String }),
        });
        const data = await response.json();
        if (data.success) {
          const updated = { ...user, avatarUrl: data.avatar_url };
          onUserUpdate(updated);
          localStorage.setItem('steamUser', JSON.stringify(updated));
          onProfileReload();
          toast({ title: 'Успешно!', description: 'Аватар обновлён' });
        } else {
          toast({ title: 'Ошибка', description: data.error || 'Ошибка при загрузке аватара', variant: 'destructive' });
        }
        setIsUploadingAvatar(false);
      };
      reader.readAsDataURL(file);
    } catch {
      toast({ title: 'Ошибка', description: 'Ошибка при загрузке аватара', variant: 'destructive' });
      setIsUploadingAvatar(false);
    }
  };

  return (
    <Card className="p-10 border border-border bg-gradient-to-br from-primary/5 to-primary/10 backdrop-blur border-primary/20">
      <div className="flex items-start gap-8">
        <div className="relative group">
          <img
            src={user.avatarUrl}
            alt={user.personaName}
            className="w-32 h-32 rounded-2xl border-4 border-primary shadow-2xl shadow-primary/20"
          />
          <label
            htmlFor="avatar-upload"
            className="absolute inset-0 bg-black/60 rounded-2xl flex items-center justify-center opacity-0 group-hover:opacity-100 transition-opacity cursor-pointer"
          >
            {isUploadingAvatar ? (
              <Icon name="Loader2" size={32} className="text-white animate-spin" />
            ) : (
              <div className="text-center">
                <Icon name="Camera" size={32} className="text-white mx-auto mb-1" />
                <p className="text-xs text-white font-medium">Сменить</p>
              </div>
            )}
          </label>
          <input
            id="avatar-upload"
            type="file"
            accept="image/*"
            onChange={handleAvatarUpload}
            className="hidden"
            disabled={isUploadingAvatar}
          />
        </div>

        <div className="flex-1 space-y-4">
          <div>
            {isEditingNickname ? (
              <div className="space-y-3">
                <div className="flex items-center gap-3">
                  <Input
                    value={newNickname}
                    onChange={(e) => setNewNickname(e.target.value)}
                    placeholder="Введите никнейм"
                    className="text-2xl font-bold h-12"
                    maxLength={30}
                  />
                  <Button onClick={handleUpdateNickname} size="sm" className="gap-2">
                    <Icon name="Check" size={16} />
                    Сохранить
                  </Button>
                  <Button
                    onClick={() => { setIsEditingNickname(false); setNicknameError(''); setNewNickname(profileUser.nickname || user.personaName); }}
                    variant="outline"
                    size="sm"
                  >
                    <Icon name="X" size={16} />
                  </Button>
                </div>
                {nicknameError && <p className="text-sm text-destructive">{nicknameError}</p>}
              </div>
            ) : (
              <div className="flex items-center gap-3">
                <h1 className="text-4xl font-bold tracking-tight">{profileUser.nickname || user.personaName}</h1>
                <Button onClick={() => setIsEditingNickname(true)} variant="ghost" size="sm" className="gap-2">
                  <Icon name="Pencil" size={16} />
                </Button>
              </div>
            )}
            <div className="flex items-center gap-2 mt-1">
              <span className={`w-2.5 h-2.5 rounded-full ${profileUser.isOnline ? 'bg-green-500' : 'bg-muted-foreground'}`} />
              <span className="text-sm text-muted-foreground">
                {profileUser.isOnline
                  ? 'Онлайн'
                  : profileUser.lastOnline
                    ? `Был(а) онлайн ${formatRelativeTime(profileUser.lastOnline)}`
                    : 'Офлайн'}
              </span>
            </div>
            <a
              href={user.profileUrl}
              target="_blank"
              rel="noopener noreferrer"
              className="text-primary hover:underline flex items-center gap-2 mt-2"
            >
              <Icon name="ExternalLink" size={16} />
              Открыть профиль Steam
            </a>
          </div>

          <div className="grid grid-cols-2 gap-6 pt-4">
            <div className="p-4 rounded-xl bg-gradient-to-br from-primary/20 to-primary/10 border border-primary/30">
              <div className="flex items-center gap-2 mb-2">
                <Icon name="Wallet" size={20} className="text-primary" />
                <p className="text-sm text-muted-foreground">Баланс</p>
              </div>
              <p className="text-3xl font-bold text-primary">{profileUser.balance}₽</p>
            </div>
            <div className="p-4 rounded-xl bg-background/50 border border-border">
              <div className="flex items-center gap-2 mb-2">
                <Icon name="Trophy" size={20} className="text-primary" />
                <p className="text-sm text-muted-foreground">Турниров</p>
              </div>
              <p className="text-3xl font-bold">{statistics.tournaments_count}</p>
            </div>
          </div>
        </div>
      </div>
    </Card>
  );
}