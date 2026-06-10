import { useState } from 'react';
import func2url from '../../../backend/func2url.json';
import { toast } from '@/hooks/use-toast';
import { SteamUser } from './types';

export function useTournamentActions(
  id: string | undefined,
  user: SteamUser | null,
  onReload: () => Promise<void>
) {
  const [isRegistering, setIsRegistering] = useState(false);
  const [isUnregistering, setIsUnregistering] = useState(false);
  const [isConfirming, setIsConfirming] = useState(false);

  const handleRegister = async () => {
    if (!user) {
      toast({ title: 'Требуется авторизация', description: 'Войдите через Steam для регистрации на турнир', variant: 'destructive' });
      return;
    }
    setIsRegistering(true);
    try {
      const response = await fetch(func2url.tournaments, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ tournament_id: Number(id), steam_id: user.steamId, persona_name: user.personaName, avatar_url: user.avatarUrl }),
      });
      const data = await response.json();
      if (response.ok) {
        toast({ title: 'Успешно!', description: 'Регистрация успешна! Увидимся на турнире!' });
        await onReload();
      } else {
        toast({ title: 'Ошибка', description: data.error || 'Ошибка регистрации', variant: 'destructive' });
      }
    } catch {
      toast({ title: 'Ошибка', description: 'Ошибка при регистрации', variant: 'destructive' });
    } finally {
      setIsRegistering(false);
    }
  };

  const confirmUnregister = async () => {
    if (!user) return;
    setIsUnregistering(true);
    try {
      const response = await fetch(func2url.tournaments, {
        method: 'DELETE',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ tournament_id: Number(id), steam_id: user.steamId }),
      });
      const data = await response.json();
      if (response.ok) {
        toast({ title: 'Успешно', description: 'Регистрация отменена' });
        await onReload();
      } else {
        toast({ title: 'Ошибка', description: data.error || 'Ошибка отмены регистрации', variant: 'destructive' });
      }
    } catch {
      toast({ title: 'Ошибка', description: 'Ошибка при отмене регистрации', variant: 'destructive' });
    } finally {
      setIsUnregistering(false);
    }
  };

  const handleConfirmParticipation = async (tournamentId: number) => {
    if (!user) return;
    setIsConfirming(true);
    try {
      const response = await fetch(func2url.tournaments, {
        method: 'PATCH',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ tournament_id: tournamentId, steam_id: user.steamId }),
      });
      const data = await response.json();
      if (response.ok) {
        toast({ title: 'Успешно!', description: 'Участие подтверждено!' });
        await onReload();
      } else {
        toast({ title: 'Ошибка', description: data.error || 'Ошибка подтверждения', variant: 'destructive' });
      }
    } catch {
      toast({ title: 'Ошибка', description: 'Ошибка при подтверждении участия', variant: 'destructive' });
    } finally {
      setIsConfirming(false);
    }
  };

  return { isRegistering, isUnregistering, isConfirming, handleRegister, confirmUnregister, handleConfirmParticipation };
}
