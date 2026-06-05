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
  return (
    <div
      className={`p-4 rounded-lg border bg-background/50 transition-colors ${
        user.isBlocked 
          ? 'border-red-500/30 bg-red-500/5' 
          : 'border-border hover:border-primary/30'
      }`}
    >
      <div className="flex items-start justify-between gap-4">
        <div className="flex items-start gap-3 flex-1 min-w-0">
          {user.avatarUrl ? (
            <PlayerLink steamId={user.steamId} name={user.personaName} showAvatar avatarUrl={user.avatarUrl} avatarSize={12} />
          ) : (
            <div className="w-12 h-12 rounded-full bg-primary/20 flex items-center justify-center">
              <Icon name="User" size={24} />
            </div>
          )}

          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2 mb-1">
              <PlayerLink steamId={user.steamId} name={user.personaName} className="truncate" />
              {user.isAdmin && (
                <span className="text-xs px-2 py-0.5 bg-destructive/20 text-destructive rounded font-semibold">
                  Администратор
                </span>
              )}
              {user.isModerator && (
                <span className="text-xs px-2 py-0.5 bg-primary/20 text-primary rounded font-semibold">
                  Модератор
                </span>
              )}
              {user.isBlocked && (
                <span className="text-xs px-2 py-0.5 bg-red-500/20 text-red-500 rounded">
                  Заблокирован
                </span>
              )}
            </div>

            <div className="space-y-1 text-sm text-muted-foreground">
              <div className="flex items-center gap-2">
                <Icon name="Hash" size={14} />
                <span className="font-mono text-xs">{user.steamId}</span>
              </div>
              <div className="flex items-center gap-4">
                <div className="flex items-center gap-1">
                  <Icon name="Wallet" size={14} />
                  <span className="font-semibold text-green-500">{user.balance} ₽</span>
                </div>
                {user.lastLogin && (
                  <div className="flex items-center gap-1">
                    <Icon name="Clock" size={14} />
                    <span className="text-xs">
                      {new Date(user.lastLogin).toLocaleString('ru-RU', { timeZone: 'Europe/Moscow' })}
                    </span>
                  </div>
                )}
              </div>

              {user.isBlocked && user.blockReason && (
                <div className="flex items-start gap-1 mt-2 p-2 bg-red-500/10 rounded">
                  <Icon name="AlertCircle" size={14} className="text-red-500 mt-0.5" />
                  <span className="text-xs text-red-500">{user.blockReason}</span>
                </div>
              )}
            </div>
          </div>
        </div>

        <div className="flex flex-col gap-2 min-w-[140px]">
          {editingUserId === user.steamId ? (
            <>
              {balanceAmount !== null && (
                <>
                  <Input
                    type="number"
                    value={balanceAmount}
                    onChange={(e) => setBalanceAmount(Number(e.target.value))}
                    placeholder="Новый баланс"
                    className="h-8 text-sm"
                  />
                  <div className="flex gap-2">
                    <Button
                      size="sm"
                      onClick={() => onUpdateBalance(user, balanceAmount)}
                      className="flex-1"
                    >
                      <Icon name="Check" size={14} />
                    </Button>
                    <Button
                      size="sm"
                      variant="outline"
                      onClick={onCancelEdit}
                      className="flex-1"
                    >
                      <Icon name="X" size={14} />
                    </Button>
                  </div>
                </>
              )}
              {blockReason !== null && balanceAmount === null && (
                <>
                  <Textarea
                    value={blockReason}
                    onChange={(e) => setBlockReason(e.target.value)}
                    placeholder="Причина блокировки..."
                    rows={2}
                    className="text-sm"
                  />
                  <div className="flex gap-2">
                    <Button
                      size="sm"
                      variant="destructive"
                      onClick={() => onBlockUser(user, blockReason)}
                      className="flex-1"
                    >
                      <Icon name="Ban" size={14} />
                    </Button>
                    <Button
                      size="sm"
                      variant="outline"
                      onClick={onCancelEdit}
                      className="flex-1"
                    >
                      <Icon name="X" size={14} />
                    </Button>
                  </div>
                </>
              )}
            </>
          ) : (
            <>
              <Button
                size="sm"
                variant="outline"
                onClick={() => onStartEditBalance(user)}
                className="gap-2"
              >
                <Icon name="Wallet" size={14} />
                Баланс
              </Button>
              <Button
                size="sm"
                variant={user.isAdmin ? "secondary" : "outline"}
                onClick={() => onToggleAdmin(user)}
                className="gap-2"
              >
                <Icon name={user.isAdmin ? "UserMinus" : "UserPlus"} size={14} />
                {user.isAdmin ? "Снять админа" : "Админ"}
              </Button>
              <Button
                size="sm"
                variant={user.isModerator ? "secondary" : "outline"}
                onClick={() => onToggleModerator(user)}
                className="gap-2"
              >
                <Icon name={user.isModerator ? "ShieldOff" : "Shield"} size={14} />
                {user.isModerator ? "Снять модера" : "Модер"}
              </Button>
              {user.isBlocked ? (
                <Button
                  size="sm"
                  variant="outline"
                  onClick={() => onUnblockUser(user)}
                  className="gap-2"
                >
                  <Icon name="CheckCircle" size={14} />
                  Разблокировать
                </Button>
              ) : (
                <Button
                  size="sm"
                  variant="destructive"
                  onClick={() => onStartBlock(user)}
                  className="gap-2"
                >
                  <Icon name="Ban" size={14} />
                  Заблокировать
                </Button>
              )}
            </>
          )}
        </div>
      </div>
    </div>
  );
}