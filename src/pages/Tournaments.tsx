import { useState, useEffect } from 'react';
import TournamentsTab from '@/components/TournamentsTab';
import { toast } from '@/hooks/use-toast';
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog';
import apiUrl from '@/lib/api';

interface SteamUser {
  steamId: string;
  personaName: string;
  avatarUrl: string;
  profileUrl: string;
}

interface Tournament {
  id: number;
  name: string;
  description: string;
  prize_pool: number;
  max_participants: number;
  status: string;
  tournament_type: string;
  game: string;
  start_date: string;
  participants_count: number;
  is_registered?: boolean;
  confirmed_at?: string | null;
}

const Tournaments = () => {
  const [isLoginOpen, setIsLoginOpen] = useState(false);
  const [isRegisterOpen, setIsRegisterOpen] = useState(false);
  const [user, setUser] = useState<SteamUser | null>(() => {
    const saved = localStorage.getItem('steamUser');
    return saved ? JSON.parse(saved) : null;
  });
  const [tournaments, setTournaments] = useState<Tournament[]>([]);
  const [isLoadingTournaments, setIsLoadingTournaments] = useState(true);
  const [isRegistering, setIsRegistering] = useState<number | null>(null);
  const [unregisterTournamentId, setUnregisterTournamentId] = useState<number | null>(null);

  useEffect(() => {
    const params = new URLSearchParams(window.location.search);
    const claimedId = params.get('openid.claimed_id');
    
    if (claimedId) {
      const verifyParams = new URLSearchParams();
      params.forEach((value, key) => {
        verifyParams.append(key, value);
      });
      verifyParams.append('mode', 'verify');
      
      fetch(`${apiUrl['steam-auth']}?${verifyParams.toString()}`)
        .then(res => res.json())
        .then(data => {
          if (data.steamId) {
            setUser(data);
            localStorage.setItem('steamUser', JSON.stringify(data));
            window.history.replaceState({}, '', window.location.pathname);
          }
        })
        .catch(err => console.error('Steam auth error:', err));
    }
  }, []);

  useEffect(() => {
    loadTournaments();
  }, [user]);

  const loadTournaments = async () => {
    setIsLoadingTournaments(true);
    try {
      const url = user 
        ? `${apiUrl['tournaments']}?steam_id=${user.steamId}`
        : apiUrl['tournaments'];
      
      const response = await fetch(url);
      const data = await response.json();
      setTournaments(data.tournaments || []);
    } catch (error) {
      console.error('Failed to load tournaments:', error);
    } finally {
      setIsLoadingTournaments(false);
    }
  };

  const handleTournamentRegister = async (tournamentId: number) => {
    if (!user) {
      toast({
        title: "Требуется авторизация",
        description: "Войдите через Steam для регистрации на турнир",
        variant: "destructive"
      });
      return;
    }

    setIsRegistering(tournamentId);

    try {
      const response = await fetch(apiUrl['tournaments'], {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          tournament_id: tournamentId,
          steam_id: user.steamId,
          persona_name: user.personaName,
          avatar_url: user.avatarUrl
        })
      });

      const data = await response.json();

      if (response.ok) {
        toast({
          title: "Успешно!",
          description: "Регистрация успешна! Увидимся на турнире!"
        });
        await loadTournaments();
      } else {
        toast({
          title: "Ошибка",
          description: data.error || 'Ошибка регистрации',
          variant: "destructive"
        });
      }
    } catch (error) {
      console.error('Registration failed:', error);
      toast({
        title: "Ошибка",
        description: "Ошибка при регистрации",
        variant: "destructive"
      });
    } finally {
      setIsRegistering(null);
    }
  };

  const confirmUnregister = async () => {
    if (!user || !unregisterTournamentId) return;

    setUnregisterTournamentId(null);

    try {
      const tournamentId = unregisterTournamentId;
      const response = await fetch(apiUrl['tournaments'], {
        method: 'DELETE',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          tournament_id: tournamentId,
          steam_id: user.steamId
        })
      });

      const data = await response.json();

      if (response.ok) {
        toast({
          title: "Успешно",
          description: "Регистрация отменена"
        });
        await loadTournaments();
      } else {
        toast({
          title: "Ошибка",
          description: data.error || 'Ошибка при отмене регистрации',
          variant: "destructive"
        });
      }
    } catch (error) {
      console.error('Unregister failed:', error);
      toast({
        title: "Ошибка",
        description: "Ошибка при отмене регистрации",
        variant: "destructive"
      });
    }
  };

  const handleSteamLogin = async () => {
    const returnUrl = `${window.location.origin}${window.location.pathname}`;
    const response = await fetch(`${apiUrl['steam-auth']}?mode=login&return_url=${encodeURIComponent(returnUrl)}`);
    const data = await response.json();
    
    if (data.redirectUrl) {
      window.location.href = data.redirectUrl;
    }
  };

  const handleLogout = () => {
    setUser(null);
    localStorage.removeItem('steamUser');
  };

  const handleConfirmParticipation = async (tournamentId: number) => {
    if (!user) return;

    try {
      const response = await fetch(apiUrl['tournaments'], {
        method: 'PATCH',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          tournament_id: tournamentId,
          steam_id: user.steamId
        })
      });

      const data = await response.json();

      if (response.ok) {
        toast({
          title: "Успешно!",
          description: "Участие подтверждено!"
        });
        await loadTournaments();
      } else {
        toast({
          title: "Ошибка",
          description: data.error || 'Ошибка подтверждения',
          variant: "destructive"
        });
      }
    } catch (error) {
      console.error('Confirmation failed:', error);
      toast({
        title: "Ошибка",
        description: "Ошибка при подтверждении участия",
        variant: "destructive"
      });
    }
  };

  return (
      <>
        <main className="container mx-auto px-6 py-16">
          <TournamentsTab
            tournaments={tournaments}
            isLoading={isLoadingTournaments}
            user={user}
            isRegistering={isRegistering}
            onRegister={handleTournamentRegister}
            onUnregister={(id) => setUnregisterTournamentId(id)}
            onConfirm={handleConfirmParticipation}
          />
        </main>

        <AlertDialog open={!!unregisterTournamentId} onOpenChange={(open) => !open && setUnregisterTournamentId(null)}>
          <AlertDialogContent>
            <AlertDialogHeader>
              <AlertDialogTitle>Отменить регистрацию?</AlertDialogTitle>
              <AlertDialogDescription>
                Вы уверены, что хотите отменить регистрацию на турнир? Это действие можно отменить, зарегистрировавшись снова.
              </AlertDialogDescription>
            </AlertDialogHeader>
            <AlertDialogFooter>
              <AlertDialogCancel>Отмена</AlertDialogCancel>
              <AlertDialogAction onClick={confirmUnregister} className="bg-destructive hover:bg-destructive/90">
                Отменить регистрацию
              </AlertDialogAction>
            </AlertDialogFooter>
          </AlertDialogContent>
        </AlertDialog>
      </>
  );
};

export default Tournaments;