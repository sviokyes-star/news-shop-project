import { Card } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import Icon from '@/components/ui/icon';
import { formatRelativeTime } from '@/utils/dateFormat';

type FriendStatus = 'none' | 'pending' | 'accepted' | 'incoming';

interface ProfileUser {
  steamId: string;
  personaName: string;
  nickname?: string;
  avatarUrl: string;
  profileUrl: string;
  balance: number;
  isBlocked: boolean;
  blockReason?: string;
  isOnline?: boolean;
  lastOnline?: string;
}

interface ProfileStatistics {
  tournaments_count: number;
  purchases_count: number;
  total_spent: number;
}

interface UserProfileHeroProps {
  user: ProfileUser;
  statistics: ProfileStatistics;
  me: { steamId: string } | null;
  friendStatus: FriendStatus;
  isFriendLoading: boolean;
  onAddFriend: () => void;
  onCancelRequest: () => void;
  onAccept: () => void;
  onDecline: () => void;
  onRemoveFriend: () => void;
}

export default function UserProfileHero({
  user,
  statistics,
  me,
  friendStatus,
  isFriendLoading,
  onAddFriend,
  onCancelRequest,
  onAccept,
  onDecline,
  onRemoveFriend,
}: UserProfileHeroProps) {
  const displayName = user.nickname || user.personaName;

  const FriendButton = () => {
    if (!me) return null;
    if (friendStatus === 'accepted') return (
      <Button variant="outline" onClick={onRemoveFriend} disabled={isFriendLoading} className="gap-2">
        <Icon name="UserMinus" size={16} />
        Удалить из друзей
      </Button>
    );
    if (friendStatus === 'pending') return (
      <Button variant="outline" onClick={onCancelRequest} disabled={isFriendLoading} className="gap-2 text-muted-foreground hover:text-destructive hover:border-destructive">
        <Icon name="X" size={16} />
        Отменить заявку
      </Button>
    );
    if (friendStatus === 'incoming') return (
      <div className="flex gap-2">
        <Button onClick={onAccept} disabled={isFriendLoading} className="gap-2">
          <Icon name="UserCheck" size={16} />
          Принять заявку
        </Button>
        <Button variant="outline" onClick={onDecline} disabled={isFriendLoading} className="gap-2">
          <Icon name="X" size={16} />
          Отклонить
        </Button>
      </div>
    );
    return (
      <Button onClick={onAddFriend} disabled={isFriendLoading} className="gap-2">
        <Icon name="UserPlus" size={16} />
        Добавить в друзья
      </Button>
    );
  };

  return (
    <Card className="p-10 border border-border bg-gradient-to-br from-primary/5 to-primary/10 backdrop-blur border-primary/20">
      <div className="flex items-start gap-8">
        <div className="relative">
          <img src={user.avatarUrl} alt={displayName} className="w-32 h-32 rounded-2xl border-4 border-primary shadow-2xl shadow-primary/20" />
          <div className={`absolute -bottom-2 -right-2 w-5 h-5 rounded-full border-2 border-background ${user.isOnline ? 'bg-green-500' : 'bg-muted-foreground'}`} />
        </div>
        <div className="flex-1 space-y-4">
          <div className="flex items-start justify-between flex-wrap gap-4">
            <div>
              <h1 className="text-4xl font-bold tracking-tight">{displayName}</h1>
              <div className="flex items-center gap-2 mt-1">
                <span className={`w-2.5 h-2.5 rounded-full flex-shrink-0 ${user.isOnline ? 'bg-green-500' : 'bg-muted-foreground'}`} />
                <span className="text-sm text-muted-foreground">
                  {user.isOnline
                    ? 'Онлайн'
                    : user.lastOnline
                      ? `Был(а) онлайн ${formatRelativeTime(user.lastOnline)}`
                      : 'Офлайн'}
                </span>
              </div>
              <a href={user.profileUrl} target="_blank" rel="noopener noreferrer" className="text-primary hover:underline flex items-center gap-2 mt-2">
                <Icon name="ExternalLink" size={16} />
                Открыть профиль Steam
              </a>
            </div>
            <FriendButton />
          </div>

          {user.isBlocked && (
            <div className="flex items-center gap-2 px-4 py-3 rounded-xl bg-destructive/10 border border-destructive/30">
              <Icon name="Ban" size={18} className="text-destructive" />
              <div>
                <p className="text-sm font-semibold text-destructive">Пользователь заблокирован</p>
                {user.blockReason && <p className="text-xs text-muted-foreground">{user.blockReason}</p>}
              </div>
            </div>
          )}

          <div className="grid grid-cols-2 gap-6 pt-4">
            <div className="p-4 rounded-xl bg-background/50 border border-border">
              <div className="flex items-center gap-2 mb-2">
                <Icon name="Trophy" size={20} className="text-primary" />
                <p className="text-sm text-muted-foreground">Турниров</p>
              </div>
              <p className="text-3xl font-bold">{statistics.tournaments_count}</p>
            </div>
            <div className="p-4 rounded-xl bg-background/50 border border-border">
              <div className="flex items-center gap-2 mb-2">
                <Icon name="ShoppingBag" size={20} className="text-primary" />
                <p className="text-sm text-muted-foreground">Покупок</p>
              </div>
              <p className="text-3xl font-bold">{statistics.purchases_count}</p>
            </div>
          </div>
        </div>
      </div>
    </Card>
  );
}
