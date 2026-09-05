import func2url from '@/lib/api';
import type { User } from './UserListItem';

interface AdminUser {
  steamId: string;
  personaName: string;
  avatarUrl: string;
  profileUrl: string;
}

export function useUserActions(
  adminUser: AdminUser | null,
  onReload: () => Promise<void>,
  setEditingUserId: (id: string | null) => void,
  setBalanceAmount: (amount: number | null) => void,
  setBlockReason: (reason: string | null) => void
) {
  const handleUpdateBalance = async (user: User, newBalance: number | null) => {
    if (!adminUser || newBalance === null) return;

    try {
      console.log('Updating balance:', { steamId: user.steamId, balance: newBalance });
      const response = await fetch(func2url.users, {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
          'X-Admin-Steam-Id': adminUser.steamId
        },
        body: JSON.stringify({
          steamId: user.steamId,
          balance: newBalance
        })
      });

      console.log('Balance update response:', response.status, await response.text());

      if (response.ok) {
        await onReload();
        setEditingUserId(null);
        setBalanceAmount(null);
      } else {
        alert('Ошибка при обновлении баланса');
      }
    } catch (error) {
      console.error('Failed to update balance:', error);
      alert('Ошибка при обновлении баланса: ' + error);
    }
  };

  const handleBlockUser = async (user: User, reason: string) => {
    if (!adminUser) return;
    if (!reason.trim()) {
      alert('Укажите причину блокировки');
      return;
    }

    try {
      console.log('Blocking user:', { steamId: user.steamId, reason });
      const response = await fetch(func2url.users, {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
          'X-Admin-Steam-Id': adminUser.steamId
        },
        body: JSON.stringify({
          steamId: user.steamId,
          isBlocked: true,
          blockReason: reason
        })
      });

      console.log('Block response:', response.status);

      if (response.ok) {
        await onReload();
        setEditingUserId(null);
        setBlockReason('');
      } else {
        alert('Ошибка при блокировке пользователя');
      }
    } catch (error) {
      console.error('Failed to block user:', error);
      alert('Ошибка при блокировке: ' + error);
    }
  };

  const handleUnblockUser = async (user: User) => {
    if (!adminUser) return;

    try {
      const response = await fetch(func2url.users, {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
          'X-Admin-Steam-Id': adminUser.steamId
        },
        body: JSON.stringify({
          steamId: user.steamId,
          isBlocked: false
        })
      });

      if (response.ok) {
        await onReload();
      }
    } catch (error) {
      console.error('Failed to unblock user:', error);
    }
  };

  const handleToggleAdmin = async (user: User) => {
    if (!adminUser) return;

    const action = user.isAdmin ? 'удалить права администратора' : 'назначить администратором';
    if (!confirm(`Вы уверены, что хотите ${action} для ${user.personaName}?`)) {
      return;
    }

    try {
      console.log('Toggling admin:', { steamId: user.steamId, isAdmin: !user.isAdmin });
      const response = await fetch(func2url.users, {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
          'X-Admin-Steam-Id': adminUser.steamId
        },
        body: JSON.stringify({
          steamId: user.steamId,
          isAdmin: !user.isAdmin
        })
      });

      console.log('Toggle admin response:', response.status);

      if (response.ok) {
        await onReload();
      } else {
        alert('Ошибка при изменении прав администратора');
      }
    } catch (error) {
      console.error('Failed to toggle admin:', error);
      alert('Ошибка при изменении прав: ' + error);
    }
  };

  const handleToggleModerator = async (user: User) => {
    if (!adminUser) return;

    const action = user.isModerator ? 'снять права модератора' : 'назначить модератором';
    if (!confirm(`Вы уверены, что хотите ${action} для ${user.personaName}?`)) {
      return;
    }

    try {
      const response = await fetch(func2url.users, {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
          'X-Admin-Steam-Id': adminUser.steamId
        },
        body: JSON.stringify({
          steamId: user.steamId,
          isModerator: !user.isModerator
        })
      });

      if (response.ok) {
        await onReload();
      } else {
        alert('Ошибка при изменении прав модератора');
      }
    } catch (error) {
      console.error('Failed to toggle moderator:', error);
      alert('Ошибка при изменении прав: ' + error);
    }
  };

  return {
    handleUpdateBalance,
    handleBlockUser,
    handleUnblockUser,
    handleToggleAdmin,
    handleToggleModerator
  };
}
