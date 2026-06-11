import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import Icon from '@/components/ui/icon';
import PlayerLink from '@/components/ui/player-link';

export interface User {
  id: number;
  steamId: string;
  personaName: string;
  avatarUrl: string | null;
  profileUrl: string | null;
  balance: number;
  isBlocked: boolean;
  blockReason: string | null;
  isAdmin: boolean;
  isModerator: boolean;
  lastLogin: string | null;
  createdAt: string | null;
  isOnline?: boolean;
  lastOnline?: string | null;
}

interface UserListItemProps {
  user: User;
  editingUserId: string | null;
  balanceAmount: number | null;
  blockReason: string | null;
  onStartEditBalance: (user: User) => void;
  onStartBlock: (user: User) => void;
  onUpdateBalance: (user: User, newBalance: number | null) => void;
  onBlockUser: (user: User, reason: string) => void;
  onUnblockUser: (user: User) => void;
  onToggleAdmin: (user: User) => void;
  onToggleModerator: (user: User) => void;
  onCancelEdit: () => void;
  setBalanceAmount: (amount: number | null) => void;
  setBlockReason: (reason: string | null) => void;
}

export default function UserListItem({
  user,
  editingUserId,
  balanceAmount,
  blockReason,
  onStartEditBalance,
  onStartBlock,
  onUpdateBalance,
  onBlockUser,
  onUnblockUser,
  onToggleAdmin,
  onToggleModerator,
  onCancelEdit,
  setBalanceAmount,
  setBlockReason
}: UserListItemProps) {
  const isEditing = editingUserId === user.steamId;

  return (
    <div
      className={`px-3 py-2 rounded-lg border transition-colors ${
        user.isBlocked
          ? 'border-red-500/30 bg-red-500/5'
          : 'border-border hover:border-primary/30 bg-background/50'
      }`}
    >
      <div className="flex items-center gap-3">
        {user.avatarUrl ? (
          <PlayerLink steamId={user.steamId} name={user.personaName} avatarOnly avatarUrl={user.avatarUrl} avatarSize={8} isOnline={user.isOnline} />
        ) : (
          <div className="w-8 h-8 rounded-full bg-primary/20 flex items-center justify-center flex-shrink-0">
            <Icon name="User" size={16} />
          </div>
        )}

        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-1.5 flex-wrap">
            <PlayerLink steamId={user.steamId} name={user.personaName} className="font-medium text-sm truncate" />
            {user.isAdmin && (
              <span className="text-xs px-1.5 py-0.5 bg-destructive/20 text-destructive rounded font-semibold leading-none">Админ</span>
            )}
            {user.isModerator && (
              <span className="text-xs px-1.5 py-0.5 bg-primary/20 text-primary rounded font-semibold leading-none">Модер</span>
            )}
            {user.isBlocked && (
              <span className="text-xs px-1.5 py-0.5 bg-red-500/20 text-red-500 rounded leading-none">Блок</span>
            )}
          </div>
          <div className="flex items-center gap-3 mt-0.5 text-xs text-muted-foreground">
            <span className="font-mono">{user.steamId}</span>
            <span className="text-green-500 font-semibold">{user.balance} ₽</span>
            {user.createdAt && (
              <span title="Первый вход">🕐 {new Date(user.createdAt + 'Z').toLocaleString('ru-RU', { timeZone: 'Europe/Moscow', day: '2-digit', month: '2-digit', year: '2-digit', hour: '2-digit', minute: '2-digit' })}</span>
            )}
            {user.lastLogin && (
              <span title="Последний вход">↩ {new Date(user.lastLogin + 'Z').toLocaleString('ru-RU', { timeZone: 'Europe/Moscow', day: '2-digit', month: '2-digit', year: '2-digit', hour: '2-digit', minute: '2-digit' })}</span>
            )}
          </div>
          {user.isBlocked && user.blockReason && (
            <p className="text-xs text-red-500 mt-0.5 truncate">{user.blockReason}</p>
          )}
        </div>

        <div className="flex items-center gap-1.5 flex-shrink-0">
          {isEditing ? (
            <>
              {balanceAmount !== null && (
                <>
                  <Input
                    type="number"
                    value={balanceAmount}
                    onChange={(e) => setBalanceAmount(Number(e.target.value))}
                    placeholder="Баланс"
                    className="h-7 w-24 text-sm"
                  />
                  <Button size="sm" className="h-7 w-7 p-0" onClick={() => onUpdateBalance(user, balanceAmount)}>
                    <Icon name="Check" size={13} />
                  </Button>
                  <Button size="sm" variant="outline" className="h-7 w-7 p-0" onClick={onCancelEdit}>
                    <Icon name="X" size={13} />
                  </Button>
                </>
              )}
              {blockReason !== null && balanceAmount === null && (
                <div className="flex items-center gap-1.5">
                  <Textarea
                    value={blockReason}
                    onChange={(e) => setBlockReason(e.target.value)}
                    placeholder="Причина..."
                    rows={1}
                    className="text-sm h-7 min-h-0 py-1 w-36 resize-none"
                  />
                  <Button size="sm" variant="destructive" className="h-7 w-7 p-0" onClick={() => onBlockUser(user, blockReason)}>
                    <Icon name="Ban" size={13} />
                  </Button>
                  <Button size="sm" variant="outline" className="h-7 w-7 p-0" onClick={onCancelEdit}>
                    <Icon name="X" size={13} />
                  </Button>
                </div>
              )}
            </>
          ) : (
            <>
              <Button size="sm" variant="outline" className="h-7 w-7 p-0" title="Изменить баланс" onClick={() => onStartEditBalance(user)}>
                <Icon name="Wallet" size={13} />
              </Button>
              <Button size="sm" variant={user.isAdmin ? "secondary" : "outline"} className="h-7 w-7 p-0" title={user.isAdmin ? "Снять админа" : "Назначить админом"} onClick={() => onToggleAdmin(user)}>
                <Icon name={user.isAdmin ? "UserMinus" : "UserCog"} size={13} />
              </Button>
              <Button size="sm" variant={user.isModerator ? "secondary" : "outline"} className="h-7 w-7 p-0" title={user.isModerator ? "Снять модератора" : "Назначить модератором"} onClick={() => onToggleModerator(user)}>
                <Icon name={user.isModerator ? "ShieldOff" : "Shield"} size={13} />
              </Button>
              {user.isBlocked ? (
                <Button size="sm" variant="outline" className="h-7 w-7 p-0" title="Разблокировать" onClick={() => onUnblockUser(user)}>
                  <Icon name="CheckCircle" size={13} />
                </Button>
              ) : (
                <Button size="sm" variant="destructive" className="h-7 w-7 p-0" title="Заблокировать" onClick={() => onStartBlock(user)}>
                  <Icon name="Ban" size={13} />
                </Button>
              )}
            </>
          )}
        </div>
      </div>
    </div>
  );
}