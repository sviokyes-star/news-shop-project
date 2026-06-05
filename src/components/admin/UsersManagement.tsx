import { useState } from 'react';
import { Card } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import Icon from '@/components/ui/icon';
import UserStats from './users/UserStats';
import UserListItem, { User } from './users/UserListItem';
import { useUserActions } from './users/useUserActions';

interface AdminUser {
  steamId: string;
  personaName: string;
  avatarUrl: string;
  profileUrl: string;
}

interface UsersManagementProps {
  users: User[];
  isLoadingUsers: boolean;
  adminUser: AdminUser | null;
  onReload: () => Promise<void>;
}

export default function UsersManagement({ 
  users, 
  isLoadingUsers, 
  adminUser, 
  onReload 
}: UsersManagementProps) {
  const [editingUserId, setEditingUserId] = useState<string | null>(null);
  const [balanceAmount, setBalanceAmount] = useState<number | null>(null);
  const [blockReason, setBlockReason] = useState<string | null>(null);
  const [searchQuery, setSearchQuery] = useState<string>('');
  const [roleFilter, setRoleFilter] = useState<'all' | 'admin' | 'moderator' | 'blocked'>('all');

  const {
    handleUpdateBalance,
    handleBlockUser,
    handleUnblockUser,
    handleToggleAdmin,
    handleToggleModerator
  } = useUserActions(
    adminUser,
    onReload,
    setEditingUserId,
    setBalanceAmount,
    setBlockReason
  );

  const startEditBalance = (user: User) => {
    setEditingUserId(user.steamId);
    setBalanceAmount(user.balance);
    setBlockReason(null);
  };

  const startBlock = (user: User) => {
    setEditingUserId(user.steamId);
    setBlockReason(user.blockReason || '');
    setBalanceAmount(null);
  };

  const cancelEdit = () => {
    setEditingUserId(null);
    setBalanceAmount(null);
    setBlockReason(null);
  };

  const filteredUsers = users.filter(user => {
    const matchesSearch = user.personaName.toLowerCase().includes(searchQuery.toLowerCase()) || user.steamId.includes(searchQuery);
    const matchesRole =
      roleFilter === 'all' ? true :
      roleFilter === 'admin' ? user.isAdmin :
      roleFilter === 'moderator' ? user.isModerator :
      roleFilter === 'blocked' ? user.isBlocked : true;
    return matchesSearch && matchesRole;
  });

  const totalBalance = users.reduce((sum, user) => sum + user.balance, 0);
  const blockedCount = users.filter(user => user.isBlocked).length;
  const moderatorsCount = users.filter(user => user.isModerator).length;

  return (
    <div className="space-y-6">
      <UserStats 
        totalUsers={users.length}
        totalBalance={totalBalance}
        moderatorsCount={moderatorsCount}
        blockedCount={blockedCount}
      />

      <Card className="p-4 bg-card/80 backdrop-blur border-primary/20">
        <div className="flex items-center justify-between mb-4 gap-3 flex-wrap">
          <h2 className="text-xl font-bold flex items-center gap-2">
            <Icon name="Users" size={20} />
            Управление пользователями
          </h2>
          <div className="flex items-center gap-2 flex-wrap">
            {(['all', 'admin', 'moderator', 'blocked'] as const).map((f) => (
              <button
                key={f}
                onClick={() => setRoleFilter(f)}
                className={`px-3 py-1.5 rounded-lg text-sm font-medium transition-colors ${
                  roleFilter === f ? 'bg-primary text-primary-foreground' : 'bg-secondary text-muted-foreground hover:text-foreground'
                }`}
              >
                {f === 'all' ? 'Все' : f === 'admin' ? 'Администраторы' : f === 'moderator' ? 'Модераторы' : 'Заблокированные'}
              </button>
            ))}
            <Input
              placeholder="Поиск..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="w-44 h-9 text-sm"
            />
          </div>
        </div>

        {isLoadingUsers ? (
          <div className="text-center py-12 text-muted-foreground">
            <Icon name="Loader2" size={48} className="mx-auto mb-3 animate-spin" />
            <p className="text-lg">Загрузка пользователей...</p>
          </div>
        ) : filteredUsers.length === 0 ? (
          <div className="text-center py-12 text-muted-foreground">
            <Icon name="Users" size={48} className="mx-auto mb-3 opacity-20" />
            <p className="text-lg">
              {searchQuery ? 'Пользователи не найдены' : 'Нет зарегистрированных пользователей'}
            </p>
          </div>
        ) : (
          <div className="space-y-3 max-h-[600px] overflow-y-auto">
            {filteredUsers.map((user) => (
              <UserListItem
                key={user.id}
                user={user}
                editingUserId={editingUserId}
                balanceAmount={balanceAmount}
                blockReason={blockReason}
                onStartEditBalance={startEditBalance}
                onStartBlock={startBlock}
                onUpdateBalance={handleUpdateBalance}
                onBlockUser={handleBlockUser}
                onUnblockUser={handleUnblockUser}
                onToggleAdmin={handleToggleAdmin}
                onToggleModerator={handleToggleModerator}
                onCancelEdit={cancelEdit}
                setBalanceAmount={setBalanceAmount}
                setBlockReason={setBlockReason}
              />
            ))}
          </div>
        )}
      </Card>
    </div>
  );
}